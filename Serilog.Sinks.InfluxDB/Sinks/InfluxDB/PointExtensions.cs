using InfluxDB.Client.Writes;
using Serilog.Events;

namespace Serilog.Sinks.InfluxDB
{
    static class PointExtensions
    {

        public static PointData ExtendTags(this PointData p, Events.LogEvent logEvent, string[] tags)
        {

            foreach (var extendedTag in tags)
            {
                if (logEvent.Properties.ContainsKey(extendedTag))
                {
                    p = p.Tag(extendedTag, logEvent.Properties[extendedTag].ToString());
                }
            }
            return p;

        }

        public static PointData ExtendFields(this PointData p, Events.LogEvent logEvent, string[] fields)
        {

            foreach (var extendedField in fields)
            {
                if (logEvent.Properties.ContainsKey(extendedField))
                {
                    var sv = logEvent.Properties[extendedField] as ScalarValue;
                    switch (sv.Value)
                    {
                        case bool bl:
                            p = p.Field(extendedField, bl);
                            break;
                        case int i:
                            p = p.Field(extendedField, i);
                            break;
                        case double db:
                            p = p.Field(extendedField, db);
                            break;
                        case decimal dc:
                            p = p.Field(extendedField, dc);
                            break;
                        case long l:
                            p = p.Field(extendedField, l);
                            break;
                        case uint ui:
                            p = p.Field(extendedField, ui);
                            break;
                        case ulong u:
                            p = p.Field(extendedField, u);
                            break;
                        case byte b:
                            p = p.Field(extendedField, b);
                            break;
                        case null:
                        default:
                            p = p.Field(extendedField, logEvent.Properties[extendedField].ToString());
                            break;
                    }
                }
            }
            return p;
        }
    }
}
