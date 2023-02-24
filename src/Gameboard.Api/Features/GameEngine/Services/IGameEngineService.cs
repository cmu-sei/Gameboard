using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gameboard.Api.Features.GameEngine;

public interface IGameEngineService
{
    Task<IEnumerable<IGameEngineSectionSubmission>> AuditChallenge(Data.Challenge entity);
    Task CompleteGamespace(Data.Challenge entity);
    Task DeleteGamespace(Data.Challenge entity);
    Task ExtendSession(Data.Challenge entity, DateTimeOffset sessionEnd);
    Task<ConsoleSummary> GetConsole(Data.Challenge entity, ConsoleRequest model, bool observer);
    Task<IGameEngineGameState> GetPreview(Data.ChallengeSpec spec);
    Task<IGameEngineGameState> GradeChallenge(Data.Challenge entity, IGameEngineSectionSubmission model);
    Task<ExternalSpec[]> ListSpecs(SearchFilter model);
    Task<IGameEngineGameState> LoadGamespace(Data.Challenge entity);
    Task<IGameEngineGameState> RegisterGamespace(Data.ChallengeSpec spec, NewChallenge model, Data.Game game, Data.Player player, Data.Challenge entity, int playerCount, string graderKey, string graderUrl);
    Task<IGameEngineGameState> RegradeChallenge(Data.Challenge entity);
    Task<IGameEngineGameState> StartGamespace(Data.Challenge entity);
    Task<IGameEngineGameState> StopGamespace(Data.Challenge entity);
}
