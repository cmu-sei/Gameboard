// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Data
{
    public class GameboardDbContextInMemory: GameboardDbContext
    {
        public GameboardDbContextInMemory(DbContextOptions<GameboardDbContextInMemory> options)
            : base(options)
        {
        }
    }

    public class GameboardDbContextSqlServer: GameboardDbContext
    {
        public GameboardDbContextSqlServer(DbContextOptions<GameboardDbContextSqlServer> options)
            : base(options)
        {
        }
    }

    public class GameboardDbContextPostgreSQL: GameboardDbContext
    {
        public GameboardDbContextPostgreSQL(DbContextOptions<GameboardDbContextPostgreSQL> options)
            : base(options)
        {
        }
    }
}
