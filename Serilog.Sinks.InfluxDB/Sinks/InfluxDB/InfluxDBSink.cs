using InfluxDB.Client;
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
using InfluxDBDomain = InfluxDB.Client.Api.Domain;
using static Serilog.Sinks.InfluxDB.Sinks.InfluxDB.SyslogConst;

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
        /// <param name="applicationName">Application name in the InfluxDB database.</param>
        /// <param name="instanceName">Facility name in the InfluxDB database.</param>
        /// <param name="batchSizeLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <param name="formatProvider"></param>
        public InfluxDBSink(InfluxDBSinkOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            _connectionInfo = options.ConnectionInfo ?? throw new ArgumentNullException(nameof(options.ConnectionInfo));

            if (options.ApplicationName == null) throw new ArgumentNullException(nameof(options.ApplicationName));
            if (_connectionInfo.Uri == null) throw new ArgumentNullException(nameof(_connectionInfo.Uri));
            if (_connectionInfo.DbName == null) throw new ArgumentNullException(nameof(_connectionInfo.DbName));
            if (_connectionInfo.Username == null) _connectionInfo.Username = string.Empty;
            if (_connectionInfo.Password == null) _connectionInfo.Password = string.Empty;

            _applicationName = options.ApplicationName;
            _instanceName = options.InstanceName ?? _applicationName;
            _formatProvider = options.FormatProvider;

            _influxDbClient = CreateInfluxDbClient();

            CreateDatabaseIfNotExists();
        }

        /// <inheritdoc />
        /// <summary>
        /// Emit a batch of log events, running asynchronously.
        /// </summary>
        /// <param name="events">The events to emit.</param>
        public async Task EmitBatchAsync(IEnumerable<LogEvent> batch)
        {
            //TODO from config
            var orgId = "88e1f5a5ad074d9e";
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


            await _influxDbClient.GetWriteApiAsync().WritePointsAsync(_connectionInfo.DbName, orgId, points).ConfigureAwait(false);

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
                    .AuthenticateToken("bGfBKhSycNiUOia4k7peib2jHFewkz3o6Hv2uz1xAoUcdnEFRW7cHn03KICySLemA4VPZKvc0CwzSQT8GNl2DA==".ToCharArray())
                    .Bucket(_connectionInfo.DbName) //todo put in config with BucketName instead of DbName
                    .Build()
                    );
        }

        /// <summary>
        /// Create the log database in InfluxDB if it does not exists. 
        /// Synchronous as should be done prior any emit done and also as don't want to move check on each emit if db exists
        /// </summary>
        private void CreateDatabaseIfNotExists()
        {
            var buckets = _influxDbClient.GetBucketsApi().FindBucketsAsync().GetAwaiter().GetResult();
            if (buckets.All(b => b.Name != _connectionInfo.DbName))
            {
                //TODO add this retention in Seconds to config and OrgId
                var orgId = "88e1f5a5ad074d9e";
                var retention = new InfluxDBDomain.BucketRetentionRules(InfluxDBDomain.BucketRetentionRules.TypeEnum.Expire, 3600);
                var bucket = _influxDbClient.GetBucketsApi().CreateBucketAsync(_connectionInfo.DbName, retention, orgId).GetAwaiter().GetResult();

                SelfLog.WriteLine($"Bucket {bucket.Name} ({bucket.Id} / Org: {bucket.OrgID}) created at {bucket.CreatedAt}");

                //// Write permission
                //var resource = new InfluxDBDomain.PermissionResource { Id = bucket.Id, OrgID = orgId, Type = InfluxDBDomain.PermissionResource.TypeEnum.Buckets };

                //var read = new InfluxDBDomain.Permission { Resource = resource, Action = InfluxDBDomain.Permission.ActionEnum.Read };
                //var write = new InfluxDBDomain.Permission { Resource = resource, Action = InfluxDBDomain.Permission.ActionEnum.Write };

                //var authorization = _influxDbClient.GetAuthorizationsApi()
                //    .CreateAuthorizationAsync(orgId, new List<InfluxDBDomain.Permission> { read, write })
                //    .GetAwaiter().GetResult();

                //SelfLog.WriteLine($"Token generated successfully for bucket {bucket.Name}");                

                //var token = authorization.Token;
            }
        }

    }
}
