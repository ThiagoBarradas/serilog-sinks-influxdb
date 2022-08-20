using InfluxDB.Client.Writes;
using Serilog.Events;
using Serilog.Parsing;
using System;
using System.Collections.Immutable;
using System.Reflection;
using Xunit;

namespace Serilog.Sinks.InfluxDB.Tests;

public class PointExtensionsTest
{

    [Fact]
    public void ValuesAreAddedToFields()
    {

        //Arrange
        var testProperty = "Test Property";

        var p = PointData.Builder.Measurement("test");

        LogEventProperty[] properties = { new LogEventProperty(testProperty, new ScalarValue("Stored Value")) };

        var le = new LogEvent(DateTime.Now, LogEventLevel.Error, new Exception(), new MessageTemplate("", Array.Empty<MessageTemplateToken>()), properties);

        string[] fields = { testProperty };

        //Act
        p.ExtendFields(le, fields);


        //Assert
        FieldInfo fi = typeof(PointData).GetField("_fields", BindingFlags.NonPublic | BindingFlags.Instance);

        var _fields = (ImmutableSortedDictionary<string, object>)fi.GetValue(p);

        Assert.Single(_fields);

        _fields.TryGetValue(testProperty, out var storedValue);

        Assert.Equal("\"Stored Value\"", storedValue);

    }

    [Fact]
    public void ValuesAreAddedToTags()
    {

        //Arrange
        var testProperty = "Test Property";

        var p = PointData.Builder.Measurement("test");

        LogEventProperty[] properties = { new LogEventProperty(testProperty, new ScalarValue("Stored Value")) };

        var le = new LogEvent(DateTime.Now, LogEventLevel.Error, new Exception(), new MessageTemplate("", Array.Empty<MessageTemplateToken>()), properties);

        string[] fields = { testProperty };

        //Act
        p = p.ExtendTags(le, fields);

        //Assert
        FieldInfo fi = typeof(PointData).GetField("_tags", BindingFlags.NonPublic | BindingFlags.Instance);

        var _tags = (ImmutableSortedDictionary<string, string>)fi.GetValue(p);

        Assert.Single(_tags);

        _tags.TryGetValue(testProperty, out var storedValue);

        Assert.Equal("\"Stored Value\"", storedValue);

    }



}