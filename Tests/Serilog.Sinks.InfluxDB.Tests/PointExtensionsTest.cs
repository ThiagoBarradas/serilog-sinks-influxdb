using InfluxDB.Client.Writes;
using Serilog.Events;
using Serilog.Parsing;
using System.Globalization;
using System.Reflection;

namespace Serilog.Sinks.InfluxDB.Tests;

public class PointExtensionsTest
{
    [Theory]
    [InlineData("Stored Value")]
    [InlineData(42L)]
    [InlineData(Math.PI)]
    public void ValuesAreAddedToFields(object value)
    {
        //Arrange
        var testProperty = "Test Property";

        var p = PointData.Builder.Measurement("test");

        LogEventProperty[] properties = { new(testProperty, new ScalarValue(value)) };

        var le = new LogEvent(DateTime.Now, LogEventLevel.Error, new Exception(), new MessageTemplate("", Array.Empty<MessageTemplateToken>()), properties);

        string[] fieldNames = { testProperty };

        //Act
        p.ExtendFields(le, fieldNames);

        //Assert
        var fi = p.GetType().GetField("_fields", BindingFlags.NonPublic | BindingFlags.Instance);

        var fields = (IDictionary<string, object>?)fi?.GetValue(p);

        Assert.NotNull(fields);
        Assert.Single(fields);

        fields.TryGetValue(testProperty, out var storedValue);

        Assert.Equal(value, storedValue);
    }

    [Theory]
    [InlineData("Stored Value")]
    [InlineData(42L)]
    [InlineData(Math.PI)]
    public void ValuesAreAddedToTags(object value)
    {
        //Arrange
        var testProperty = "Test Property";

        var p = PointData.Builder.Measurement("test");

        LogEventProperty[] properties = { new(testProperty, new ScalarValue(value)) };

        var le = new LogEvent(DateTime.Now, LogEventLevel.Error, new Exception(), new MessageTemplate("", Array.Empty<MessageTemplateToken>()), properties);

        string[] tagNames = { testProperty };

        //Act
        p.ExtendTags(le, tagNames);

        //Assert
        var fi = p.GetType().GetField("_tags", BindingFlags.NonPublic | BindingFlags.Instance);

        var tags = (IDictionary<string, string>?)fi?.GetValue(p);

        Assert.NotNull(tags);
        Assert.Single(tags);

        tags.TryGetValue(testProperty, out var storedValue);

        Assert.Equal(Convert.ToString(value, CultureInfo.InvariantCulture), storedValue);
    }
}