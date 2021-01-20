﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.InfluxDB.Console.Console.FluentConfig;
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Linq;
using System.Threading.Tasks;

namespace Serilog.Sinks.InfluxDB.Console.FluentConfig
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
               .MinimumLevel.Information()
               .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
               .Enrich.FromLogContext()
               .WriteTo.InfluxDB("Test App"      // Application Name
                    , "Test Instance"            // Instance or Environment Name
                    , "http://localhost:8086"    // InfluxDb Address
                    , "_internal")               // InfluxDb Database Name)
               .CreateLogger();

            await BuildCommandLine()
            .UseHost(_ => Host.CreateDefaultBuilder(),
            host =>
            {
                SelfLog.Enable(System.Console.Out);
                host.UseSerilog((hostingContext, loggerConfiguration) =>
                {
                    // could be configured here, but prefer earlier configuration
                })
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    // The next 2 lines commented out as they are added by default in the correct order
                    // for more control first call config.Sources.Clear();
                    //config.AddJsonFile("appsettings.json", optional: true);
                    //config.AddEnvironmentVariables();
                    config.AddUserSecrets<Program>();
                    var configuration = config.Build();

                    if (args is null)
                    {
                        //add some defaults from config
                        var number = configuration.GetSection("Sample").GetValue<int>("number");
                        args = new string[0];
                        args = args.Append($"-n {number}").ToArray();
                    }

                    config.AddCommandLine(args);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions<SampleOptions>()
                 .Bind(hostContext.Configuration.GetSection(SampleOptions.Sample));

                });
            })
            .UseDefaults()
            .Build()
            .InvokeAsync(args);
        }

        private static CommandLineBuilder BuildCommandLine()
        {
            var root = new RootCommand(@"$ TestConsole.exe --number 10"){
                new Option<int>(aliases: new string[] { "--number", "-n" }){
                    Description = "Number of log entries to create",
                    IsRequired = false
                },
            };
            root.Handler = CommandHandler.Create<SampleOptions, IHost>(Run);
            return new CommandLineBuilder(root);
        }

        private static void Run(SampleOptions options, IHost host)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            Log.Logger = new LoggerConfiguration()
                .WriteTo.InfluxDB("fluentSample", "fluentSampleInstance", new InfluxDBConnectionInfo()
                {
                    Uri = new Uri("http://127.0.0.1:8086"),
                    DbName = "_internal",
                })
                .CreateLogger();

            for (var i = 0; i < options.Number; ++i)
            {
                Log.Information("Hello, InfluxDB logger!");
                Log.Error("Error, InfluxDB logger!");
            }

            Log.CloseAndFlush();

            sw.Stop();

            System.Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds} ms");
            System.Console.WriteLine("Press any key to exit...");

            System.Console.ReadKey(true);
        }
    }
}
