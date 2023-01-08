using InfluxDB.Client.Core.Flux.Domain;
using JetBrains.Annotations;

[PublicAPI]
public class QueryResult
{
    public QueryResult(FluxRecord record)
    {
        Table = record.Table;
        var values = record.Values;

        Time = ((NodaTime.Instant)values["_time"]).ToDateTimeUtc();
        Field = (string)values["_field"];
        Value = values["_value"];
        Tags = values
            .Skip(5)
            .SkipWhile(item => item.Key.StartsWith('_'))
            .ToDictionary(item => item.Key, item => item.Value?.ToString());
    }

    public DateTime Time { get; }
    public int Table { get; }
    public string Field { get; }
    public object Value { get; set; }
    public IDictionary<string, string?> Tags { get; }
}