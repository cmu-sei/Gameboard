using AutoMapper;
using Gameboard.Api.Features.GameEngine;

namespace Gameboard.Api.Tests.Unit;

public class GameEngineMapsTests
{
    [Theory, InlineAutoData]
    public void MapTopoState_WithCompleteData_DoesNotThrow(IFixture fixture)
    {
        // given
        var topoState = fixture.Create<TopoMojo.Api.Client.GameState>();
        topoState.Players = fixture.CreateMany<TopoMojo.Api.Client.Player>(2).ToArray();
        topoState.Vms = fixture.CreateMany<TopoMojo.Api.Client.VmState>(1).ToArray();
        topoState.Challenge = fixture.Create<TopoMojo.Api.Client.ChallengeView>();
        topoState.Challenge.Questions = fixture.CreateMany<TopoMojo.Api.Client.QuestionView>(3).ToArray();

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new GameEngineMaps());
        });
        var mapper = new Mapper(mapperConfig);

        // when/then
        mapperConfig.AssertConfigurationIsValid();
        Should.NotThrow(() => mapper.Map<GameEngineGameState>(topoState));
    }

    // given an explicit topo state, does it apparently map correctly?
    // yes, this is an iffy test
    [Fact]
    public void MapStringState_WithCompleteData_PopulatesSpotCheckedFields()
    {
        // given
        var topoState = new TopoMojo.Api.Client.GameState
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
                new TopoMojo.Api.Client.VmState
                {
                    Id = "10fccb66-6916-45e2-9a39-188d3a692d4a",
                    Name = "VM 1",
                    IsolationId = "vm1",
                    IsRunning = true,
                    IsVisible = true
                },
                new TopoMojo.Api.Client.VmState
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
                    new TopoMojo.Api.Client.QuestionView
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
        };

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new GameEngineMaps());
        });
        var mapper = new Mapper(mapperConfig);

        // when
        var mapped = mapper.Map<GameEngineGameState>(topoState);

        // then
        mapped.Vms.FirstOrDefault(vm => vm.IsolationId == "vm2").ShouldNotBeNull();
        mapped.Challenge.Questions.First().IsCorrect.ShouldBeFalse();
    }
}
