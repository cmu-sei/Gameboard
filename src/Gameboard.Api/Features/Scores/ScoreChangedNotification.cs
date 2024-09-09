using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using MediatR;

namespace Gameboard.Api.Features.Scores;

public record ScoreChangedNotification(string TeamId) : INotification;

internal class ScoreChangedNotificationHandler(
    IScoreDenormalizationService scoringDenormalizationService,
    IStore store
    ) : INotificationHandler<ScoreChangedNotification>
{
    private readonly IScoreDenormalizationService _scoreDenormalizationService = scoringDenormalizationService;
    private readonly IStore _store = store;

    public async Task Handle(ScoreChangedNotification notification, CancellationToken cancellationToken)
    {
        await _scoreDenormalizationService.DenormalizeTeam(notification.TeamId, cancellationToken);
    }
}
