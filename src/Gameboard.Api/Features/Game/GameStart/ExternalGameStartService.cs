using System;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Games.Start;
using Gameboard.Api.Features.Teams;

namespace Gameboard.Api.Features.Games.External;

public interface IExternalGameStartService : IGameModeStartService { }

internal class ExternalGameStartService : IExternalGameStartService
{
    public TeamSessionResetType StartFailResetType => TeamSessionResetType.PreserveChallenges;

    public Task<GameStartDeployedResources> DeployResources(GameModeStartRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<GamePlayState> GetGamePlayState(string gameId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<GameStartContext> Start(GameModeStartRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task TryCleanUpFailedDeploy(GameModeStartRequest request, Exception exception, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task ValidateStart(GameModeStartRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
