// Copyright 2022 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Gameboard.Api.Extensions;
using Gameboard.Api.Structure;
using Microsoft.AspNetCore.Builder;

// expose internals for unit test mocking
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c547cac37abd99c8db225ef2f6c8a3602f3b3606cc9891605d02baa56104f4cfc0734aa39b93bf7852f7d9266654753cc297e7d2edfe0bac1cdcf9f717241550e0a7b191195b7667bb4f64bcb8e2121380fd1d9d46ad2d92d2d15605093924cceaf74c4861eff62abf69b9291ed0a340e113be11e6a7d3113e92484cf7045cc7")]

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
