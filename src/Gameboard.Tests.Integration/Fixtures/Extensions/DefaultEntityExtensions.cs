using Gameboard.Api.Data;

namespace Gameboard.Tests.Integration.Fixtures;

public static class GameboardTestContextDefaultEntityExtensions
{
    private static T BuildEntity<T>(T entity, Action<T>? builder) where T : class, IEntity
    {
        builder?.Invoke(entity);
        return entity;
    }

    public static void AddChallengeSpec(this IDataStateBuilder dataStateBuilder, Action<ChallengeSpec>? specBuilder = null)
        => dataStateBuilder.Add(BuildChallengeSpec(dataStateBuilder, specBuilder));

    public static ChallengeSpec BuildChallengeSpec(this IDataStateBuilder dataStateBuilder, Action<ChallengeSpec>? specBuilder = null)
        => BuildEntity
        (
            new ChallengeSpec
            {
                Id = TestIds.Generate(),
                Game = BuildGame(dataStateBuilder),
                Name = "Integration Test Challenge Spec",
                AverageDeploySeconds = 1,
                Points = 50,
                X = 0,
                Y = 0,
                R = 1
            },
            specBuilder
        );

    public static Challenge BuildChallenge(this IDataStateBuilder dataStateBuilder, Action<Challenge>? challengeBuilder = null)
        => BuildEntity
        (
            new Challenge
            {
                Id = TestIds.Generate(),
                Name = "Integration Test Challenge",
            },
            challengeBuilder
        );

    public static void AddGame(this IDataStateBuilder dataStateBuilder, Action<Game>? gameBuilder = null)
        => dataStateBuilder.Add(BuildGame(dataStateBuilder, gameBuilder));

    public static Game BuildGame(this IDataStateBuilder dataStateBuilder, Action<Game>? gameBuilder = null)
        => BuildEntity
        (
            new Game
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
            },
            gameBuilder
        );

    public static void AddPlayer(this IDataStateBuilder dataStateBuilder, Action<Player>? playerBuilder = null)
        => dataStateBuilder.Add(BuildPlayer(dataStateBuilder, playerBuilder));

    public static Player BuildPlayer(this IDataStateBuilder dataStateBuilder, Action<Player>? playerBuilder = null)
        => BuildEntity
        (
            new Player
            {
                Id = TestIds.Generate(),
                TeamId = TestIds.Generate(),
                ApprovedName = "Integration Test Player",
                Sponsor = "Integration Test Sponsor",
                Role = Gameboard.Api.PlayerRole.Manager,
                Game = BuildGame(dataStateBuilder),
                User = BuildUser(dataStateBuilder)
            },
            playerBuilder
        );

    public static TeamBuilderResult AddTeam(this IDataStateBuilder dataStateBuilder, IFixture fixture, Action<TeamBuilderOptions> optsBuilder)
    {
        var options = new TeamBuilderOptions
        {
            Name = fixture.Create<string>(),
            NumPlayers = 5,
            GameBuilder = g => { },
            TeamId = fixture.Create<string>()
        };

        optsBuilder.Invoke(options);

        // fill out properties
        var teamName = string.IsNullOrEmpty(options.Name) ? fixture.Create<string>() : options.Name;

        var game = new Api.Data.Game
        {
            Id = fixture.Create<string>(),
            // just to avoid obnoxious overconfig on the other end
            RegistrationClose = DateTimeOffset.UtcNow.AddYears(1),
            RegistrationOpen = DateTimeOffset.UtcNow.AddYears(-1),
            RegistrationType = Api.GameRegistrationType.Open
        };

        options.GameBuilder?.Invoke(game);

        var challenge = new Api.Data.Challenge
        {
            Id = fixture.Create<string>(),
            Game = game,
            TeamId = options.TeamId
        };

        // create players
        var players = new List<Player>();

        for (var i = 0; i < options.NumPlayers; i++)
        {
            var createManager = i == 0;
            var player = new Player
            {
                Id = fixture.Create<string>(),
                ApprovedName = teamName,
                Name = teamName,
                Role = createManager ? Api.PlayerRole.Manager : Api.PlayerRole.Member,
                TeamId = options.TeamId,
                User = new User { Id = fixture.Create<string>() },
                Challenges = new List<Api.Data.Challenge> { challenge },
                Game = game
            };

            players.Add(player);
        }

        // Add entities
        dataStateBuilder.AddRange(players);

        return new TeamBuilderResult
        {
            TeamId = options.TeamId,
            Manager = players.Single(p => p.Role == Api.PlayerRole.Manager),
            Players = players,
        };
    }

    public static void AddUser(this IDataStateBuilder dataStateBuilder, Action<User>? userBuilder = null)
        => dataStateBuilder.Add(BuildUser(dataStateBuilder, userBuilder));

    public static User BuildUser(this IDataStateBuilder dataStateBuilder, Action<User>? userBuilder = null)
        => BuildEntity
        (
            new Api.Data.User
            {
                Id = TestIds.Generate(),
                Username = "integrationtester",
                Email = "integration@test.com",
                Name = "integrationtester",
                ApprovedName = "integrationtester",
                Sponsor = "SEI",
                Role = Api.UserRole.Member
            },
            userBuilder
        );
}
