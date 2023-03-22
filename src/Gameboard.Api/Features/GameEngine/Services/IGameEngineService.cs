using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gameboard.Api.Features.GameEngine;

public interface IGameEngineService
{
    Task<IEnumerable<GameEngineSectionSubmission>> AuditChallenge(Data.Challenge entity);
    Task CompleteGamespace(Data.Challenge entity);
    Task DeleteGamespace(Data.Challenge entity);
    Task ExtendSession(Data.Challenge entity, DateTimeOffset sessionEnd);
    Task<ConsoleSummary> GetConsole(Data.Challenge entity, ConsoleRequest model, bool observer);
    Task<GameEngineGameState> GetPreview(Data.ChallengeSpec spec);
    Task<GameEngineGameState> GradeChallenge(Data.Challenge entity, GameEngineSectionSubmission model);
    Task<ExternalSpec[]> ListSpecs(SearchFilter model);
    Task<GameEngineGameState> LoadGamespace(Data.Challenge entity);
    Task<GameEngineGameState> RegisterGamespace(GameEngineChallengeRegistration registration);
    Task<GameEngineGameState> RegradeChallenge(Data.Challenge entity);
    Task<GameEngineGameState> StartGamespace(Data.Challenge entity);
    Task<GameEngineGameState> StopGamespace(Data.Challenge entity);
}
