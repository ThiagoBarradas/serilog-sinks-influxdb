using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Serilog.Sinks.InfluxDB;

namespace Serilog.Sinks.InfluxDb.Benchmarks;

public class SerilogInfluxDBBenchmarks
{
    private ILogger Log { get; set; } = default!;

    private IAsyncLifetime? _testContainer;

    [Params(100)]
    public int N;

    [GlobalSetup]
    public async Task Setup()
    {
        var testContainer = new InfluxDBTestContainer();

        _testContainer = testContainer;

        await _testContainer.InitializeAsync();

        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.InfluxDB(new InfluxDBSinkOptions()
            {
                ApplicationName = "benchmark",
                InstanceName = "benchmarkInstance",
                ConnectionInfo = new InfluxDBConnectionInfo()
                {
                    Uri = new Uri($"http://127.0.0.1:{testContainer.Port}"),
                    BucketName = "logs",
                    OrganizationId = testContainer.DefaultBucket.OrgID,
                    CreateBucketIfNotExists = true,
                    AllAccessToken = InfluxDBTestContainer.AdminToken,
                },
                BatchOptions = new PeriodicBatching.PeriodicBatchingSinkOptions()
                {
                    BatchSizeLimit = 200
                }
            });
        Log = loggerConfig.CreateLogger();
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        var testContainer = _testContainer;
        if (testContainer != null)
        {
            await testContainer.DisposeAsync();
        }
    }

    [Benchmark]
    public void LogSomething() => Log.Error("Error {N}", N);
}

static class Program
{
    static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<SerilogInfluxDBBenchmarks>();
    }
}