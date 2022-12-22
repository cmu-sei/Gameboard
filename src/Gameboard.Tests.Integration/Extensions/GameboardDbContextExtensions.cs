using Gameboard.Api;
using Gameboard.Api.Data;

namespace Gameboard.Tests.Integration.Extensions;

public static class GameboardDbContextExtensions
{
    public static async Task<Api.Data.Game> CreateGame(this GameboardDbContext dbContext, Action<Api.Data.Game>? gameBuilder = null)
    {
        var game = new Api.Data.Game()
        {
            Id = Guid.NewGuid().ToString("n"),
            Name = "Test game",
            Competition = "Test competition",
            Season = "1",
            Track = "Individual",
            Sponsor = "Test Sponsor",
            GameStart = DateTimeOffset.UtcNow,
            GameEnd = DateTime.UtcNow + TimeSpan.FromDays(30),
            RegistrationOpen = DateTimeOffset.UtcNow,
            RegistrationClose = DateTime.UtcNow + TimeSpan.FromDays(30),
            RegistrationType = GameRegistrationType.Open
        };

        if (gameBuilder != null)
            gameBuilder(game);

        dbContext.Games.Add(game);
        await dbContext.SaveChangesAsync();

        return game;
    }

    //public static async Task<Api.Data.Player> CreatePlayer(this GameboardDbContext dbContext, Action<Api.Data.Player>? playerBuilder = null)
    //{
    //    var player = new Api.Data.Player
    //    {

    //    }
    //}

    public static async Task<Api.Data.User> CreateUser(this GameboardDbContext dbContext, UserRole role)
    {
        var user = new Api.Data.User()
        {
            Id = Guid.NewGuid().ToString("n"),
            Username = "integrationtester",
            Email = "integration@test.com",
            Name = "integrationtester",
            ApprovedName = "integrationtester",
            Sponsor = "SEI",
            Role = role
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        return user;
    }
}

