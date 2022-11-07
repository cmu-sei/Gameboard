// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Gameboard.Api.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Gameboard.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.Title = "Gameboard";

            LoadSettings();

            var hostBuilder = CreateHostBuilder(args)
                .Build()
                .InitializeDatabase();

            bool dbonly = args.ToList().Contains("--dbonly")
                || Environment.GetEnvironmentVariable("GAMEBOARD_DBONLY")?.ToLower() == "true";

            if (!dbonly)
            {
                try
                {
                    // Log.Information("Starting Gameboard...");
                    System.Diagnostics.Debug.WriteLine("Starting Gameboard...");
                    hostBuilder.Run();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Gameboard terminated unexpectedly: {ex.GetType().Name} - {ex.Message}");
                    // Log.Fatal($"Gameboard terminated unexpectedly: {ex.GetType().Name} - {ex.Message}");
                }
                finally
                {
                    // Log.CloseAndFlush();
                }
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                // .UseSerilog((ctx, cfg) =>
                // {
                //     cfg
                //         .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                //         .Enrich.FromLogContext()
                //         .Enrich.WithProperty("Application", ctx.HostingEnvironment.ApplicationName)
                //         .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName)
                //         .WriteTo.Console(new RenderedCompactJsonFormatter());
                // })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        public static void LoadSettings()
        {
            string envname = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            string path = Environment.GetEnvironmentVariable("APPSETTINGS_PATH") ?? "./conf/appsettings.conf";
            ConfToEnv("appsettings.conf");
            ConfToEnv($"appsettings.{envname}.conf");
            ConfToEnv(path);
        }

        public static void ConfToEnv(string conf)
        {
            if (!File.Exists(conf))
                return;

            try
            {
                foreach (string line in File.ReadAllLines(conf))
                {
                    if (
                        line.Equals(string.Empty)
                        || line.Trim().StartsWith("#")
                        || !line.Contains("=")
                    )
                    {
                        continue;
                    }

                    int x = line.IndexOf("=");

                    Environment.SetEnvironmentVariable(
                        line.Substring(0, x).Trim(),
                        line.Substring(x + 1).Trim()
                    );
                }
            }
            catch { }
        }
    }
}
