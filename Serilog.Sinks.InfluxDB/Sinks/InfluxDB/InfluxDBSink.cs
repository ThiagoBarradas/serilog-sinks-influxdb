using InfluxDB.Client;
using InfluxDB.Client.Core.Exceptions;
using InfluxDB.Client.Writes;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using static Serilog.Sinks.InfluxDB.Sinks.InfluxDB.SyslogConst;
using InfluxDBDomain = InfluxDB.Client.Api.Domain;

namespace Serilog.Sinks.InfluxDB.Sinks.InfluxDB
{
    internal class InfluxDBSink : IBatchedLogEventSink, IDisposable
    {
        private readonly string _applicationName;
        private readonly string _instanceName;

        private readonly IFormatProvider _formatProvider;

        /// <summary>
        /// Connection info used to connect to InfluxDB instance.
        /// </summary>
        private readonly InfluxDBConnectionInfo _connectionInfo;

        /// <summary>
        /// Client object used to connect to InfluxDB instance.
        /// </summary>
        private InfluxDBClient _influxDbClient;

        /// <inheritdoc />
        /// <summary>
        /// Construct a sink inserting into InfluxDB with the specified details.
        /// </summary>
        /// <param name="connectionInfo">Connection information used to construct InfluxDB client.</param>
        /// <param name="applicationName">Application name in the InfluxDB bucket.</param>
        /// <param name="instanceName">Facility name in the InfluxDB bucket.</param>
        /// <param name="batchSizeLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <param name="formatProvider"></param>
        public InfluxDBSink(InfluxDBSinkOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            _connectionInfo = options.ConnectionInfo ?? throw new ArgumentNullException(nameof(options.ConnectionInfo));

            if (options.ApplicationName is null) throw new ArgumentNullException(nameof(options.ApplicationName));
            if (_connectionInfo.Uri is null) throw new ArgumentNullException(nameof(_connectionInfo.Uri));
            if (_connectionInfo.BucketName is null) throw new ArgumentNullException(nameof(_connectionInfo.BucketName));
            if (_connectionInfo.OrganizationId is null) throw new ArgumentNullException(nameof(_connectionInfo.OrganizationId));
            if (string.IsNullOrWhiteSpace(_connectionInfo.Token) && string.IsNullOrWhiteSpace(_connectionInfo.AllAccessToken))
                throw new ArgumentNullException(nameof(_connectionInfo.Token), $"At least one Token should be given either {nameof(_connectionInfo.Token)} if already created with write permissions or {nameof(_connectionInfo.AllAccessToken)}");

            _applicationName = options.ApplicationName;
            _instanceName = options.InstanceName ?? _applicationName;
            _formatProvider = options.FormatProvider;

            CreateBucketIfNotExists();

            _influxDbClient = CreateInfluxDbClient();
        }

        /// <inheritdoc />
        /// <summary>
        /// Emit a batch of log events, running asynchronously.
        /// </summary>
        /// <param name="events">The events to emit.</param>
        public async Task EmitBatchAsync(IEnumerable<LogEvent> batch)
        {
            if (batch == null) throw new ArgumentNullException(nameof(batch));

            var logEvents = batch as List<LogEvent> ?? batch.ToList();
            var points = new List<PointData>(logEvents.Count);

            foreach (var logEvent in logEvents)
            {
                var severity = logEvent.Level.ToSeverity();

                var p = PointData.Measurement(PointName)
                    .Tag(Tags.Level, logEvent.Level.ToString())
                    .Tag(Tags.AppName, _applicationName)
                    .Tag(Tags.Facility, _instanceName)
                    .Tag(Tags.Hostname, Environment.MachineName)
                    .Tag(Tags.Severity, severity.ToString())
                    .Field(Fields.Message, StripSpecialCharacter(logEvent.RenderMessage(_formatProvider)))
                    .Field(Fields.Facility, 16)
                    .Field(Fields.ProcId, Process.GetCurrentProcess().Id)
                    .Field(Fields.Severity, severity.ToString())
                    .Field(Fields.Timestamp, logEvent.Timestamp.ToUnixTimeMilliseconds() * 1000000)
                    .Field(Fields.Version, 1)
                    .Timestamp(logEvent.Timestamp.UtcDateTime, InfluxDBDomain.WritePrecision.Ms);

                if (logEvent.Exception != null) p = p.Tag("exceptionType", logEvent.Exception.GetType().Name);

                points.Add(p);
            }

            //Not handling exceptions and let handle by wrapping class PeriodicBatchingSink
            //TODO wrap exception with InfluxDbClientWriteException
            await _influxDbClient.GetWriteApiAsync().WritePointsAsync(_connectionInfo.BucketName, _connectionInfo.OrganizationId, points).ConfigureAwait(false);

            //            if (!response.Success)
            //            {
            //                // in case InfluxDbClient not throwing exception, throw new Exception to be handle by base class PeriodicBatchingSink

            //                var message = $@"A status code of {response.StatusCode} was received when attempting to send to {_connectionInfo?.Uri}.
            //The event has been discarded and will not be placed back in the queue.
            //Response body: {response.Body}";

            //                SelfLog.WriteLine(message);

            //                throw new InfluxDbClientWriteException(message);
            //            }
        }

        public Task OnEmptyBatchAsync()
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            //TODO add disposing pattern
            _influxDbClient?.Dispose();
            _influxDbClient = null;
        }


        private string StripSpecialCharacter(string text)
        {
            return text != null ?
                    HttpUtility.JavaScriptStringEncode(text)
                : string.Empty;
        }

