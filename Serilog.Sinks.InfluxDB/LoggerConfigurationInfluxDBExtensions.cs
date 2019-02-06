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
            string address,
            int port,
            string dbName,
            string username,
            string password,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            int batchPostingLimit = InfluxDBSink.DefaultBatchPostingLimit,
            TimeSpan? period = null,
            IFormatProvider formatProvider = null)
        {
            if (loggerConfiguration == null) throw new ArgumentNullException(nameof(loggerConfiguration));
            if (string.IsNullOrEmpty(address)) throw new ArgumentNullException(nameof(address));
            if (port <= 0) throw new ArgumentException("port");
            if (string.IsNullOrEmpty(dbName)) throw new ArgumentException("dbName");

            var connectionInfo = new InfluxDBConnectionInfo
            {
                Address = address,
                Port = port,
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
            string address,
            string dbName,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            int batchPostingLimit = InfluxDBSink.DefaultBatchPostingLimit,
            TimeSpan? period = null,
            IFormatProvider formatProvider = null)
        {
            var connectionInfo = new InfluxDBConnectionInfo
            {
                Address = address,
                Port = InfluxDBDefaults.DefaultPort,
                DbName = dbName,
                Username = string.Empty,
                Password = string.Empty
            };

            return InfluxDB(loggerConfiguration, source, connectionInfo, restrictedToMinimumLevel, batchPostingLimit, period, formatProvider);
        }

        /// <summary>
        /// Adds the WriteTo.InfluxDB() extension method to <see cref="LoggerConfiguration"/>.
        /// </summary>
        public static LoggerConfiguration InfluxDB(
            this LoggerSinkConfiguration loggerConfiguration,
            string source,
            string address,
            string dbName,
            string username,
            string password,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            int batchPostingLimit = InfluxDBSink.DefaultBatchPostingLimit,
            TimeSpan? period = null,
            IFormatProvider formatProvider = null)
        {
            var connectionInfo = new InfluxDBConnectionInfo
            {
                Address = address,
                Port = InfluxDBDefaults.DefaultPort,
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
            if (connectionInfo == null) throw new ArgumentNullException(nameof(connectionInfo));

            var defaultedPeriod = period ?? InfluxDBSink.DefaultPeriod;

            return loggerConfiguration.Sink(new InfluxDBSink(connectionInfo, source, batchPostingLimit, defaultedPeriod, formatProvider), restrictedToMinimumLevel);
        }
    }
}