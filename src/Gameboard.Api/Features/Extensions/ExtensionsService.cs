// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Scores;
using Gameboard.Api.Features.Support;
using Gameboard.Api.Features.Teams;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Extensions;

public interface IExtensionsService { }

internal class ExtensionsService
(
    CoreOptions coreOptions,
    IHttpClientFactory httpClientFactory,
    IScoringService scoringService,
    IStore store,
    ITeamService teamService
) : IExtensionsService, INotificationHandler<ScoreChangedNotification>, INotificationHandler<TicketCreatedNotification>
{
    public async Task Handle(ScoreChangedNotification notification, CancellationToken cancellationToken)
    {
        var captain = await teamService.ResolveCaptain(notification.TeamId, cancellationToken);
        var score = await scoringService.GetTeamScore(notification.TeamId, cancellationToken);
        var game = await store
            .WithNoTracking<Data.Game>()
            .Select(g => new
            {
                g.Id,
                g.Name
            })
            .SingleAsync(g => g.Id == captain.GameId, cancellationToken);

        await NotifyScored(new ExtensionsTeamScoredEvent
        {
            Team = new SimpleEntity { Id = notification.TeamId, Name = captain.Name },
            Game = new SimpleEntity { Id = captain.GameId, Name = game.Name },
            Rank = 1,
            ScoringSummary = new()
            {
                ChallengeName = score.Challenges.FirstOrDefault()?.Name ?? "Unknown Challenge",
                Points = score.Challenges.FirstOrDefault()?.Score?.TotalScore ?? 0
            }
        }, cancellationToken);
    }

    public Task Handle(TicketCreatedNotification notification, CancellationToken cancellationToken)
        => NotifyTicketCreated(notification, cancellationToken);

    private async Task NotifyScored(ExtensionsTeamScoredEvent ev, CancellationToken cancellationToken)
    {
        var pointsDescription = $"on challenge _{ev.ScoringSummary.ChallengeName}_";

        if (ev.ScoringSummary.ChallengeName.IsNotEmpty())
        {
            pointsDescription = $"on challenge _{ev.ScoringSummary.ChallengeName}_";

            if (ev.ScoringSummary.IsChallengeManualBonus)
                pointsDescription += " (manual bonus)";
        }
        else if (ev.ScoringSummary.IsTeamManualBonus)
            pointsDescription = "from a team manual bonus";

        var text = $"It's time for a score update on **{ev.Game.Name}!** {ev.Team.Name} just scored {ev.ScoringSummary.Points} points {pointsDescription}. They're now rank **{ev.Rank}**!";
        var extensions = await GetEnabledExtensions(cancellationToken);
        foreach (var extension in extensions)
            await extension.NotifyScored(new ExtensionMessage { Text = text });
    }

    private async Task NotifyTicketCreated(TicketCreatedNotification ev, CancellationToken cancellationToken)
    {
        var text = $"""{ev.Creator.Name} just sent us a new ticket ({ev.FullKey}): _"{ev.Title}"_. Head to [{coreOptions.AppName}]({coreOptions.AppUrl}/support/tickets/{ev.Key}) to check it out.""";
        var extensions = await GetEnabledExtensions(cancellationToken);

        foreach (var extension in extensions)
            await extension.NotifyTicketCreated(new ExtensionMessage { Text = text });
    }

    private async Task<IEnumerable<IExtensionService>> GetEnabledExtensions(CancellationToken cancellationToken)
    {
        var configuredExtensions = new List<IExtensionService>();
        var allExtensions = await store
            .WithNoTracking<Extension>()
            .Where(e => e.HostUrl != null && e.HostUrl != "")
            .Where(e => e.Token != null && e.Token != "")
            .Where(e => e.IsEnabled)
            .ToArrayAsync(cancellationToken);

        var mm = allExtensions.Where(e => e.Type == ExtensionType.Mattermost).SingleOrDefault();

        if (mm is not null)
            configuredExtensions.Add(new MattermostExtensionService(mm, httpClientFactory));

        return configuredExtensions;
    }
}
