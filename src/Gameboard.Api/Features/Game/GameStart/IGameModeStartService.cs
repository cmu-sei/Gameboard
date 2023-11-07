using System;
using System.Threading;
using System.Threading.Tasks;

namespace Gameboard.Api.Features.Games.Start;

// Gameboard currently has four possible game modes:
//
// 1. Standard (VM) games, no synchronized start
// 2. Standard (VM) games, synchronized start
// 3. External (Unity) games, no synchronized start (e.g. PC4)
// 4. External (Unity) games, synchronized start
// 
// Each possible mode will eventually have an implementation of this interface that orchestrates 
// the logic that happens when a game starts (e.g. ExternalSyncGameStartService for #4)
public interface IGameModeStartService
{
    public Task ValidateStart(GameModeStartRequest request, CancellationToken cancellationToken);
    public Task<GameStartPhase> GetStartPhase(string gameId, string teamId, CancellationToken cancellationToken);
    public Task<GameStartContext> Start(GameModeStartRequest request, CancellationToken cancellationToken);
    public Task TryCleanUpFailedDeploy(GameModeStartRequest request, Exception exception);
}
