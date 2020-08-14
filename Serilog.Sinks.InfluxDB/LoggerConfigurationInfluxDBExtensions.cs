using Serilog.Configuration;
using Serilog.Events;
using Serilog.Sinks.InfluxDB;
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
            string source,
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
            
            return InfluxDB(loggerConfiguration, source, new Uri(uriString), dbName, username, password, restrictedToMinimumLevel, batchPostingLimit, period, formatProvider);
        }

        /// <summary>
        /// Adds the WriteTo.InfluxDB() extension method to <see cref="LoggerConfiguration"/>.
        /// </summary>
        public static LoggerConfiguration InfluxDB(
            this LoggerSinkConfiguration loggerConfiguration,
            string source,
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

            return InfluxDB(loggerConfiguration, source, connectionInfo, restrictedToMinimumLevel, batchPostingLimit, period, formatProvider);
        }

        /// <summary>
        /// Adds the WriteTo.InfluxDB() extension method to <see cref="LoggerConfiguration"/>.
        /// </summary>
        public static LoggerConfiguration InfluxDB(
            this LoggerSinkConfiguration loggerConfiguration,
            string source,
            InfluxDBConnectionInfo connectionInfo,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            int batchPostingLimit = InfluxDBSink.DefaultBatchPostingLimit,
            TimeSpan? period = null,
            IFormatProvider formatProvider = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (connectionInfo == null) throw new ArgumentNullException(nameof(connectionInfo));
            if (connectionInfo.Uri == null) throw new ArgumentNullException(nameof(connectionInfo.Uri));
            if (connectionInfo.DbName == null) throw new ArgumentNullException(nameof(connectionInfo.DbName));
            if (connectionInfo.Username == null) connectionInfo.Username = string.Empty;
            if (connectionInfo.Password == null) connectionInfo.Password = string.Empty;

            var defaultedPeriod = period ?? InfluxDBSink.DefaultPeriod;

            return loggerConfiguration.Sink(new InfluxDBSink(connectionInfo, source, batchPostingLimit, defaultedPeriod, formatProvider), restrictedToMinimumLevel);
        }
    }
}