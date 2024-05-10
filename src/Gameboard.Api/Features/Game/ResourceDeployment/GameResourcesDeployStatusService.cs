using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Games;

public interface IGameResourcesDeployStatusService
{
    Task<GameResourcesDeployStatus> GetStatus(string gameId);
}

internal class GameResourcesDeployStatusService : IGameResourcesDeployStatusService, INotificationHandler<ChallengeDeployedNotification>
{
    private readonly Dictionary<string, GameResourcesDeployStatus> _deployingGames = new();
    private readonly IStore _store;

    public GameResourcesDeployStatusService(IStore store)
    {
        _store = store;
    }

    public async Task Handle(ChallengeDeployedNotification notification, CancellationToken cancellationToken)
    {
        var gameId = await _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => c.Id == notification.Challenge.Id)
            .Select(c => c.GameId)
            .SingleAsync(cancellationToken);

        if (_deployingGames.TryGetValue(gameId, out var gameDeployStatus))
        {
            // _deployingGames[gameId].ChallengesCreated
        }
    }

    public async Task<GameResourcesDeployStatus> GetStatus(string gameId)
    {
        return await Task.FromResult<GameResourcesDeployStatus>(null);
    }
}
