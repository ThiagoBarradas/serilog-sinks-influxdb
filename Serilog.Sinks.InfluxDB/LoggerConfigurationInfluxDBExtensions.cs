using Serilog.Configuration;
using Serilog.Events;
using Serilog.Sinks.InfluxDB;
using Serilog.Sinks.PeriodicBatching;
using System;

namespace Serilog
{
    /// <summary>
    /// Adds the WriteTo.InfluxDB() extension method to <see cref="LoggerConfiguration"/>.
    /// </summary>
    public static class LoggerConfigurationInfluxDBExtensions
    {
        public static LoggerConfiguration InfluxDB(
            this LoggerSinkConfiguration loggerConfiguration,
            string applicationName,
            string instanceName,
            string uriString,
            string dbName,
            string username = null,
            string password = null,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            int batchPostingLimit = InfluxDBSink.DefaultBatchPostingLimit,
            TimeSpan? period = null,
            IFormatProvider formatProvider = null)
        {
            if (string.IsNullOrEmpty(uriString)) throw new ArgumentNullException(nameof(uriString));
            if (!Uri.TryCreate(uriString, UriKind.Absolute, out var _)) throw new ArgumentException($"Invalid uri : {uriString}");

            return InfluxDB(loggerConfiguration, applicationName, instanceName, new Uri(uriString), dbName,
                username, password, restrictedToMinimumLevel, batchPostingLimit, period, formatProvider);
        }

        /// <summary>
        /// Adds the WriteTo.InfluxDB() extension method to <see cref="LoggerConfiguration"/>.
        /// </summary>
        public static LoggerConfiguration InfluxDB(
            this LoggerSinkConfiguration loggerConfiguration,
            string applicationName,
            string instanceName,
            Uri uri,
            string dbName,
            string username = null,
            string password = null,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            int batchPostingLimit = InfluxDBSink.DefaultBatchPostingLimit,
            TimeSpan? period = null,
            IFormatProvider formatProvider = null)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            if (loggerConfiguration == null) throw new ArgumentNullException(nameof(loggerConfiguration));
            if (string.IsNullOrEmpty(dbName)) throw new ArgumentException("dbName");

            var connectionInfo = new InfluxDBConnectionInfo
            {
                Uri = uri,
                DbName = dbName,
                Username = username,
                Password = password
            };

            return InfluxDB(loggerConfiguration, applicationName, instanceName, connectionInfo,
                restrictedToMinimumLevel, batchPostingLimit, period, formatProvider);
        }

        /// <summary>
        /// Adds the WriteTo.InfluxDB() extension method to <see cref="LoggerConfiguration"/>.
        /// </summary>
        public static LoggerConfiguration InfluxDB(
            this LoggerSinkConfiguration loggerConfiguration,
            string applicationName,
            string instanceName,
            InfluxDBConnectionInfo connectionInfo,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            int batchPostingLimit = InfluxDBSink.DefaultBatchPostingLimit,
            TimeSpan? period = null,
            IFormatProvider formatProvider = null)
        {
            if (applicationName == null) throw new ArgumentNullException(nameof(applicationName));
            if (connectionInfo == null) throw new ArgumentNullException(nameof(connectionInfo));
            if (connectionInfo.Uri == null) throw new ArgumentNullException(nameof(connectionInfo.Uri));
            if (connectionInfo.DbName == null) throw new ArgumentNullException(nameof(connectionInfo.DbName));
            if (connectionInfo.Username == null) connectionInfo.Username = string.Empty;
            if (connectionInfo.Password == null) connectionInfo.Password = string.Empty;

            var defaultedPeriod = period ?? InfluxDBSink.DefaultPeriod;

            return InfluxDB(loggerConfiguration, applicationName, instanceName, connectionInfo,
                new PeriodicBatchingSinkOptions() { BatchSizeLimit = batchPostingLimit, Period = defaultedPeriod },
                restrictedToMinimumLevel, formatProvider);
        }

        /// <summary>
        /// Adds the WriteTo.InfluxDB() extension method to <see cref="LoggerConfiguration"/>.
        /// </summary>
        public static LoggerConfiguration InfluxDB(
            this LoggerSinkConfiguration loggerConfiguration,
            string applicationName,
            string instanceName,
            InfluxDBConnectionInfo connectionInfo,
            PeriodicBatchingSinkOptions batchingOptions,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            IFormatProvider formatProvider = null)
        {
            if (applicationName == null) throw new ArgumentNullException(nameof(applicationName));
            if (connectionInfo == null) throw new ArgumentNullException(nameof(connectionInfo));
            if (connectionInfo.Uri == null) throw new ArgumentNullException(nameof(connectionInfo.Uri));
            if (connectionInfo.DbName == null) throw new ArgumentNullException(nameof(connectionInfo.DbName));
            if (connectionInfo.Username == null) connectionInfo.Username = string.Empty;
            if (connectionInfo.Password == null) connectionInfo.Password = string.Empty;
            if (batchingOptions == null) batchingOptions = new PeriodicBatchingSinkOptions();

            var influxDbSink = new InfluxDBSink(connectionInfo, applicationName, instanceName, formatProvider);
            var batchingSink = new PeriodicBatchingSink(influxDbSink, batchingOptions);

            return loggerConfiguration.Sink(batchingSink, restrictedToMinimumLevel);
        }
    }
}