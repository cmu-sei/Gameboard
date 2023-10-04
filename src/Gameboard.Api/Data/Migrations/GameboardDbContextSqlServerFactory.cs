using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Gameboard.Api.Data.Migrations;

// this allows the creation/deletion of SQL Server migrations without requiring us to configure a connection
// string to an actual server every time (which mostly matters because we never use MSSQL)
//
// NOTE: per a Github issue (https://github.com/dotnet/efcore/issues/25053), this method does not enable 
// migration removal without a conn string. It's on EF's backlog.
public class GameboardDbContextSqlServerFactory : IDesignTimeDbContextFactory<GameboardDbContextSqlServer>
{
    private readonly IWebHostEnvironment _env = null;

    public GameboardDbContextSqlServerFactory() { }

    public GameboardDbContextSqlServerFactory(IWebHostEnvironment env)
    {
        _env = env;
    }

    public GameboardDbContextSqlServer CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<GameboardDbContextSqlServer>();
        optionsBuilder.UseSqlServer();

        return new GameboardDbContextSqlServer(optionsBuilder.Options, _env);
    }
}
