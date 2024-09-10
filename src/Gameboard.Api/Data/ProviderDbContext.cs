// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Data;

public class GameboardDbContextPostgreSQL(DbContextOptions options) : GameboardDbContext(options) { }
public class GameboardDbContextSqlServer(DbContextOptions options) : GameboardDbContext(options) { }

