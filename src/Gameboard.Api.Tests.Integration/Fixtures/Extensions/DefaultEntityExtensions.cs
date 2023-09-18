using Gameboard.Api.Common;
using Gameboard.Api.Data;

namespace Gameboard.Api.Tests.Integration.Fixtures;

public static class GameboardTestContextDefaultEntityExtensions
{
    private static T BuildEntity<T>(T entity, Action<T>? builder = null) where T : class, Data.IEntity
    {
        builder?.Invoke(entity);
        return entity;
    }

    // eventually will replace these with registrations in the customization (like the integration test project does)
    private static string GenerateTestGuid() => Guid.NewGuid().ToString("n");

    public static TEntity Build<TEntity>(this IDataStateBuilder dataStateBuilder, IFixture fixture) where TEntity : class, IEntity
        => Build<TEntity>(dataStateBuilder, fixture, null);

    public static TEntity Build<TEntity>(this IDataStateBuilder dataStateBuilder, IFixture fixture, Action<TEntity>? entityBuilder) where TEntity : class, IEntity
    {
        var entity = fixture.Create<TEntity>();
        entityBuilder?.Invoke(entity);
        return entity;
    }

    public static void AddChallengeSpec(this IDataStateBuilder dataStateBuilder, Action<Data.ChallengeSpec>? specBuilder = null)
        => dataStateBuilder.Add(BuildChallengeSpec(dataStateBuilder, specBuilder));

    public static Data.ChallengeSpec BuildChallengeSpec(this IDataStateBuilder dataStateBuilder, Action<Data.ChallengeSpec>? specBuilder = null)
        => BuildEntity
        (
            new Data.ChallengeSpec
            {
                Id = GenerateTestGuid(),
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

    public static void AddChallenge(this IDataStateBuilder dataStateBuilder, Action<Data.Challenge>? challengeBuilder = null)
        => dataStateBuilder.Add(BuildChallenge(dataStateBuilder, challengeBuilder));

    public static Data.Challenge BuildChallenge(this IDataStateBuilder dataStateBuilder, Action<Data.Challenge>? challengeBuilder = null)
        => BuildEntity
        (
            new Data.Challenge { Name = "Integration Test Challenge", },
            challengeBuilder
        );

    public static void AddGame(this IDataStateBuilder dataStateBuilder, Action<Data.Game>? gameBuilder = null)
        => dataStateBuilder.Add(BuildGame(dataStateBuilder, gameBuilder));

    public static Data.Game BuildGame(this IDataStateBuilder dataStateBuilder, Action<Data.Game>? gameBuilder = null)
        => BuildEntity
        (
            new Data.Game
            {
                Id = GenerateTestGuid(),
                Name = "Test game",
                Competition = "Test competition",
                Season = "1",
                Track = "Individual",
                Sponsor = "Test Sponsor",
                GameStart = DateTimeOffset.UtcNow,
                GameEnd = DateTime.UtcNow + TimeSpan.FromDays(30),
                RegistrationOpen = DateTimeOffset.UtcNow,
                RegistrationClose = DateTime.UtcNow + TimeSpan.FromDays(30),
                RegistrationType = GameRegistrationType.Open,
            },
            gameBuilder
        );

    public static void AddPlayer(this IDataStateBuilder dataStateBuilder, IFixture fixture, Action<Data.Player>? playerBuilder = null)
        => dataStateBuilder.Add(BuildPlayer(dataStateBuilder, fixture, playerBuilder));

    public static Data.Player BuildPlayer(this IDataStateBuilder dataStateBuilder, IFixture fixture, Action<Data.Player>? playerBuilder = null)
    {
        var player = fixture.Create<Data.Player>();
        return BuildEntity(player, playerBuilder);
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
        // build options
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

        // create and attach players
        var players = new List<Data.Player>();

        for (var i = 0; i < options.NumPlayers; i++)
        {
            var createManager = i == 0;
            var playerUser = fixture.Create<Data.User>();

            var player = new Data.Player
            {
                Id = fixture.Create<string>(),
                ApprovedName = teamName,
                Name = teamName,
                Role = createManager ? PlayerRole.Manager : PlayerRole.Member,
                TeamId = options.TeamId,
                Sponsor = fixture.Create<Data.Sponsor>(),
                User = playerUser,
                UserId = playerUser.Id
            };

            players.Add(player);
        }

        var game = new Data.Game
        {
            Id = fixture.Create<string>(),

            // just to avoid obnoxious overconfig on the other end
            RegistrationClose = DateTimeOffset.UtcNow.AddYears(1),
            RegistrationOpen = DateTimeOffset.UtcNow.AddYears(-1),
            RegistrationType = GameRegistrationType.Open
        };
        game.Players = players;

        // // create and attach challenge for each player
        // if (options.Challenge is not null)
        // {
        //     // create the challengespec
        //     var specId = fixture.Create<string>();

        //     dataStateBuilder.AddChallengeSpec(spec =>
        //     {
        //         spec.Id = specId;
        //         spec.Name = fixture.Create<string>();
        //     });

        //     foreach (var player in game.Players)
        //     {
        //         player.Challenges = new Data.Challenge
        //         {
        //             Id = options.Challenge.Id,
        //             Name = options.Challenge.Name,
        //             Game = game,
        //             SpecId = specId,
        //             TeamId = options.TeamId
        //         }.ToCollection();
        //     }
        // }

        options.GameBuilder?.Invoke(game);

        return new TeamBuilderResult
        {
            // Challenges = game.pl,
            Game = game,
            TeamId = options.TeamId,
            Manager = players.Single(p => p.Role == PlayerRole.Manager)
        };
    }

    // public static IDataStateBuilder AddUser(this IDataStateBuilder dataStateBuilder, Action<Data.User>? userBuilder = null)
    //     => dataStateBuilder.Add(BuildUser(dataStateBuilder, userBuilder));

    // public static Data.User BuildUser(this IDataStateBuilder dataStateBuilder, Action<Data.User>? userBuilder = null)
    //     => BuildEntity
    //     (
    //         new Data.User
    //         {
    //             Id = GenerateTestGuid(),
    //             Username = "integrationtester",
    //             Email = "integration@test.com",
    //             Name = "integrationtester",
    //             ApprovedName = "integrationtester",
    //             Sponsor = new Data.Sponsor { Id = "integrationTestSponsor", Name = "Integration Test Sponsor" },
    //             Role = UserRole.Member
    //         },
    //         userBuilder
    //     );

    public static IEnumerable<Data.Player> BuildTeam(this IDataStateBuilder builder, IFixture fixture, int teamSize = 5, Action<Data.Player>? playerBuilder = null)
    {
        var team = new List<Data.Player>();
        var teamId = GenerateTestGuid();

        for (var i = 0; i < teamSize; i++)
        {
            var player = BuildPlayer(builder, fixture, p => p.TeamId = teamId);
            playerBuilder?.Invoke(player);
            team.Add(player);
        }

        return team;
    }
}
