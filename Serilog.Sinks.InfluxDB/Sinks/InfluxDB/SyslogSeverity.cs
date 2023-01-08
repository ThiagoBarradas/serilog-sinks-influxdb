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
        switch (logEventLevel)
        {
            case LogEventLevel.Error: return SyslogSeverity.err;
            case LogEventLevel.Information: return SyslogSeverity.info;
            case LogEventLevel.Warning: return SyslogSeverity.warning;
            case LogEventLevel.Verbose: return SyslogSeverity.debug;
            case LogEventLevel.Debug: return SyslogSeverity.debug;
            case LogEventLevel.Fatal: return SyslogSeverity.emerg;
            default:
                return SyslogSeverity.info;
        }
    }
}