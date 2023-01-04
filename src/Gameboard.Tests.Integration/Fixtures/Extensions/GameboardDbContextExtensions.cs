using Gameboard.Api.Data;

namespace Gameboard.Tests.Integration.Extensions;

public static class GameboardDbContextExtensions
{
    public static async Task<Api.Data.Game> CreateGame(this GameboardDbContext dbContext, GameBuilder? gameBuilder = null)
    {
        var game = GenerateGame(gameBuilder?.Configure);

        if (gameBuilder?.WithChallengeSpec == true)
        {
            var challengeSpec = GenerateChallengeSpec(game.Id, gameBuilder.ConfigureChallengeSpec);
            dbContext.ChallengeSpecs.Add(challengeSpec);
        }

        dbContext.Games.Add(game);
        await dbContext.SaveChangesAsync();

        return game;
    }

    public static async Task<Api.Data.Player> CreatePlayer(this GameboardDbContext dbContext, Action<Api.Data.Player>? playerBuilder = null, GameBuilder? gameBuilder = null, Action<Api.Data.User>? userBuilder = null)
    {
        var player = new Api.Data.Player
        {
            Id = TestIds.Generate(),
            TeamId = TestIds.Generate(),
            ApprovedName = "Integration Test Player",
            Sponsor = "Integration Test Sponsor",
            Role = Gameboard.Api.PlayerRole.Manager,
            User = GenerateUser(Gameboard.Api.UserRole.Member, userBuilder)
        };

        playerBuilder?.Invoke(player);

        // TODO: yuck
        if (string.IsNullOrWhiteSpace(player.GameId) && player.Game == null)
        {
            player.Game = await CreateGame(dbContext, gameBuilder);
        }

        dbContext.Players.Add(player);
        await dbContext.SaveChangesAsync();

        return player;
    }

    public static async Task<Api.Data.User> CreateUser(this GameboardDbContext dbContext, Gameboard.Api.UserRole role, Action<Api.Data.User>? userBuilder = null)
    {
        var user = GenerateUser(role, userBuilder);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        return user;
    }

    private static Api.Data.ChallengeSpec GenerateChallengeSpec(string gameId, Action<Api.Data.ChallengeSpec>? challengeSpecBuilder = null)
    {
        var spec = new Api.Data.ChallengeSpec
        {
            Id = TestIds.Generate(),
            GameId = gameId,
            Name = "Integration Test Challenge Spec",
            AverageDeploySeconds = 1,
            Points = 50,
            X = 0,
            Y = 0,
            R = 1
        };

        challengeSpecBuilder?.Invoke(spec);

        return spec;
    }

    private static Api.Data.Game GenerateGame(Action<Game>? gameBuilder = null)
    {
        var game = new Api.Data.Game()
        {
            Id = TestIds.Generate(),
            Name = "Test game",
            Competition = "Test competition",
            Season = "1",
            Track = "Individual",
            Sponsor = "Test Sponsor",
            GameStart = DateTimeOffset.UtcNow,
            GameEnd = DateTime.UtcNow + TimeSpan.FromDays(30),
            RegistrationOpen = DateTimeOffset.UtcNow,
            RegistrationClose = DateTime.UtcNow + TimeSpan.FromDays(30),
            RegistrationType = Gameboard.Api.GameRegistrationType.Open,
        };

        gameBuilder?.Invoke(game);

        return game;
    }

    private static Api.Data.User GenerateUser(Gameboard.Api.UserRole role, Action<Api.Data.User>? userBuilder = null)
    {
        var user = new Api.Data.User()
        {
            Id = TestIds.Generate(),
            Username = "integrationtester",
            Email = "integration@test.com",
            Name = "integrationtester",
            ApprovedName = "integrationtester",
            Sponsor = "SEI",
            Role = role
        };

        userBuilder?.Invoke(user);

        return user;
    }

    // private static IEnumerable<Api.Data.Player> GeneratePlayers
}

