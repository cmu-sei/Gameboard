using Gameboard.Api;
using Gameboard.Api.Data;

namespace Gameboard.Tests.Integration.Extensions;

internal static class GameboardTestContextExtensions
{
    public static async Task<Api.Data.User> CreateUser(this GameboardTestContext<Program, GameboardDbContextPostgreSQL> testContext, UserRole role, string username = "integrationtester", string id = "integrationtester")
    {
        var user = new Api.Data.User()
        {
            Id = id,
            Username = username,
            Email = "integration@test.com",
            Name = username,
            ApprovedName = username,
            Sponsor = "SEI",
            Role = role
        };

        using (var dbContext = testContext.GetDbContext())
        {
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
        }

        return user;
    }
}
