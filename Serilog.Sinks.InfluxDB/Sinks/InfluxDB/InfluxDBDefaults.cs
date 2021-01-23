namespace Serilog.Sinks.InfluxDB
{
    public class InfluxDBDefaults
    {
        /// <summary>
        /// Default port for InfluxDB
        /// </summary>
        public const int DefaultPort = 8086;

        /// <summary>
        /// Default bucket name in InfluxDB
        /// </summary>
        public const string DefaultBucketName = "_monitoring";

        /// <summary>
        /// Default retention period - 7days (in seconds)
        /// </summary>
        public const int DefaultRetentionPeriodInSeconds = 604_800;
    }
}
