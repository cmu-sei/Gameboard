using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Scores;
using MediatR;

namespace Gameboard.Api.Features.Teams;

public record TeamSessionResetNotification(string GameId, string TeamId) : INotification;

internal class TeamSessionResetHandler(IScoreDenormalizationService scoreDenorm) : INotificationHandler<TeamSessionResetNotification>
{
    private readonly IScoreDenormalizationService _scoreDenorm = scoreDenorm;

    public async Task Handle(TeamSessionResetNotification notification, CancellationToken cancellationToken)
    {
        await _scoreDenorm.DenormalizeGame(notification.GameId, cancellationToken);
    }
}
