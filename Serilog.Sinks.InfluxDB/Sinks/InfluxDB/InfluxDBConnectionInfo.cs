using System;
using System.ComponentModel;

namespace Serilog.Sinks.InfluxDB
{
    /// <summary>
    /// Connection information for use by the InfluxDB sink.
    /// </summary>
    public class InfluxDBConnectionInfo
    {
        /// <summary>
        /// Constructs the <see cref="InfluxDBConnectionInfo"/> with the default port and database name.
        /// </summary>
        public InfluxDBConnectionInfo()
        {
            DbName = InfluxDBDefaults.DefaultDbName;
        }

        /// <summary>
        /// Uri to influx db instance.
        /// </summary>
        public Uri Uri { get; set; }

        /// <summary>
        /// Gets or sets the database name in InfluxDB.
        /// Default value is logDb.
        /// </summary>
        [DefaultValue(InfluxDBDefaults.DefaultDbName)]
        public string DbName { get; set; }

        /// <summary>
        /// Gets or sets the username used for authentication.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the password used for authentication.
        /// </summary>
        public string Password { get; set; }
    }
}