using AutoMapper;
using Gameboard.Api.Common;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Features.GameEngine;

namespace Gameboard.Api.Tests.Integration.Fixtures;

public class TestGameEngineService : IGameEngineService
{
    private readonly ITestGradingResultService _gradingResultService;
    private readonly ITestGameEngineStateChangeService _gameEngineStateChangeService;
    private readonly IGuidService _guids;
    private readonly IMapper _mapper;

    public TestGameEngineService
    (
        IGuidService guids,
        IMapper mapper,
        ITestGameEngineStateChangeService gameEngineStateChangeService,
        ITestGradingResultService gradingResultService
    )
    {
        _gameEngineStateChangeService = gameEngineStateChangeService;
        _gradingResultService = gradingResultService;
        _guids = guids;
        _mapper = mapper;
    }

    public Task<IEnumerable<GameEngineSectionSubmission>> AuditChallenge(Api.Data.Challenge entity)
    {
        return Task.FromResult(Array.Empty<GameEngineSectionSubmission>() as IEnumerable<GameEngineSectionSubmission>);
    }

    public Task CompleteGamespace(Data.Challenge entity)
        => CompleteGamespace(entity.Id, entity.GameEngineType);

    public Task CompleteGamespace(string id, GameEngineType gameEngineType)
    {
        return Task.CompletedTask;
    }

    public Task DeleteGamespace(Data.Challenge entity)
        => DeleteGamespace(entity.Id, entity.GameEngineType);

    public Task DeleteGamespace(string id, GameEngineType gameEngineType)
    {
        return Task.CompletedTask;
    }

    public Task ExtendSession(Data.Challenge entity, DateTimeOffset sessionEnd)
    {
        return Task.CompletedTask;
    }

    public Task ExtendSession(string challengeId, DateTimeOffset sessionEnd, GameEngineType gameEngineType)
    {
        return Task.CompletedTask;
    }

    public Task<GameEngineGameState> GetChallengeState(GameEngineType gameEngineType, string stateJson)
    {
        return Task.FromResult(new GameEngineGameState());
    }

    public Task<ConsoleSummary> GetConsole(Api.Data.Challenge entity, ConsoleRequest model, bool observer)
    {
        return Task.FromResult(new ConsoleSummary { });
    }

    public IEnumerable<GameEngineGamespaceVm> GetGamespaceVms(GameEngineGameState state)
    {
        return Array.Empty<GameEngineGamespaceVm>();
    }

    public Task<GameEngineGameState> GetPreview(Data.ChallengeSpec spec)
    {
        return Task.FromResult(new GameEngineGameState());
    }

    public Task<GameEngineGameState> GradeChallenge(Data.Challenge entity, GameEngineSectionSubmission model)
    {
        return Task.FromResult(_gradingResultService.Get(entity));
    }

    public Task<ExternalSpec[]> ListGameEngineSpecs(SearchFilter model)
    {
        return Task.FromResult(Array.Empty<ExternalSpec>());
    }

    public Task<GameEngineGameState> LoadGamespace(Data.Challenge entity)
    {
        return Task.FromResult(new GameEngineGameState());
    }

    public Task<GameEngineGameState> RegisterGamespace(GameEngineChallengeRegistration registration)
    {
        return Task.FromResult(new GameEngineGameState
        {
            Id = _guids.Generate(),
            Name = registration.Challenge.Name,
            ManagerId = registration.Player.Id,
            ManagerName = registration.Player.ApprovedName,
            IsActive = true,
            Players = new GameEnginePlayer[] { _mapper.Map<GameEnginePlayer>(registration.Player) },
            WhenCreated = DateTimeOffset.UtcNow,
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddHours(8),
            ExpirationTime = DateTimeOffset.UtcNow.AddHours(8),
            Vms = new GameEngineVmState
            {
                Id = _guids.Generate(),
                Name = "VM1",
                IsolationId = registration.ChallengeSpec.Id,
                IsRunning = true,
                IsVisible = true
            }.ToCollection(),
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
                Questions = new GameEngineQuestionView()
                {
                    Answer = "test answer",
                    IsCorrect = false,
                    IsGraded = false
                }.ToCollection()
            }
        });
    }

    public Task<GameEngineGameState> RegradeChallenge(Data.Challenge entity)
    {
        return Task.FromResult(new GameEngineGameState());
    }

    public Task<GameEngineGameState> StartGamespace(GameEngineGamespaceStartRequest request)
    {
        return Task.FromResult(_gameEngineStateChangeService.StartGamespaceResult);
    }

    public Task<GameEngineGameState> StopGamespace(Data.Challenge entity)
    {
        return Task.FromResult(_gameEngineStateChangeService.StopGamespaceResult);
    }
}
