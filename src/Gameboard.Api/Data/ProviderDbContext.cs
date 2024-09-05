// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Data;

public class GameboardDbContextInMemory(IServiceProvider serviceProvider, DbContextOptions<GameboardDbContext> options, IWebHostEnvironment env)
    : GameboardDbContext(serviceProvider, options, env)
{ }

public class GameboardDbContextSqlServer(IServiceProvider serviceProvider, DbContextOptions<GameboardDbContext> options, IWebHostEnvironment env)
    : GameboardDbContext(serviceProvider, options, env)
{ }

public class GameboardDbContextPostgreSQL(IServiceProvider serviceProvider, DbContextOptions<GameboardDbContext> options, IWebHostEnvironment env)
    : GameboardDbContext(serviceProvider, options, env)
{ }
