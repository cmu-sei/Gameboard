using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Players;
using MediatR;

namespace Gameboard.Api.Features.Games;

public interface IGameHubNotificationsHandler : INotificationHandler<PlayerEnrolledNotification>, INotificationHandler<PlayerUnenrolledNotification> { }

internal class GameHubNotificationsHandler : IGameHubNotificationsHandler
{
    private readonly IGameHubService _gameHubService;

    public GameHubNotificationsHandler(IGameHubService gameHubService)
    {
        _gameHubService = gameHubService;
    }

    public Task Handle(PlayerEnrolledNotification notification, CancellationToken cancellationToken)
        => _gameHubService.SendYourActiveGamesChanged(notification.Context.UserId);

    public Task Handle(PlayerUnenrolledNotification notification, CancellationToken cancellationToken)
        => _gameHubService.SendYourActiveGamesChanged(notification.Context.UserId);
}
