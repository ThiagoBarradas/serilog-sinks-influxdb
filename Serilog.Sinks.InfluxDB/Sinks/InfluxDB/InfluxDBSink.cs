using InfluxData.Net.Common.Enums;
using InfluxData.Net.InfluxDb;
using InfluxData.Net.InfluxDb.Models;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Sinks.InfluxDB.Sinks.InfluxDB;
using Serilog.Sinks.PeriodicBatching;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using static Serilog.Sinks.InfluxDB.Sinks.InfluxDB.SyslogConst;

namespace Serilog.Sinks.InfluxDB
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
        private InfluxDbClient _influxDbClient;

        /// <summary>
        /// A reasonable default for the number of events posted in
        /// each batch.
        /// </summary>
        public const int DefaultBatchPostingLimit = 100;

        /// <summary>
        /// A reasonable default time to wait between checking for event batches.
        /// </summary>
        public static readonly TimeSpan DefaultPeriod = TimeSpan.FromSeconds(30);

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
        public InfluxDBSink(InfluxDBConnectionInfo connectionInfo, string applicationName, string instanceName, IFormatProvider formatProvider)
        {
            _connectionInfo = connectionInfo ?? throw new ArgumentNullException(nameof(connectionInfo));
            _applicationName = applicationName;
            _instanceName = instanceName ?? applicationName;
            _influxDbClient = CreateInfluxDbClient();
            _formatProvider = formatProvider;           
            
            CreateDatabaseIfNotExists();
        }

        /// <inheritdoc />
        /// <summary>
        /// Construct a sink inserting into InfluxDB with the specified details.
        /// </summary>
        /// <param name="connectionInfo">Connection information used to construct InfluxDB client.</param>
        /// <param name="applicationName">Measurement name in the InfluxDB database.</param>
        /// <param name="batchSizeLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <param name="formatProvider"></param>
        public InfluxDBSink(InfluxDBConnectionInfo connectionInfo, string applicationName, int batchSizeLimit, TimeSpan period,
            IFormatProvider formatProvider)
            : this(connectionInfo, applicationName, null, formatProvider)
        {
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
            var points = new List<Point>(logEvents.Count);

            foreach (var logEvent in logEvents)
            {
                var p = new Point
                {
                    Name = PointName,
                    Fields = new Dictionary<string, object>(),
                    Timestamp = logEvent.Timestamp.UtcDateTime
                };

                // Add tags
                if (logEvent.Exception != null) p.Tags.Add("exceptionType", logEvent.Exception.GetType().Name);

                var severity = logEvent.Level.ToSeverity();

                p.Tags.Add(Tags.Level, logEvent.Level.ToString());
                p.Tags.Add(Tags.AppName, _applicationName);
                p.Tags.Add(Tags.Facility, _instanceName);
                p.Tags.Add(Tags.Hostname, Environment.MachineName);
                p.Tags.Add(Tags.Severity, severity.ToString());

                // Add Fields - rendered message
                p.Fields[Fields.Message] = StripSpecialCharacter(logEvent.RenderMessage(_formatProvider));
                p.Fields[Fields.Facility] = 16;
                p.Fields[Fields.ProcId] = Process.GetCurrentProcess().Id;
                p.Fields[Fields.Severity] = severity.ToString();
                p.Fields[Fields.Timestamp] = logEvent.Timestamp.ToUnixTimeMilliseconds() * 1000000;
                p.Fields[Fields.Version] = 1;

                points.Add(p);
            }


            var response = await _influxDbClient.Client.WriteAsync(points, _connectionInfo.DbName);

            if (!response.Success)
            {
                // in case InfluxDbClient not throwing exception, throw new Exception to be handle by base class PeriodicBatchingSink

                var message = $@"A status code of {response.StatusCode} was received when attempting to send to {_connectionInfo?.Uri}.
The event has been discarded and will not be placed back in the queue.
Response body: {response.Body}";

                SelfLog.WriteLine(message);

                throw new InfluxDbClientWriteException(message);
            }
        }

        public Task OnEmptyBatchAsync()
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
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
        private InfluxDbClient CreateInfluxDbClient()
        {
            return new InfluxDbClient(
                _connectionInfo.Uri.ToString(),
                _connectionInfo.Username,
                _connectionInfo.Password,
                InfluxDbVersion.Latest);
        }

        /// <summary>
        /// Create the log database in InfluxDB if it does not exists. 
        /// Synchronous as should be done prior any emit done and also as don't want to move check on each emit if db exists
        /// </summary>
        private void CreateDatabaseIfNotExists()
        {
            var dbList = _influxDbClient.Database.GetDatabasesAsync().GetAwaiter().GetResult();
            if (dbList.All(db => db.Name != _connectionInfo.DbName))
            {
                var _ = _influxDbClient.Database.CreateDatabaseAsync(_connectionInfo.DbName).GetAwaiter().GetResult();
            }
        }

    }
}
