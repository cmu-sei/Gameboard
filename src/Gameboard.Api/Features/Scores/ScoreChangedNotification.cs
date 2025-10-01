// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Gameboard.Api.Features.Scores;

public record ScoreChangedNotification(string TeamId) : INotification;

internal class ScoreChangedNotificationHandler(IScoreDenormalizationService scoringDenormalizationService) : INotificationHandler<ScoreChangedNotification>
{
    private readonly IScoreDenormalizationService _scoreDenormalizationService = scoringDenormalizationService;

    public async Task Handle(ScoreChangedNotification notification, CancellationToken cancellationToken)
    {
        await _scoreDenormalizationService.DenormalizeTeam(notification.TeamId, cancellationToken);
    }
}
