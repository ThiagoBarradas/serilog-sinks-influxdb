﻿using Serilog.Core;
using Serilog.Events;
using System.Runtime.CompilerServices;

namespace Serilog.Sinks.InfluxDB.Console.AppSettings
{
    public class ReleaseNumberEnricher : ILogEventEnricher
    {
        LogEventProperty _cachedProperty;

        public const string PropertyName = "ReleaseNumber";

        /// <summary>
        /// Enrich the log event.
        /// </summary>
        /// <param name="logEvent">The log event to enrich.</param>
        /// <param name="propertyFactory">Factory for creating new properties to add to the event.</param>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(GetLogEventProperty(propertyFactory));
        }

        private LogEventProperty GetLogEventProperty(ILogEventPropertyFactory propertyFactory)
        {
            // Don't care about thread-safety, in the worst case the field gets overwritten and one property will be GCed
            if (_cachedProperty == null)
                _cachedProperty = CreateProperty(propertyFactory);

            return _cachedProperty;
        }

        // Qualify as uncommon-path
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static LogEventProperty CreateProperty(ILogEventPropertyFactory propertyFactory)
        {
            var value = 12;
            return propertyFactory.CreateProperty(PropertyName, value);
        }
    }
}
