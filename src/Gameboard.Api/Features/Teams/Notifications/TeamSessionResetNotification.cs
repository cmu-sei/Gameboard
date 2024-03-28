using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Services;
using MediatR;

namespace Gameboard.Api.Features.Teams;

public record TeamSessionResetNotification(string GameId, string TeamId) : INotification;

internal class TeamSessionResetHandler : INotificationHandler<TeamSessionResetNotification>
{
    private readonly GameService _gameService;

    public TeamSessionResetHandler
    (
        GameService gameService
    )
    {
        _gameService = gameService;
    }

    public async Task Handle(TeamSessionResetNotification notification, CancellationToken cancellationToken)
    {
        await _gameService.ReRank(notification.GameId);
    }
}
