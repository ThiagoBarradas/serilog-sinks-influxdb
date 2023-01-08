using Serilog.Events;
using Serilog.Parsing;

namespace Serilog.Sinks.InfluxDB.Tests;

public class InfluxDBSinkTests
{
    public class GivenBucketDoesNotExists : InfluxDBTestContainer
    {
        [Fact]
        public async Task ShouldCreateBucketIfCreateFlagIsSet()
        {
            var sut = new InfluxDBSink(new InfluxDBSinkOptions()
            {
                ApplicationName = $"Test_{nameof(GivenBucketDoesNotExists)}",
                ConnectionInfo = new InfluxDBConnectionInfo()
                {
                    Uri = new Uri($"http://127.0.0.1:{Port}"),
                    BucketName = "logs",
                    OrganizationId = DefaultBucket.OrgID,
                    CreateBucketIfNotExists = true,
                    AllAccessToken = AdminToken,
                    BucketRetentionPeriod = TimeSpan.FromDays(1)
                },
            });

            var events = new[] {
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
            var sut = new InfluxDBSink(new InfluxDBSinkOptions()
            {
                ApplicationName = $"Test_{nameof(GivenBucketDoesNotExists)}",
                ConnectionInfo = new InfluxDBConnectionInfo()
                {
                    Uri = new Uri($"http://127.0.0.1:{Port}"),
                    BucketName = "logs",
                    OrganizationId = DefaultBucket.OrgID,
                    CreateBucketIfNotExists = false,
                    AllAccessToken = AdminToken,
                    BucketRetentionPeriod = TimeSpan.FromDays(1)
                },
            });

            var events = new[] {
                new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Information, null
                    , new MessageTemplate($"{nameof(ShouldCreateBucketIfCreateFlagIsSet)}", Enumerable.Empty<MessageTemplateToken>())
                    , Enumerable.Empty<LogEventProperty>())
            };

            await Assert.ThrowsAsync<global::InfluxDB.Client.Core.Exceptions.NotFoundException>(() => sut.EmitBatchAsync(events));
        }
    }
}