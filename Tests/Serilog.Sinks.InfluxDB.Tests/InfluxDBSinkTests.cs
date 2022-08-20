using DockerComposeFixture;
using Serilog.Events;
using Serilog.Parsing;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Serilog.Sinks.InfluxDB.Tests;

public class InfluxDBSinkTests : IClassFixture<DockerFixture>
{
    public InfluxDBSinkTests(DockerFixture dockerFixture)
    {
        dockerFixture.InitOnce(() => new DockerFixtureOptions
        {
            DockerComposeFiles = new[] { "docker-compose-v2.yml" },
            //DockerComposeFiles = new[] { "docker-compose-testing-v2.yml" },
            CustomUpTest = output => output.Any(l => l.Contains("Welcome to InfluxDB"))
        });
    }


    public class GivenBucketDoesNotExists : InfluxDBSinkTests
    {
        private readonly InfluxDBSink _sut;

        public GivenBucketDoesNotExists(DockerFixture dockerFixture)
            : base(dockerFixture)
        {
            _sut = new InfluxDBSink(new InfluxDBSinkOptions()
            {
                ApplicationName = $"Test_{nameof(GivenBucketDoesNotExists)}",
                ConnectionInfo = new InfluxDBConnectionInfo()
                {
                    Uri = new Uri("http://127.0.0.1:8086"),
                    BucketName = "logs",
                    OrganizationId = "88e1f5a5ad074d9e",  // Organization Id - unique id can be found under Profile > About > Common Ids
                    CreateBucketIfNotExists = true,
                    //Username = "admin",
                    //Password = "admin",
                    AllAccessToken = "bGfBKhSycNiUOia4k7peib2jHFewkz3o6Hv2uz1xAoUcdnEFRW7cHn03KICySLemA4VPZKvc0CwzSQT8GNl2DA==",
                    BucketRetentionPeriod = TimeSpan.FromDays(1)
                },
            });
        }

        [Fact]
        public async Task ShouldCreateBucketIfCreateFlagIsSet()
        {
            var events = new[] {
                new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Information, null
                    , new MessageTemplate($"{nameof(ShouldCreateBucketIfCreateFlagIsSet)}", Enumerable.Empty<MessageTemplateToken>())
                    , Enumerable.Empty<LogEventProperty>())
            };

            await _sut.EmitBatchAsync(events);


        }
    }
}