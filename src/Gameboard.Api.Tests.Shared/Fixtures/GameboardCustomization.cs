using AutoFixture;
using Gameboard.Api.Features.GameEngine;

namespace Gameboard.Api.Tests.Shared;

public class GameboardCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        var now = DateTimeOffset.UtcNow;

        fixture.Register<Data.User>(() => new Data.User
        {
            Id = fixture.Create<string>(),
            Username = "testuser",
            ApprovedName = "Test User",
            Sponsor = "Test Sponsor",
            Role = UserRole.Member
        });

        fixture.Register<Data.Game>(() => new Data.Game
        {
            Id = fixture.Create<string>(),
            Name = "A test game",
            GameStart = now,
            GameEnd = now.AddDays(1),
            IsPublished = true
        });

        fixture.Register<Data.Player>(() => new Data.Player
        {
            Id = fixture.Create<string>(),
            TeamId = fixture.Create<string>(),
            User = fixture.Create<Data.User>(),
            Game = fixture.Create<Data.Game>(),
            ApprovedName = "Test Player",
            Sponsor = "Test Sponsor",
            Role = PlayerRole.Manager,
            SessionBegin = now,
            SessionEnd = now.AddDays(1),
            Score = 0,
            Mode = PlayerMode.Competition
        });

        fixture.Register<Data.Challenge>(() => new Data.Challenge
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


        fixture.Register(() => new GameEngineSectionSubmission
        {
            Id = fixture.Create<string>(),
            Timestamp = DateTimeOffset.Now.AddMinutes(1),
            SectionIndex = 0,
            Questions = new GameEngineAnswerSubmission[]
            {
                new GameEngineAnswerSubmission { Answer = fixture.Create<string>() }
            }
        });
    }
}
