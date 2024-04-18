using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Scores;
using Gameboard.Api.Features.Teams;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Extensions;

public interface IExtensionsService
{
    Task NotifyScored(ExtensionsTeamScoredEvent ev, CancellationToken cancellationToken);
}

internal class ExtensionsService : IExtensionsService, INotificationHandler<ScoreChangedNotification>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IScoringService _scoringService;
    private readonly IStore _store;
    private readonly ITeamService _teamService;

    public ExtensionsService
    (
        IHttpClientFactory httpClientFactory,
        IScoringService scoringService,
        IStore store,
        ITeamService teamService
    )
    {
        _httpClientFactory = httpClientFactory;
        _scoringService = scoringService;
        _store = store;
        _teamService = teamService;
    }

    public async Task Handle(ScoreChangedNotification notification, CancellationToken cancellationToken)
    {
        var captain = await _teamService.ResolveCaptain(notification.TeamId, cancellationToken);
        var score = await _scoringService.GetTeamScore(notification.TeamId, cancellationToken);

        await NotifyScored(new ExtensionsTeamScoredEvent
        {
            Team = new SimpleEntity { Id = notification.TeamId, Name = captain.Name },
            Game = new SimpleEntity { Id = captain.GameId, Name = captain.Game.Name },
            Rank = 1,
            ScoringSummary = new()
            {
                ChallengeName = score.Challenges.First().Name,
            }
        }, cancellationToken);
    }

    public async Task NotifyScored(ExtensionsTeamScoredEvent ev, CancellationToken cancellationToken)
    {
        var pointsDescription = $" on challenge _{ev.ScoringSummary.ChallengeName}_";

        if (ev.ScoringSummary.ChallengeName.IsNotEmpty())
        {
            pointsDescription = $" on challenge _{ev.ScoringSummary.ChallengeName}_";

            if (ev.ScoringSummary.IsChallengeManualBonus)
                pointsDescription = " (manual bonus)";
        }
        else if (ev.ScoringSummary.IsTeamManualBonus)
            pointsDescription = " from a team manual bonus";

        pointsDescription += ".";

        var text = $"**{ev.Game.Name.ToUpper()} SCORE UPDATE!** {ev.Team.Name} just scored {ev.ScoringSummary.Points} {pointsDescription}.";
        var extensions = await GetConfiguredExtensions(cancellationToken);
        foreach (var extension in extensions)
            await extension.NotifyScored(new ExtensionMessage { Text = text });
    }

    private async Task<IEnumerable<IExtensionService>> GetConfiguredExtensions(CancellationToken cancellationToken)
    {
        var configuredExtensions = new List<IExtensionService>();
        var allExtensions = await _store
            .WithNoTracking<Extension>()
            .Where(e => e.HostUrl != null && e.HostUrl != "")
            .Where(e => e.Token != null && e.Token != "")
            .ToArrayAsync(cancellationToken);

        var mm = allExtensions.Where(e => e.Type == ExtensionType.Mattermost).SingleOrDefault();

        if (mm is not null)
            configuredExtensions.Add(new MattermostExtensionService(mm, _httpClientFactory));

        return configuredExtensions;
    }
}
