using BenchmarkDotNet.Attributes;
using System.Diagnostics;

namespace Serilog.Sinks.InfluxDB.Tests;

public class InfluxAppenderBenchmarks : InfluxDBTestContainer
{
    [Params(1000)]
    public int N;

    [GlobalSetup(Targets = new[] { nameof(LogSomethingInfluxWithLayout), nameof(LogSomethingInfluxWithLayoutParameterized) })]
    public void SetupWithLayout()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.InfluxDB(new InfluxDBSinkOptions()
            {
                ApplicationName = "benchmark",
                InstanceName = "benchmarkInstance",
                ConnectionInfo = new InfluxDBConnectionInfo()
                {
                    AllAccessToken = AdminToken,
                    Uri = new Uri($"http://127.0.0.1:{Port}"),
                    BucketName = "test"
                },
                BatchOptions = new PeriodicBatching.PeriodicBatchingSinkOptions()
                {
                    BatchSizeLimit = 100
                }
            })
            .CreateLogger();
    }

    [Benchmark]
    public void LogSomethingInfluxWithLayout() => Log.Error("Error Console");

    [Benchmark]
    public void LogSomethingInfluxWithLayoutParameterized() => Log.Error("Error Console {N}", N);
}

public class InfluxAppenderBenchmarkTests
{
    [Fact]
    [Conditional("Release")]
    public void BenchmarkTest()
    {
#if !DEBUG
        var summary = BenchmarkDotNet.Running.BenchmarkRunner.Run<InfluxAppenderBenchmarks>();

        Assert.Empty(summary.ValidationErrors);
#endif
    }
}