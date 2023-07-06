using Gameboard.Api.Tests.Shared;

namespace Gameboard.Api.Tests.Integration.Fixtures;

public static class GameboardTestContextDefaultEntityExtensions
{
    private static T BuildEntity<T>(T entity, Action<T>? builder = null) where T : class, Data.IEntity
    {
        builder?.Invoke(entity);
        return entity;
    }

    public static void AddChallengeSpec(this IDataStateBuilder dataStateBuilder, Action<Data.ChallengeSpec>? specBuilder = null)
        => dataStateBuilder.Add(BuildChallengeSpec(dataStateBuilder, specBuilder));

    public static Data.ChallengeSpec BuildChallengeSpec(this IDataStateBuilder dataStateBuilder, Action<Data.ChallengeSpec>? specBuilder = null)
        => BuildEntity
        (
            new Data.ChallengeSpec
            {
                Id = TestIds.Generate(),
                Name = "Integration Test Challenge Spec",
                AverageDeploySeconds = 1,
                Points = 50,
                X = 0,
                Y = 0,
                R = 1
            },
            specBuilder
        );

    public static void AddChallenge(this IDataStateBuilder dataStateBuilder, Action<Data.Challenge>? challengeBuilder = null)
        => dataStateBuilder.Add(BuildChallenge(dataStateBuilder, challengeBuilder));

    public static Data.Challenge BuildChallenge(this IDataStateBuilder dataStateBuilder, Action<Data.Challenge>? challengeBuilder = null)
        => BuildEntity
        (
            new Data.Challenge
            {
                Name = "Integration Test Challenge",
                StartTime = DateTimeOffset.Now.ToUniversalTime()
            },
            challengeBuilder
        );

    public static void AddGame(this IDataStateBuilder dataStateBuilder, string gameId)
        => AddGame(dataStateBuilder, g => { g.Id = gameId; });

    public static void AddGame(this IDataStateBuilder dataStateBuilder, Action<Data.Game>? gameBuilder = null)
        => dataStateBuilder.Add(BuildGame(dataStateBuilder, gameBuilder));

    public static Data.Game BuildGame(this IDataStateBuilder dataStateBuilder, Action<Data.Game>? gameBuilder = null)
        => BuildEntity
        (
            new Data.Game
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

    public static void AddPlayer(this IDataStateBuilder dataStateBuilder, Action<Data.Player>? playerBuilder = null)
        => dataStateBuilder.Add(BuildPlayer(dataStateBuilder, playerBuilder));

    public static Data.Player BuildPlayer(this IDataStateBuilder dataStateBuilder, Action<Data.Player>? playerBuilder = null)
    {
        // TODO: this is potentially urky if the testing dev sets userid but not user's id
        var userId = TestIds.Generate();

        return BuildEntity
        (
            new Data.Player
            {
                Id = TestIds.Generate(),
                TeamId = TestIds.Generate(),
                ApprovedName = "Integration Test Player",
                Sponsor = "Integration Test Sponsor",
                Role = Gameboard.Api.PlayerRole.Manager,
                User = new Data.User { Id = userId },
                UserId = userId
            },
            playerBuilder
        );
    }

    /// <summary>
    /// Creates a team with a challenge, and players dictated by the options parameter.
    /// </summary>
    /// <param name="dataStateBuilder"></param>
    /// <param name="fixture"></param>
    /// <param name="optsBuilder"></param>
    /// <returns></returns>
    public static TeamBuilderResult AddTeam(this IDataStateBuilder dataStateBuilder, IFixture fixture, Action<TeamBuilderOptions> optsBuilder)
    {
        var options = new TeamBuilderOptions
        {
            Challenge = new SimpleEntity { Id = fixture.Create<string>(), Name = fixture.Create<string>() },
            Name = fixture.Create<string>(),
            NumPlayers = 5,
            GameBuilder = g => { },
            TeamId = fixture.Create<string>()
        };

        optsBuilder.Invoke(options);

        // fill out properties
        var teamName = string.IsNullOrWhiteSpace(options.Name) ? fixture.Create<string>() : options.Name;

        var game = new Api.Data.Game
        {
            Id = fixture.Create<string>(),
            // just to avoid obnoxious overconfig on the other end
            RegistrationClose = DateTimeOffset.UtcNow.AddYears(1),
            RegistrationOpen = DateTimeOffset.UtcNow.AddYears(-1),
            RegistrationType = Api.GameRegistrationType.Open
        };

        options.GameBuilder?.Invoke(game);

        Data.Challenge? challenge = null;
        if (options.Challenge != null)
        {
            var specId = fixture.Create<string>();
            challenge = new Api.Data.Challenge
            {
                Id = options.Challenge.Id,
                Name = options.Challenge.Name,
                Game = game,
                SpecId = specId,
                TeamId = options.TeamId
            };

            dataStateBuilder.AddChallengeSpec(spec =>
            {
                spec.Id = specId;
                spec.Name = fixture.Create<string>();
            });
        }

        // create players
        var players = new List<Data.Player>();

        for (var i = 0; i < options.NumPlayers; i++)
        {
            var createManager = i == 0;
            var player = new Data.Player
            {
                Id = (i == 0 && options.Manager?.Id != null ? options.Manager.Id : fixture.Create<string>()),
                ApprovedName = teamName ?? options.Manager?.Name,
                Name = teamName ?? options.Manager?.Name,
                Role = createManager ? Api.PlayerRole.Manager : Api.PlayerRole.Member,
                TeamId = options.TeamId,
                User = new Data.User { Id = fixture.Create<string>() },
                Challenges = challenge != null ? new List<Api.Data.Challenge> { challenge } : new Api.Data.Challenge[] { },
                Game = game
            };

            players.Add(player);
        }
        dataStateBuilder.AddRange(players);

        return new TeamBuilderResult
        {
            Challenge = challenge,
            Game = game,
            TeamId = options.TeamId,
            Manager = players.Single(p => p.Role == Api.PlayerRole.Manager),
            Players = players,
        };
    }

    public static IDataStateBuilder AddUser(this IDataStateBuilder dataStateBuilder, Action<Data.User>? userBuilder = null)
        => dataStateBuilder.Add(BuildUser(dataStateBuilder, userBuilder));

    public static Data.User BuildUser(this IDataStateBuilder dataStateBuilder, Action<Data.User>? userBuilder = null)
        => BuildEntity
        (
            new Data.User
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

    public static IEnumerable<Data.Player> BuildTeam(this IDataStateBuilder builder, int teamSize = 5, Action<Data.Player>? playerBuilder = null)
    {
        var team = new List<Data.Player>();
        var teamId = TestIds.Generate();

        for (var i = 0; i < teamSize; i++)
        {
            var player = BuildPlayer(builder, p => p.TeamId = teamId);
            playerBuilder?.Invoke(player);
            team.Add(player);
        }

        return team;
    }

    public static ICollection<TEntity> ToCollection<TEntity>(this TEntity entity)
    {
        return new TEntity[] { entity };
    }
}
