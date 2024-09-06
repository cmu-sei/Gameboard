// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Data;

public class GameboardDbContextSqlServer : GameboardDbContext
{
    public GameboardDbContextSqlServer(IServiceProvider serviceProvider, DbContextOptions options, IHostEnvironment env)
        : base(serviceProvider, options, env) { }
}

public class GameboardDbContextPostgreSQL : GameboardDbContext
{
    public GameboardDbContextPostgreSQL(IServiceProvider serviceProvider, DbContextOptions options, IHostEnvironment env)
        : base(serviceProvider, options, env) { }
}
