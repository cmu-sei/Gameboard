// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Data;

public class GameboardDbContextInMemory : GameboardDbContext
{
    public GameboardDbContextInMemory(IServiceProvider serviceProvider, DbContextOptions<GameboardDbContextInMemory> options, IWebHostEnvironment env)
        : base(serviceProvider, options, env) { }
}

public class GameboardDbContextSqlServer : GameboardDbContext
{
    public GameboardDbContextSqlServer(IServiceProvider serviceProvider, DbContextOptions<GameboardDbContextSqlServer> options, IWebHostEnvironment env)
        : base(serviceProvider, options, env) { }
}

public class GameboardDbContextPostgreSQL : GameboardDbContext
{
    public GameboardDbContextPostgreSQL(IServiceProvider serviceProvider, DbContextOptions<GameboardDbContextPostgreSQL> options, IWebHostEnvironment env)
        : base(serviceProvider, options, env) { }
}
