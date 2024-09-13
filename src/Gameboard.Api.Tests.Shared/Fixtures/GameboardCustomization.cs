using AutoFixture;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Gameboard.Api.Data;
using Gameboard.Api.Features.GameEngine;

namespace Gameboard.Api.Tests.Shared.Fixtures;

public class GameboardCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        RegisterDefaultEntityModels(fixture);
        RegisterDefaultServices(fixture);
    }

    private void RegisterDefaultEntityModels(IFixture fixture)
    {
        fixture.Register(() => fixture);
        fixture.Customizations.Add(new IdBuilder());
        var now = DateTimeOffset.UtcNow;

        fixture.Register(() => new Data.ArchivedChallenge
        {
            Id = fixture.Create<string>(),
            Name = $"Archived challenge {fixture.Create<string>()}",
            Points = fixture.Create<int>(),
            StartTime = now.AddDays(-2),
            EndTime = now.AddDays(-1),
            HasGamespaceDeployed = false
        });

        fixture.Register(() => new AwardedChallengeBonus
        {
            Id = fixture.Create<string>(),
            EnteredOn = DateTimeOffset.MinValue,
        });

        fixture.Register(() => new ChallengeBonusCompleteSolveRank
        {
            Id = fixture.Create<string>(),
            Description = fixture.Create<string>(),
            PointValue = 10,
            ChallengeBonusType = ChallengeBonusType.CompleteSolveRank,
            ChallengeSpec = fixture.Create<Data.ChallengeSpec>(),
            AwardedTo = new List<Data.AwardedChallengeBonus>()
        });

        fixture.Register<ChallengeBonus>(() => new ChallengeBonusCompleteSolveRank
        {
            Id = fixture.Create<string>(),
            Description = fixture.Create<string>(),
            PointValue = 10,
            ChallengeBonusType = ChallengeBonusType.CompleteSolveRank,
            ChallengeSpec = fixture.Create<Data.ChallengeSpec>(),
            AwardedTo = new List<Data.AwardedChallengeBonus>()
        });

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
            HasDeployedGamespace = false,
            GameEngineType = GameEngineType.TopoMojo
        });

        fixture.Register(() => new Data.ChallengeSpec
        {
            Id = fixture.Create<string>(),
            Game = fixture.Create<Data.Game>(),
            Name = fixture.Create<string>(),
            X = 0,
            Y = 0,
            R = 1
        });

        fixture.Register(() => new Data.Game
        {
            Id = fixture.Create<string>(),
            Name = fixture.Create<string>(),
            GameStart = now,
            GameEnd = now.AddDays(1),
            IsPublished = true,
            RegistrationOpen = now,
            RegistrationClose = now.AddDays(1),
            RegistrationType = GameRegistrationType.Open
        });

        fixture.Register(() => new PracticeModeSettings
        {
            Id = fixture.Create<string>(),
            CertificateHtmlTemplate = null,
            DefaultPracticeSessionLengthMinutes = 60,
            IntroTextMarkdown = null,
            SuggestedSearches = ""
        });

        fixture.Register<Data.Sponsor>(() => new()
        {
            Id = fixture.Create<string>(),
            Name = $"Sponsor {fixture.Create<string>()}",
            Approved = true,
            Logo = "test.svg",
            SponsoredPlayers = new List<Data.Player>(),
            SponsoredUsers = new List<Data.User>()
        });

        fixture.Register(() => new Data.Player
        {
            Id = fixture.Create<string>(),
            User = fixture.Create<Data.User>(),
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
            Players = new List<TopoMojo.Api.Client.Player>
            {
                new()
                {
                    GamespaceId = "33b9cf31-8686-4d95-b5a8-9fb1b7f8ce71",
                    SubjectId = "f4390dff-420d-47da-90c8-4c982eeab822",
                    SubjectName = "A cool player",
                    Permission = TopoMojo.Api.Client.Permission.None,
                    IsManager = true
                }
            },
            WhenCreated = DateTimeOffset.UtcNow,
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddMinutes(60),
            ExpirationTime = DateTime.UtcNow.AddMinutes(60),
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
                LastScoreTime = DateTimeOffset.UtcNow.AddMinutes(5),
                Questions =
                [
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
                ]
            }
        });

        fixture.Register(() => new Data.User
        {
            Id = fixture.Create<string>(),
            Username = fixture.Create<string>(),
            ApprovedName = fixture.Create<string>(),
            Sponsor = fixture.Create<Data.Sponsor>(),
            Role = UserRoleKey.Member
        });

        fixture.Register(() => new GameEngineSectionSubmission
        {
            Id = fixture.Create<string>(),
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(1),
            SectionIndex = 0,
            Questions =
            [
                new() { Answer = fixture.Create<string>() }
            ]
        });
    }

    private void RegisterDefaultServices(IFixture fixture)
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.AddGameboardMaps();
        }).CreateMapper();

        fixture.Register(() => mapper);
    }
}
