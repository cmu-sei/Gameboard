// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Extensions;
using Gameboard.Api.Structure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

// expose internals for unit test mocking
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c547cac37abd99c8db225ef2f6c8a3602f3b3606cc9891605d02baa56104f4cfc0734aa39b93bf7852f7d9266654753cc297e7d2edfe0bac1cdcf9f717241550e0a7b191195b7667bb4f64bcb8e2121380fd1d9d46ad2d92d2d15605093924cceaf74c4861eff62abf69b9291ed0a340e113be11e6a7d3113e92484cf7045cc7")]

// set logging properties
Console.Title = "Gameboard";
var startupLogger = new StartupLogger("Startup", () => new ColorConsoleLoggerConfiguration());
startupLogger.LogInformation("Welcome to Gameboard!");

// load and resolve settings
startupLogger.LogInformation("Configuring Gameboard app...");
var envname = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
var appSettingsPath = Environment.GetEnvironmentVariable("APPSETTINGS_PATH") ?? "./conf/appsettings.conf";
ConfToEnv.Load("appsettings.conf");
ConfToEnv.Load($"appsettings.{envname}.conf");
ConfToEnv.Load(appSettingsPath);

startupLogger.LogInformation($"Starting Gameboard in {envname} configuration.");

// create an application builder
var builder = WebApplication.CreateBuilder(args);

// load settings and configure services
var settings = builder.BuildAppSettings(startupLogger);
builder.ConfigureServices(settings);

// launch db if db only 
var dbOnly = args.ToList().Contains("--dbonly")
    || Environment.GetEnvironmentVariable("GAMEBOARD_DBONLY")?.ToLower() == "true";

if (dbOnly)
{
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();

    var dbOnlyApp = builder.Build();
    dbOnlyApp.Logger.LogInformation("Starting the app in dbonly mode...");
    dbOnlyApp.Logger.LogInformation($"Connection string: ", settings.Database.ConnectionString);
    dbOnlyApp.InitializeDatabase(settings, dbOnlyApp.Logger);
    dbOnlyApp.Logger.LogInformation("DB initialized.");

    return;
}

// build and configure app
var app = builder.Build();
app
    .InitializeDatabase(settings, app.Logger)
    .ConfigureGameboard(settings);

// start!
startupLogger.LogInformation("Let the games begin!");
app.Run();

// required for integration tests
public partial class Program { }
