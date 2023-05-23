using AutoMapper;
using Gameboard.Api;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Services;

namespace Gameboard.Api.Tests.Integration.Fixtures;

internal class TestGameEngineService : IGameEngineService
{
    private readonly IGameEngineStore _store;
    private readonly IGuidService _guids;
    private readonly IMapper _mapper;

    public TestGameEngineService(IGuidService guids, IMapper mapper, IGameEngineStore store)
    {
        _guids = guids;
        _mapper = mapper;
        _store = store;
    }

    public Task<IEnumerable<GameEngineSectionSubmission>> AuditChallenge(Api.Data.Challenge entity)
    {
        return Task.FromResult(new GameEngineSectionSubmission[] { } as IEnumerable<GameEngineSectionSubmission>);
    }

    public Task CompleteGamespace(Api.Data.Challenge entity)
    {
        return Task.CompletedTask;
    }

    public Task DeleteGamespace(Api.Data.Challenge entity)
    {
        return Task.CompletedTask;
    }

    public Task ExtendSession(Api.Data.Challenge entity, DateTimeOffset sessionEnd)
    {
        return Task.CompletedTask;
    }

    public Task<ConsoleSummary> GetConsole(Api.Data.Challenge entity, ConsoleRequest model, bool observer)
    {
        return Task.FromResult(new ConsoleSummary { });
    }

    public Task<IEnumerable<GameEngineGamespaceVm>> GetGamespaceVms(GameEngineGameState state)
    {
        return Task.FromResult(new GameEngineGamespaceVm[] { }.AsEnumerable());
    }

    public Task<GameEngineGameState> GetPreview(Api.Data.ChallengeSpec spec)
    {
        return Task.FromResult(new GameEngineGameState());
    }

    public Task<GameEngineGameState> GradeChallenge(Api.Data.Challenge entity, GameEngineSectionSubmission model)
    {
        return Task.FromResult(new GameEngineGameState());
    }

    public Task<ExternalSpec[]> ListSpecs(SearchFilter model)
    {
        return Task.FromResult(new ExternalSpec[] { });
    }

    public Task<GameEngineGameState> LoadGamespace(Api.Data.Challenge entity)
    {
        return Task.FromResult(new GameEngineGameState());
    }

    public Task<GameEngineGameState> RegisterGamespace(GameEngineChallengeRegistration registration)
    {
        return Task.FromResult(new GameEngineGameState
        {
            Id = _guids.GetGuid(),
            Name = registration.Challenge.Name,
            ManagerId = registration.Player.Id,
            ManagerName = registration.Player.ApprovedName,
            IsActive = true,
            Players = new GameEnginePlayer[] { _mapper.Map<GameEnginePlayer>(registration.Player) },
            WhenCreated = DateTimeOffset.UtcNow,
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddHours(8),
            ExpirationTime = DateTimeOffset.UtcNow.AddHours(8),
            Vms = new GameEngineVmState[]
            {
                new GameEngineVmState()
                {
                    Id = _guids.GetGuid(),
                    Name = "VM1",
                    IsolationId = registration.ChallengeSpec.Id,
                    IsRunning = true,
                    IsVisible = true
                }
            },
            Challenge = new GameEngineChallengeView
            {
                MaxPoints = 50,
                MaxAttempts = 3,
                Attempts = 0,
                Score = 0,
                SectionCount = 1,
                SectionIndex = 0,
                SectionScore = 0,
                LastScoreTime = DateTimeOffset.MinValue,
                Questions = new GameEngineQuestionView[]
                {
                    new GameEngineQuestionView
                    {
                        Answer = "test answer",
                        IsCorrect = false,
                        IsGraded = false
                    }
                }
            }
        });
    }

    public Task<GameEngineGameState> RegradeChallenge(Api.Data.Challenge entity)
    {
        return Task.FromResult(new GameEngineGameState());
    }

    public Task<GameEngineGameState> StartGamespace(Api.Challenge challenge)
    {
        return Task.FromResult(new GameEngineGameState());
    }

    public Task<GameEngineGameState> StopGamespace(Api.Data.Challenge entity)
    {
        return Task.FromResult(new GameEngineGameState());
    }
}
