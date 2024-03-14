using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Games.External;
using Gameboard.Api.Features.Scores;
using Gameboard.Api.Services;
using MediatR;

namespace Gameboard.Api.Features.Teams;

public record TeamSessionResetNotification(string GameId, string TeamId) : INotification;

internal class TeamSessionResetHandler : INotificationHandler<TeamSessionResetNotification>
{
    private readonly ChallengeService _challengeService;
    private readonly IExternalGameService _externalGameService;
    private readonly GameService _gameService;
    private readonly IScoreDenormalizationService _scoreDenormalizationService;

    public TeamSessionResetHandler
    (
        ChallengeService challengeService,
        IExternalGameService externalGameService,
        GameService gameService,
        IScoreDenormalizationService scoreDenormalizationService
    )
    {
        _challengeService = challengeService;
        _externalGameService = externalGameService;
        _gameService = gameService;
        _scoreDenormalizationService = scoreDenormalizationService;
    }

    public async Task Handle(TeamSessionResetNotification notification, CancellationToken cancellationToken)
    {
        // archive challenges
        await _challengeService.ArchiveTeamChallenges(notification.TeamId);

        // delete data for any external games
        await _externalGameService.DeleteTeamExternalData(cancellationToken, notification.TeamId);

        // also update scoreboards
        await _scoreDenormalizationService.DenormalizeGame(notification.GameId, cancellationToken);
        await _gameService.ReRank(notification.GameId);
    }
}
