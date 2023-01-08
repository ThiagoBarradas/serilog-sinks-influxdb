using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sinks.InfluxDB.Console.AppSettings;

public class SourceContextMockEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SourceContext", "Debug"));
    }
}