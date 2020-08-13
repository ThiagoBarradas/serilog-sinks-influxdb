using InfluxData.Net.Common.Enums;
using InfluxData.Net.InfluxDb;
using InfluxData.Net.InfluxDb.Models;
using Newtonsoft.Json;
using Serilog.Events;
using Serilog.Parsing;
using Serilog.Sinks.PeriodicBatching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Serilog.Sinks.InfluxDB
{
    internal class InfluxDBSink : PeriodicBatchingSink //InfluxDBSink
    {
        private readonly string _source;

        private readonly IFormatProvider _formatProvider;

        /// <summary>
        /// Connection info used to connect to InfluxDB instance.
        /// </summary>
        private readonly InfluxDBConnectionInfo _connectionInfo;

        /// <summary>
        /// Client object used to connect to InfluxDB instance.
        /// </summary>
        private readonly InfluxDbClient _influxDbClient;

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
        /// <param name="source">Measurement name in the InfluxDB database.</param>
        /// <param name="batchSizeLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <param name="formatProvider"></param>
        public InfluxDBSink(InfluxDBConnectionInfo connectionInfo, string source, int batchSizeLimit, TimeSpan period,
            IFormatProvider formatProvider)
            : base(batchSizeLimit, period)
        {
            _connectionInfo = connectionInfo ?? throw new ArgumentNullException(nameof(connectionInfo));
            _source = source;
            _influxDbClient = CreateInfluxDbClient();
            _formatProvider = formatProvider;

            CreateDatabase();
        }

        /// <inheritdoc />
        /// <summary>
        /// Emit a batch of log events, running asynchronously.
        /// </summary>
        /// <param name="events">The events to emit.</param>
        /// <remarks>Override either <see cref="M:Serilog.Sinks.PeriodicBatching.PeriodicBatchingSink.EmitBatch(System.Collections.Generic.IEnumerable{Serilog.Events.LogEvent})" /> or <see cref="M:Serilog.Sinks.PeriodicBatching.PeriodicBatchingSink.EmitBatchAsync(System.Collections.Generic.IEnumerable{Serilog.Events.LogEvent})" />,
        /// not both.</remarks>
        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            if (events == null) throw new ArgumentNullException(nameof(events));

            var logEvents = events as List<LogEvent> ?? events.ToList();
            var points = new List<Point>(logEvents.Count);

            foreach (var logEvent in FilteredSpecialChars(logEvents))
            {
                var p = new Point
                {
                    Name = _source,
                    Fields = logEvent.Properties.ToDictionary(k => k.Key, v => (object)v.Value),
                    Timestamp = logEvent.Timestamp.UtcDateTime
                };                

                // Add tags
                if (logEvent.Exception != null) p.Tags.Add("exceptionType", logEvent.Exception.GetType().Name);
                if (logEvent.MessageTemplate != null) p.Tags.Add("messageTemplate", logEvent.MessageTemplate.Text);

                p.Tags.Add("level", logEvent.Level.ToString());

                // Add rendered message
                p.Fields["message"] = logEvent.RenderMessage(_formatProvider);

                points.Add(p);
            }

            await _influxDbClient.Client.WriteAsync(points, _connectionInfo.DbName);
        }

        private IEnumerable<LogEvent> FilteredSpecialChars(IEnumerable<LogEvent> logEvents)
        {
            if (logEvents == null) yield break;

            foreach (var logEvent in logEvents)
            {
                if (logEvent.MessageTemplate != null)
                {
                    yield return new LogEvent(logEvent.Timestamp
                        , logEvent.Level
                        , logEvent.Exception
                        , StripSpecialCharacter(logEvent.MessageTemplate)
                        , logEvent?.Properties.Select(o => new LogEventProperty(o.Key, o.Value))
                        );
                }
                else
                {
                    yield return logEvent;
                }
            }
        }

        private MessageTemplate StripSpecialCharacter(MessageTemplate messageTemplate)
        {            
            var message = messageTemplate.Text != null ?
                messageTemplate.Text.Contains("HangFire:  - , Recurring job") ?
                    messageTemplate.Text.Split(new[] { ";Job:{" }, StringSplitOptions.RemoveEmptyEntries)?[0]
                    : HttpUtility.JavaScriptStringEncode(messageTemplate.Text)
                : string.Empty;
            return new MessageTemplate(message, new[] { messageTemplate.Tokens.FirstOrDefault() });                
        }

        /// <summary>
        /// Initialize and return an InfluxDB client object.
        /// </summary>
        /// <returns></returns>
        private InfluxDbClient CreateInfluxDbClient()
        {
            return new InfluxDbClient(
                $"{_connectionInfo.Address}:{_connectionInfo.Port}",
                _connectionInfo.Username,
                _connectionInfo.Password,
                InfluxDbVersion.Latest);
        }

        /// <summary>
        /// Create the log database in InfluxDB if it does not exists.
        /// </summary>
        private void CreateDatabase()
        {
            var dbList = _influxDbClient.Database.GetDatabasesAsync().Result;
            if (dbList.All(db => db.Name != _connectionInfo.DbName))
            {
                var _ = _influxDbClient.Database.CreateDatabaseAsync(_connectionInfo.DbName).Result;
            }
        }
    }
}