        /// <summary>
        /// Initialize and return an InfluxDB client object.
        /// </summary>
        /// <returns></returns>
        private InfluxDBClient CreateInfluxDbClient()
        {
            return InfluxDBClientFactory.Create(
                InfluxDBClientOptions
                    .Builder
                    .CreateNew()
                    .Url(_connectionInfo.Uri.ToString())
                    //TODO Change options to accodomate two ways of authentication token or basic auth                    
                    .AuthenticateToken((_connectionInfo.Token ?? _connectionInfo.AllAccessToken).ToCharArray())
                    .Bucket(_connectionInfo.BucketName) 
                    .Build()
                    );
        }

        /// <summary>
        /// Create the log Bucket in InfluxDB if it does not exists. 
        /// Synchronous as should be done prior any emit done and also as don't want to move check on each emit if db exists
        /// </summary>
        private void CreateBucketIfNotExists()
        {
            using (var createBucketClient = InfluxDBClientFactory.Create(
                InfluxDBClientOptions
                    .Builder
                    .CreateNew()
                    .Url(_connectionInfo.Uri.ToString())
                    .AuthenticateToken((_connectionInfo.AllAccessToken ?? _connectionInfo.Token).ToCharArray())
                    .Build()
                    ))
            {
                var bucket = GetBucketOrDefault(createBucketClient, _connectionInfo.BucketName);

                if (bucket == null)
                {
                    if (!_connectionInfo.CreateBucketIfNotExists) throw new InfluxDbClientCreateBucketException($"Cannot create bucket {_connectionInfo.BucketName} as not existing and parameter {_connectionInfo.CreateBucketIfNotExists} set to false");

                    var newBucket = CreateBucket(createBucketClient);
                    _connectionInfo.Token = GenerateWriteToken(createBucketClient, newBucket);
                }
            }
        }

        private InfluxDBDomain.Bucket GetBucketOrDefault(InfluxDBClient createBucketClient, string bucketName)
        {
            //TODO use Maybe monad ?
            InfluxDBDomain.Bucket bucket;
            try
            {
                bucket = createBucketClient.GetBucketsApi()
                    .FindBucketByNameAsync(bucketName)
                    .GetAwaiter().GetResult();
            }
            catch (HttpException ex)
            {
                SelfLog.WriteLine($"Error while trying to get {_connectionInfo.BucketName} (Org: {_connectionInfo.OrganizationId}), Message : {ex.Message}. Check if Token has enough access to read (if only write to bucket then set to False parameter {_connectionInfo.CreateBucketIfNotExists}) or set AllAccessToken or is active token");
                throw new InfluxDbClientCreateBucketException($"Cannot create bucket {_connectionInfo.BucketName}. Check if Token has enough access or set AllAccessToken or is active", ex);
            }

            return bucket;
        }

        private string GenerateWriteToken(InfluxDBClient createBucketClient, InfluxDBDomain.Bucket bucket)
        {
            var resource = new InfluxDBDomain.PermissionResource { Id = bucket.Id, OrgID = _connectionInfo.OrganizationId, Type = InfluxDBDomain.PermissionResource.TypeEnum.Buckets };

            var write = new InfluxDBDomain.Permission(InfluxDBDomain.Permission.ActionEnum.Write, resource);
            string token;

            try
            {
                var authorization = createBucketClient.GetAuthorizationsApi()
                .CreateAuthorizationAsync(_connectionInfo.OrganizationId, new List<InfluxDBDomain.Permission> { write })
                .GetAwaiter().GetResult();
                token = authorization?.Token;
            }
            catch (HttpException ex)
            {
                SelfLog.WriteLine($"Error while trying to get {_connectionInfo.BucketName} (Org: {_connectionInfo.OrganizationId}), Message : {ex.Message}. Check if Token has enough access to read (if only write to bucket then set to False parameter {_connectionInfo.CreateBucketIfNotExists}) or set AllAccessToken or is active token");
                throw new InfluxDbClientCreateBucketException($"Cannot create token for bucket {_connectionInfo.BucketName} with write permissions. Check if Token has enough access or set AllAccessToken or is active", ex);
            }

            SelfLog.WriteLine($"Token generated successfully for bucket {bucket.Name}, using it for write operations");

            return token;
        }

        private InfluxDBDomain.Bucket CreateBucket(InfluxDBClient createBucketClient)
        {
            var retention = new InfluxDBDomain.BucketRetentionRules(InfluxDBDomain.BucketRetentionRules.TypeEnum.Expire, _connectionInfo.BucketRetentionPeriodInSeconds);

            InfluxDBDomain.Bucket bucket;
            try
            {
                bucket = createBucketClient.GetBucketsApi().CreateBucketAsync(_connectionInfo.BucketName, retention, _connectionInfo.OrganizationId).GetAwaiter().GetResult();
            }
            catch (HttpException ex)
            {
                SelfLog.WriteLine($"Error while creating {_connectionInfo.BucketName} (Org: {_connectionInfo.OrganizationId}), Message : {ex.Message}. Check if Token has enough access or set AllAccessToken or is active");
                throw new InfluxDbClientCreateBucketException($"Cannot create bucket {_connectionInfo.BucketName}. Check if Token has enough access or set AllAccessToken or is active", ex);
            }

            SelfLog.WriteLine($"Bucket {bucket.Name} ({bucket.Id} / Org: {bucket.OrgID}) created at {bucket.CreatedAt}");

            return bucket;
        }
    }
}
