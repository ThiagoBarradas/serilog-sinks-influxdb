﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Debugging;
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Serilog.Sinks.InfluxDB.Console.AppSettings
{
    class Program
    {
        public static IConfiguration Configuration { get; } = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddUserSecrets<Program>()
            .AddEnvironmentVariables()
            .Build();


        static async Task Main(string[] args)
        {
            await BuildCommandLine()
            .UseHost(_ => Host.CreateDefaultBuilder(),
            host =>
            {
                SelfLog.Enable(System.Console.Out);
                host.UseSerilog((hostingContext, loggerConfiguration) =>
                {
                    // could be configured here, but prefer earlier configuration
                    loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration);
                })
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.Sources.Clear();
                    config.AddConfiguration(Configuration);
                    var configuration = config.Build();

                    if(args is not null)
                    {
                        config.AddCommandLine(args);
                    }

                    if (args is null)
                    {
                    //add some defaults from config
                    var number = configuration.GetSection("Sample").GetValue<int>("number");
                        args = new string[0];
                        args = args.Append($"-n {number}").ToArray();
                    }

                    configuration = config.Build();
                })
                    .ConfigureServices((hostContext, services) =>
                    {
                        services.AddOptions<SampleOptions>()
                     .Bind(hostContext.Configuration.GetSection(SampleOptions.Sample));

                    })
                    .ConfigureLogging((hostContext, logging) =>
                    {
                        // nothing to do here as UseSerilog configures everything
                    }
                    );
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

            for (var i = 0; i < options.Number; ++i)
            {
                Log.Information("Hello, InfluxDB logger!");
                Log.Error("Error, InfluxDB logger!");
            }

            Log.CloseAndFlush();

            sw.Stop();

            System.Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds} ms");

            System.Console.WriteLine("Press any key to delete the temporary log file...");
            System.Console.ReadKey(true);
        }
    }
}
