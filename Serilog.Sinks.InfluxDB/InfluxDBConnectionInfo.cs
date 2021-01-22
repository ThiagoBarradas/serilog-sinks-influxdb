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
        /// Constructs the <see cref="InfluxDBConnectionInfo"/> with the default port and bucket name.
        /// </summary>
        public InfluxDBConnectionInfo()
        {
            BucketName = InfluxDBDefaults.DefaultBucketName;
            CreateBucketIfNotExists = true;
            BucketRetentionPeriodInSeconds = InfluxDBDefaults.DefaultRetentionPeriodInSeconds;
        }

        /// <summary>
        /// Uri to influx db instance.
        /// </summary>
        public Uri Uri { get; set; }

        /// <summary>
        /// Bucket name in InfluxDB.
        /// Default value is _monitoring.
        /// </summary>
        [DefaultValue(InfluxDBDefaults.DefaultBucketName)]
        public string BucketName { get; set; }

        /// <summary>
        /// Organization Id (unique id can be found under Profile > About > Common Ids)
        /// </summary>
        public string OrganizationId { get; set; }

        /// <summary>
        /// Token with at least write permissions to target <see cref="BucketName"/>
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Indicates if bucket should be created if not exists using super token to create it
        /// Then another token with only write permissions to <see cref="BucketName"/> is automatically created
        /// </summary>
        [DefaultValue(true)]
        public bool CreateBucketIfNotExists { get; set; }

        /// <summary>
        /// Bucket Retention period (in seconds).
        /// </summary>
        [DefaultValue(InfluxDBDefaults.DefaultRetentionPeriodInSeconds)]
        public int BucketRetentionPeriodInSeconds { get; set; }

        /// <summary>
        /// Token to use to create bucket if needed and new write token when <see cref="CreateBucketIfNotExists"/> is set to true
        /// </summary>
        public string AllAccessToken { get; set; }
    }
}