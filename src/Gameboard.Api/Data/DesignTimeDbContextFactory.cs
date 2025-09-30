// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Gameboard.Api.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<GameboardDbContext>
{
    public GameboardDbContext CreateDbContext(string[] args)
    {
        var dbContextOptionsBuilder = new DbContextOptionsBuilder<GameboardDbContext>();
        dbContextOptionsBuilder.WithGameboardOptions(null, null);
        dbContextOptionsBuilder.UseNpgsql("Server=localhost;Port=5432;Database=gameboard;User ID=foundry;Password=foundry;");

        return new GameboardDbContext(dbContextOptionsBuilder.Options);
    }
}
