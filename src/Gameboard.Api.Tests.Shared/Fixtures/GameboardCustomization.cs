using AutoFixture;

namespace Gameboard.Api.Tests.Shared.Fixtures;

public class GameboardCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customizations.Add(new IdBuilder());
        fixture.Register(() => fixture);
        var now = DateTimeOffset.UtcNow;

        fixture.Register(() => new Data.Challenge
        {
            Id = fixture.Create<string>(),
            Name = "A test challenge",
            ExternalId = fixture.Create<string>(),
            SpecId = fixture.Create<string>(),
            TeamId = fixture.Create<string>(),
            Player = fixture.Create<Data.Player>(),
            Points = 50,
            WhenCreated = now,
            StartTime = now,
            EndTime = now.AddDays(1),
            HasDeployedGamespace = false,
            GameEngineType = GameEngineType.TopoMojo
        });

        fixture.Register(() => new Data.Game
        {
            Id = fixture.Create<string>(),
            Name = "A test game",
            GameStart = now,
            GameEnd = now.AddDays(1),
            IsPublished = true
        });

        fixture.Register(() => new Data.Player
        {
            Id = fixture.Create<string>(),
            User = fixture.Create<Data.User>(),
            Game = fixture.Create<Data.Game>(),
            ApprovedName = "Test Player",
            Sponsor = fixture.Create<Data.Sponsor>(),
            Role = PlayerRole.Manager,
            Score = 0,
            SessionBegin = DateTimeOffset.MinValue,
            SessionEnd = DateTimeOffset.MinValue,
            TeamId = fixture.Create<string>(),
            Mode = PlayerMode.Competition
        });

        fixture.Register(() => new TopoMojo.Api.Client.GameState
        {
            Id = "75da686f-4d37-48cb-8755-80121ebf3647",
            Name = "BenState",
            ManagerId = "7fb2c8d6-c83f-412e-8e18-0e87ed3befcc",
            ManagerName = "Sergei",
            Markdown = "Here is some markdown _stuff_.",
            Audience = "gameboard",
            LaunchpointUrl = "https://google.com",
            Players = new TopoMojo.Api.Client.Player[]
            {
                new TopoMojo.Api.Client.Player
                {
                    GamespaceId = "33b9cf31-8686-4d95-b5a8-9fb1b7f8ce71",
                    SubjectId = "f4390dff-420d-47da-90c8-4c982eeab822",
                    SubjectName = "A cool player",
                    Permission = TopoMojo.Api.Client.Permission.None,
                    IsManager = true
                }
            },
            WhenCreated = DateTimeOffset.Now,
            StartTime = DateTimeOffset.Now,
            EndTime = DateTimeOffset.Now.AddMinutes(60),
            ExpirationTime = DateTime.Now.AddMinutes(60),
            IsActive = true,
            Vms = new TopoMojo.Api.Client.VmState[]
            {
                new()
                {
                    Id = "10fccb66-6916-45e2-9a39-188d3a692d4a",
                    Name = "VM 1",
                    IsolationId = "vm1",
                    IsRunning = true,
                    IsVisible = true
                },
                new()
                {
                    Id = "8d771689-8b37-48e7-b706-9efe1c64bdca",
                    Name = "VM 2",
                    IsolationId = "vm2",
                    IsRunning = true,
                    IsVisible = false
                },
            },
            Challenge = new TopoMojo.Api.Client.ChallengeView
            {
                Text = "A challenging challenge",
                MaxPoints = 100,
                MaxAttempts = 3,
                Attempts = 1,
                Score = 50,
                SectionCount = 1,
                SectionIndex = 0,
                SectionScore = 50,
                SectionText = "The best one",
                LastScoreTime = DateTimeOffset.Now.AddMinutes(5),
                Questions = new TopoMojo.Api.Client.QuestionView[]
                {
                    new()
                    {
                        Text = "What is your quest?",
                        Hint = "It's not about swallows or whatever",
                        Answer = "swallows",
                        Example = "To be the very best, like no one ever was",
                        Weight = 0.5f,
                        Penalty = 0.2f,
                        IsCorrect = false,
                        IsGraded = true
                    }
                }
            }
        });

        fixture.Register<Data.Sponsor>(() => new()
        {
            Id = fixture.Create<string>(),
            Name = $"Sponsor {fixture.Create<string>()}",
            Approved = true,
            Logo = "test.svg",
            SponsoredPlayers = Array.Empty<Data.Player>(),
            SponsoredUsers = Array.Empty<Data.User>()
        });

        fixture.Register(() => new Data.User
        {
            Id = fixture.Create<string>(),
            Username = "testuser",
            ApprovedName = "Test User",
            Sponsor = new Data.Sponsor { Id = fixture.Create<string>(), Name = "Test Sponsor" },
            Role = UserRole.Member
        });
    }
}