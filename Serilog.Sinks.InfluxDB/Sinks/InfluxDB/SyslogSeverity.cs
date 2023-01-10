using Serilog.Events;

namespace Serilog.Sinks.InfluxDB;

internal enum SyslogSeverity
{
    emerg,
    alert,
    crit,
    err,
    warning,
    notice,
    info,
    debug
}

internal static class SyslogExtensions
{
    public static SyslogSeverity ToSeverity(this LogEventLevel logEventLevel)
    {
        return logEventLevel switch
        {
            LogEventLevel.Error => SyslogSeverity.err,
            LogEventLevel.Information => SyslogSeverity.info,
            LogEventLevel.Warning => SyslogSeverity.warning,
            LogEventLevel.Verbose => SyslogSeverity.debug,
            LogEventLevel.Debug => SyslogSeverity.debug,
            LogEventLevel.Fatal => SyslogSeverity.emerg,
            _ => SyslogSeverity.info
        };
    }
}