using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Serilog.Sinks.InfluxDB;

namespace Serilog.Sinks.InfluxDb.Benchmarks
{
    public class SerilogInfluxDBBenchmarks
    {
        private ILogger log { get; set; }
        [Params(1000)]
        public int N;

        [GlobalSetup]
        public void Setup()
        {
            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.InfluxDB(new InfluxDBSinkOptions()
                {
                    ApplicationName = "benchmark",
                    InstanceName = "benchmarkInstance",
                    ConnectionInfo = new InfluxDBConnectionInfo()
                    {
                        Uri = new Uri("http://127.0.0.1:8086"),
                        BucketName = "logs",
                        OrganizationId = "88e1f5a5ad074d9e",  // Organization Id - unique id can be found under Profile > About > Common Ids
                        // To be set if bucket already created and give write permission and set CreateBucketIfNotExists to false
                        Token = null,
                        CreateBucketIfNotExists = true,
                        //To specify if Bucket needs to be created and if token not known or without all access permissions
                        AllAccessToken = "bGfBKhSycNiUOia4k7peib2jHFewkz3o6Hv2uz1xAoUcdnEFRW7cHn03KICySLemA4VPZKvc0CwzSQT8GNl2DA==",
                    },
                    BatchOptions = new PeriodicBatching.PeriodicBatchingSinkOptions()
                    {
                        BatchSizeLimit = 200
                    }
                });
            log = loggerConfig.CreateLogger();
        }

        public SerilogInfluxDBBenchmarks()
        {

        }

        [Benchmark]
        public void LogSomething() => log.Error($"Error {N}");
    }
    class Program
    {
        protected Program()
        { }
        static void Main(string[] args)
        {
            var loggerConfig = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.InfluxDB(
                        applicationName: "tests",
                        organizationId: "88e1f5a5ad074d9e",
                        bucketName:"logs",
                        uri : new Uri("http://127.0.0.1:8086"),
                        token: "bGfBKhSycNiUOia4k7peib2jHFewkz3o6Hv2uz1xAoUcdnEFRW7cHn03KICySLemA4VPZKvc0CwzSQT8GNl2DA==");
            var log = loggerConfig.CreateLogger();

            Console.WriteLine("Hello World!");
            log.Error("Error Console");
            log.Debug("Debug Console");

#if RELEASE
            var summary = BenchmarkRunner.Run<SerilogInfluxDBBenchmarks>();
#endif

            Console.WriteLine("Hello World!");
            log.Error("Error Console");
            log.Debug("Debug Console");
            log.Warning("Warn Console");
            log.Information("Info Console");

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}
