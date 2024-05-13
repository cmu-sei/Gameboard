using System.Collections.Concurrent;
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

internal class GameResourcesDeployStatusService : IGameResourcesDeployStatusService,
    INotificationHandler<ChallengeDeployedNotification>,
    INotificationHandler<GameLaunchStartedNotification>,
    INotificationHandler<GameResourcesDeployFailedNotification>
{
    private static readonly ConcurrentDictionary<string, GameResourcesDeployStatus> _gameDeploys = new();
    private readonly IStore _store;

    public GameResourcesDeployStatusService(IStore store)
    {
        _store = store;
    }

    public Task Handle(GameLaunchStartedNotification notification, CancellationToken cancellationToken)
        => EnsureGameEntry(notification.GameId, cancellationToken);

    public Task Handle(ChallengeDeployedNotification notification, CancellationToken cancellationToken)
    {

        var deploy = _gameDeploys[notification.GameId];
        var challenges = deploy.Challenges.ToList();
        var existingChallenge = challenges.SingleOrDefault(c => c.Id == notification.Challenge.Id);
        if (existingChallenge is not null)
            challenges.Add(notification.Challenge);

        return Task.CompletedTask;
    }

    public Task Handle(GameResourcesDeployFailedNotification notification, CancellationToken cancellationToken)
    {
        var brokenDeployments = _gameDeploys.Values.Where(d => d.Teams.Any(t => notification.TeamIds.Contains(t.Id)));
        foreach (var deployment in brokenDeployments)
            deployment.Error = notification.Message;

        return Task.CompletedTask;
    }

    public Task<GameResourcesDeployStatus> GetStatus(string gameId)
    {
        if (!_gameDeploys.TryGetValue(gameId, out var retVal))
            return Task.FromResult<GameResourcesDeployStatus>(null);

        return Task.FromResult(retVal);
    }

    private async Task EnsureGameEntry(string gameId, CancellationToken cancellationToken)
    {
        if (!_gameDeploys.TryGetValue(gameId, out var gameDeployStatus))
        {
            var game = await _store
                    .WithNoTracking<Data.Game>()
                    .Select(g => new SimpleEntity { Id = g.Id, Name = g.Name })
                    .SingleAsync(g => g.Id == gameId, cancellationToken);

            _gameDeploys.TryAdd(game.Id, new GameResourcesDeployStatus { Game = game });
        }
    }
}
