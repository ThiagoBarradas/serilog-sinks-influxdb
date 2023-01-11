using System.ComponentModel;

namespace Serilog.Sinks.InfluxDB;

/// <summary>
/// Connection information for use by the InfluxDB sink.
/// </summary>
public class InfluxDBConnectionInfo
{
    /// <summary>
    /// Uri to influx db instance.
    /// </summary>
    public Uri? Uri { get; set; }

    /// <summary>
    /// Bucket name in InfluxDB.
    /// Default value is _monitoring.
    /// </summary>
    [DefaultValue(InfluxDBDefaults.DefaultBucketName)]
    public string BucketName { get; set; } = InfluxDBDefaults.DefaultBucketName;

    /// <summary>
    /// Organization Id (unique id can be found under Profile > About > Common Ids)
    /// </summary>
    public string? OrganizationId { get; set; }

    /// <summary>
    /// Token with at least write permissions to target <see cref="BucketName"/>
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Username at least write permissions to target <see cref="BucketName"/>
    /// Either use Token/AllAccessToken or Username/Password authentication credentials
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Password of<see cref="Username"/>
    /// Mandatory if UserName is use and no token provider
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Indicates if bucket should be created if not exists using super token to create it
    /// Then another token with only write permissions to <see cref="BucketName"/> is automatically created
    /// </summary>
    [DefaultValue(true)]
    public bool CreateBucketIfNotExists { get; set; } = true;

    /// <summary>
    /// Bucket Retention period (in seconds).
    /// </summary>
    public TimeSpan BucketRetentionPeriod { get; set; } = InfluxDBDefaults.DefaultRetentionPeriodInSeconds;

    /// <summary>
    /// Token to use to create bucket if needed and new write token when <see cref="CreateBucketIfNotExists"/> is set to true
    /// </summary>
    public string? AllAccessToken { get; set; }
}