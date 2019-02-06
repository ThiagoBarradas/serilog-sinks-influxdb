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
            Port = InfluxDBDefaults.DefaultPort;
            DbName = InfluxDBDefaults.DefaultDbName;
        }

        /// <summary>
        /// Address of InfluxDB instance.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Gets or sets the port used for the connection.
        /// Default value is 8086.
        /// </summary>
        [DefaultValue(InfluxDBDefaults.DefaultPort)]
        public int Port { get; set; }

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