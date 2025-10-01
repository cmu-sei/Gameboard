// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Scores;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.ChallengeBonuses;

public record AddManualBonusCommand(string ChallengeId, string TeamId, CreateManualBonus Model) : IRequest;

internal class AddManualBonusHandler(
    IActingUserService actingUserService,
    IMediator mediator,
    INowService now,
    IStore store,
    IGameboardRequestValidator<AddManualBonusCommand> validator) : IRequestHandler<AddManualBonusCommand>
{
    private readonly IActingUserService _actingUserService = actingUserService;
    private readonly IMediator _mediator = mediator;
    private readonly INowService _now = now;
    private readonly IStore _store = store;

    // validators
    private readonly IGameboardRequestValidator<AddManualBonusCommand> _validator = validator;

    public async Task Handle(AddManualBonusCommand request, CancellationToken cancellationToken)
    {
        await _validator.Validate(request, cancellationToken);

        // this endpoint can either of two entities (using EF table-per-hierarchy)
        // if the challengeId is set, it's a manual challenge bonus, otherwise it's
        // a manual team bonus
        var resolvedTeamId = request.TeamId;
        if (request.ChallengeId.IsNotEmpty())
        {
            await _store.Create(new ManualChallengeBonus
            {
                ChallengeId = request.ChallengeId,
                Description = request.Model.Description,
                EnteredOn = _now.Get(),
                EnteredByUserId = _actingUserService.Get().Id,
                PointValue = request.Model.PointValue
            });

            // we need set the teamId based on this challenge since
            // the request doesn't pass it and the mediator notification
            // needs to know
            resolvedTeamId = await _store
                .WithNoTracking<Data.Challenge>()
                .Where(c => c.Id == request.ChallengeId)
                .Select(c => c.TeamId)
                .SingleAsync(cancellationToken);
        }
        else
            await _store.Create(new ManualTeamBonus
            {
                TeamId = request.TeamId,
                Description = request.Model.Description,
                EnteredOn = _now.Get(),
                EnteredByUserId = _actingUserService.Get().Id,
                PointValue = request.Model.PointValue
            });

        // adding a manual bonus will change the team's score, so we need to 
        // manually refresh the denormalization of the scoreboard
        await _mediator.Publish(new ScoreChangedNotification(resolvedTeamId), cancellationToken);
    }
}
