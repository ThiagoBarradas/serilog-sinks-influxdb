﻿using InfluxDB.Client.Core.Exceptions;
using Serilog.Events;
using Serilog.Parsing;
using Serilog.Sinks.InfluxDB.Console.AppSettings;

namespace Serilog.Sinks.InfluxDB.Tests;

public static class InfluxDBSinkTests
{
    public class GivenBucketDoesNotExists : InfluxDBTestContainer
    {
        [Fact]
        public async Task ShouldCreateBucketIfCreateFlagIsSet()
        {
            var sut = new InfluxDBSink(new InfluxDBSinkOptions
            {
                ApplicationName = $"Test_{nameof(GivenBucketDoesNotExists)}",
                ConnectionInfo = new InfluxDBConnectionInfo
                {
                    Uri = new Uri($"http://127.0.0.1:{Port}"),
                    BucketName = "logs",
                    OrganizationId = DefaultBucket.OrgID,
                    CreateBucketIfNotExists = true,
                    AllAccessToken = AdminToken,
                    BucketRetentionPeriod = TimeSpan.FromDays(1)
                },
            });

            var events = new[]
            {
                new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Information, null
                    , new MessageTemplate($"{nameof(ShouldCreateBucketIfCreateFlagIsSet)}", Enumerable.Empty<MessageTemplateToken>())
                    , Enumerable.Empty<LogEventProperty>())
            };

            await sut.EmitBatchAsync(events);

            var bucketsApi = InfluxDBClient.GetBucketsApi();

            var bucket = await bucketsApi.FindBucketByNameAsync("logs");

            Assert.NotNull(bucket);
        }

        [Fact]
        public async Task DoesNotCreateBucketIfCreateFlagIsNotSet()
        {
            var sut = new InfluxDBSink(new InfluxDBSinkOptions
            {
                ApplicationName = $"Test_{nameof(GivenBucketDoesNotExists)}",
                ConnectionInfo = new InfluxDBConnectionInfo
                {
                    Uri = new Uri($"http://127.0.0.1:{Port}"),
                    BucketName = "logs",
                    OrganizationId = DefaultBucket.OrgID,
                    CreateBucketIfNotExists = false,
                    AllAccessToken = AdminToken,
                    BucketRetentionPeriod = TimeSpan.FromDays(1)
                },
            });

            var events = new[]
            {
                new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Information, null
                    , new MessageTemplate($"{nameof(ShouldCreateBucketIfCreateFlagIsSet)}", Enumerable.Empty<MessageTemplateToken>())
                    , Enumerable.Empty<LogEventProperty>())
            };

            await Assert.ThrowsAsync<NotFoundException>(() => sut.EmitBatchAsync(events));
        }
    }

    [UsesVerify]
    public class GivenSpecificConfigurations : InfluxDBTestContainer
    {
        [Fact]
        public async Task ApplicationAndFacilityAreLoggedIfOnlyApplicationIsConfigured()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.InfluxDB(new InfluxDBSinkOptions
                {
                    ApplicationName = "TestApplication",
                    ConnectionInfo = ConnectionInfo,
                })
                .CreateLogger();

            Log.Warning("Some warning {Parameter}", "Some parameter");

            await Log.CloseAndFlushAsync();

            await Verify(GetAllRowsAsync());
        }

        [Fact]
        public async Task OnlyApplicationIsLoggedIfApplicationIsConfiguredAndInstanceIsAnEmptyString()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.InfluxDB(new InfluxDBSinkOptions
                {
                    ApplicationName = "TestApplication",
                    InstanceName = string.Empty,
                    ConnectionInfo = ConnectionInfo,
                })
                .CreateLogger();

            Log.Warning("Some warning {Parameter}", "Some parameter");

            await Log.CloseAndFlushAsync();

            await Verify(GetAllRowsAsync());
        }

        [Fact]
        public async Task NoApplicationIsLoggedIfNotConfigured()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.InfluxDB(new InfluxDBSinkOptions { ConnectionInfo = ConnectionInfo })
                .CreateLogger();

            Log.Warning("Some warning {Parameter}", "Some parameter");

            await Log.CloseAndFlushAsync();

            await Verify(GetAllRowsAsync());
        }

        [Fact]
        public async Task ExtendedFieldIsLogged()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.With<SourceContextMockEnricher>()
                .WriteTo.InfluxDB(new InfluxDBSinkOptions
                {
                    ConnectionInfo = ConnectionInfo,
                    ExtendedFields = new[] { "SourceContext" }
                })
                .CreateLogger();

            Log.Warning("Some warning {Parameter}", "Some parameter");

            await Log.CloseAndFlushAsync();

            await Verify(GetAllRowsAsync());
        }

        [Fact]
        public async Task ExtendedFieldIsLoggedWithMappedName()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.With<SourceContextMockEnricher>()
                .WriteTo.InfluxDB(new InfluxDBSinkOptions
                {
                    ConnectionInfo = ConnectionInfo,
                    ExtendedFields = new[] { "SourceContext:context" }
                })
                .CreateLogger();

            Log.Warning("Some warning {Parameter}", "Some parameter");

            await Log.CloseAndFlushAsync();

            await Verify(GetAllRowsAsync());
        }

        [Fact]
        public async Task ExtendedTagIsLogged()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.With<SourceContextMockEnricher>()
                .WriteTo.InfluxDB(new InfluxDBSinkOptions
                {
                    ConnectionInfo = ConnectionInfo,
                    ExtendedTags = new[] { "SourceContext" }
                })
                .CreateLogger();

            Log.Warning("Some warning {Parameter}", "Some parameter");

            await Log.CloseAndFlushAsync();

            await Verify(GetAllRowsAsync());
        }

        [Fact]
        public async Task ExtendedTagIsLoggedWithMappedName()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.With<SourceContextMockEnricher>()
                .WriteTo.InfluxDB(new InfluxDBSinkOptions
                {
                    ConnectionInfo = ConnectionInfo,
                    ExtendedTags = new[] { "SourceContext:context" }
                })
                .CreateLogger();

            Log.Warning("Some warning {Parameter}", "Some parameter");

            await Log.CloseAndFlushAsync();

            await Verify(GetAllRowsAsync());
        }
    }

    [UsesVerify]
    public class GivenVariousInputData : InfluxDBTestContainer
    {
        [Fact]
        public async Task OnlyNeededCharactersAreEscaped()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.InfluxDB(new InfluxDBSinkOptions { ConnectionInfo = ConnectionInfo })
                .CreateLogger();

            Log.Information("quoted: '{Parameter}' `back-quotes` CRLF:\r\n LF:\n CR:\r TAB:\t \"double-quotes\" BS:\\r äöüß. EOL", "Some parameter");

            await Log.CloseAndFlushAsync();

            await Verify(GetAllRowsAsync());
        }
    }
}