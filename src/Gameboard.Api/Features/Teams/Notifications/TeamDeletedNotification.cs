using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Games.External;
using Gameboard.Api.Features.Scores;
using Gameboard.Api.Services;
using MediatR;

namespace Gameboard.Api.Features.Teams;

public record TeamDeletedNotification(string GameId, string TeamId) : INotification;

internal sealed class TeamDeletedHandler : INotificationHandler<TeamDeletedNotification>
{
    private readonly ChallengeService _challengeService;
    private readonly IExternalGameService _externalGameService;
    private readonly IScoreDenormalizationService _scoreDenormalizer;

    public TeamDeletedHandler
    (
        ChallengeService challengeService,
        IExternalGameService externalGameService,
        IScoreDenormalizationService scoreDenormalizer
    )
    {
        _challengeService = challengeService;
        _externalGameService = externalGameService;
        _scoreDenormalizer = scoreDenormalizer;
    }

    public async Task Handle(TeamDeletedNotification notification, CancellationToken cancellationToken)
    {
        // under current behavior, a team being deleted should never have challenges because their session
        // must first be reset. but just in case, archive any remaining challenges (which sends the
        // "completed" notification to game engines).
        await _challengeService.ArchiveTeamChallenges(notification.TeamId);
        await _externalGameService.DeleteTeamExternalData(cancellationToken, notification.TeamId);
        await _scoreDenormalizer.DenormalizeGame(notification.GameId, cancellationToken);
    }
}
