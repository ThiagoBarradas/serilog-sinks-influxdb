using Serilog.Configuration;
using System;

namespace Serilog.Sinks.InfluxDB.Console.AppSettings
{
    public static class LoggingExtensions
    {
        public static LoggerConfiguration WithReleaseNumber(
            this LoggerEnrichmentConfiguration enrich)
        {
            if (enrich is null)
                throw new ArgumentNullException(nameof(enrich));

            return enrich.With<ReleaseNumberEnricher>();
        }
    }
}
