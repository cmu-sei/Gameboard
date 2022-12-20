// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using Gameboard.Api.Extensions;
using Gameboard.Api.Structure;
using Microsoft.AspNetCore.Builder;

// set logging properties
Console.Title = "Gameboard";

// load and resolve settings
var envname = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
var path = Environment.GetEnvironmentVariable("APPSETTINGS_PATH") ?? "./conf/appsettings.conf";
ConfToEnv.Load("appsettings.conf");
ConfToEnv.Load($"appsettings.{envname}.conf");
ConfToEnv.Load(path);

// create an application builder
var builder = WebApplication.CreateBuilder(args);

// launch db if db only
var dbOnly = args.ToList().Contains("--dbonly")
    || Environment.GetEnvironmentVariable("GAMEBOARD_DBONLY")?.ToLower() == "true";

if (dbOnly)
{
    builder
        .Build()
        .InitializeDatabase();
}
else
{
    Console.WriteLine("Configuring Gameboard app...");

    // load settings and configure services
    var settings = builder.BuildAppSettings();
    builder.ConfigureServices(settings);

    // build and configure app
    var app = builder
        .Build()
        .InitializeDatabase()
        .ConfigureGameboard(settings);

    // start!
    app.Run();
}

// required for integration tests
public partial class Program { }
