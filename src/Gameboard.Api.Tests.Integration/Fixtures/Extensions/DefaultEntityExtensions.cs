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
            new Data.Challenge
            {
                Name = "Integration Test Challenge",
            },
            challengeBuilder
        );

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

    public static TeamBuilderResult AddTeam(this IDataStateBuilder dataStateBuilder, IFixture fixture, Action<TeamBuilderOptions> optsBuilder)
    {
        var options = new TeamBuilderOptions
        {
            ChallengeId = fixture.Create<string>(),
            ChallengeName = fixture.Create<string>(),
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

        var specId = fixture.Create<string>();
        var challenge = new Api.Data.Challenge
        {
            Id = options.ChallengeId,
            Name = options.Name,
            Game = game,
            SpecId = specId,
            TeamId = options.TeamId
        };

        // create players
        var players = new List<Data.Player>();

        for (var i = 0; i < options.NumPlayers; i++)
        {
            var createManager = i == 0;
            var player = new Data.Player
            {
                Id = fixture.Create<string>(),
                ApprovedName = teamName,
                Name = teamName,
                Role = createManager ? Api.PlayerRole.Manager : Api.PlayerRole.Member,
                TeamId = options.TeamId,
                User = new Data.User { Id = fixture.Create<string>() },
                Challenges = new List<Api.Data.Challenge> { challenge },
                Game = game
            };

            players.Add(player);
        }

        // Add entities
        dataStateBuilder.AddChallengeSpec(spec =>
        {
            spec.Id = specId;
            spec.Name = fixture.Create<string>();
        });
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
}
