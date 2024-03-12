using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Games.External;
using Gameboard.Api.Features.Scores;
using MediatR;

namespace Gameboard.Api.Features.Teams;

public record TeamDeletedNotification(string GameId, string TeamId) : INotification;

internal sealed class TeamDeletedHandler : INotificationHandler<TeamDeletedNotification>
{
    private readonly IExternalGameService _externalGameService;
    private readonly IScoreDenormalizationService _scoreDenormalizer;

    public TeamDeletedHandler
    (
        IExternalGameService externalGameService,
        IScoreDenormalizationService scoreDenormalizer
    )
    {
        _externalGameService = externalGameService;
        _scoreDenormalizer = scoreDenormalizer;
    }

    public async Task Handle(TeamDeletedNotification notification, CancellationToken cancellationToken)
    {
        await _externalGameService.DeleteTeamExternalData(cancellationToken, notification.TeamId);
        await _scoreDenormalizer.DenormalizeGame(notification.GameId, cancellationToken);
    }
}
