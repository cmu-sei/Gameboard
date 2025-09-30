// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Scores;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.ChallengeBonuses;

public record DeleteManualBonusCommand(string ManualBonusId) : IRequest;

internal class DeleteManualBonusCommandHandler(
    EntityExistsValidator<DeleteManualBonusCommand, ManualBonus> bonusExists,
    IMediator mediator,
    IStore store,
    IValidatorService<DeleteManualBonusCommand> validatorService) : IRequestHandler<DeleteManualBonusCommand>
{
    private readonly IMediator _mediator = mediator;
    private readonly IStore _store = store;

    // validators
    private readonly EntityExistsValidator<DeleteManualBonusCommand, ManualBonus> _bonusExists = bonusExists;
    private readonly IValidatorService<DeleteManualBonusCommand> _validatorService = validatorService;

    public async Task Handle(DeleteManualBonusCommand request, CancellationToken cancellationToken)
    {
        // authorize and validate
        await _validatorService
            .Auth(a => a.Require(PermissionKey.Scores_AwardManualBonuses))
            .AddValidator(_bonusExists.UseProperty(r => r.ManualBonusId))
            .Validate(request, cancellationToken);

        // before we delete, we need to know whose score is changing
        var bonus = await _store
            .WithNoTracking<ManualBonus>()
            .SingleAsync(b => b.Id == request.ManualBonusId, cancellationToken);
        string teamId;

        if (bonus is ManualChallengeBonus)
        {
            var challengeId = (bonus as ManualChallengeBonus).ChallengeId;
            teamId = await _store
                .WithNoTracking<Data.Challenge>()
                .Where(c => c.Id == challengeId)
                .Select(c => c.TeamId)
                .SingleAsync(cancellationToken);
        }
        else teamId = (bonus as ManualTeamBonus).TeamId;

        if (teamId == null)
            throw new CantResolveManualBonusType(request.ManualBonusId);

        await _store
            .WithNoTracking<ManualBonus>()
            .Where(b => b.Id == request.ManualBonusId)
            .ExecuteDeleteAsync(cancellationToken);

        // notify interested parties
        await _mediator.Publish(new ScoreChangedNotification(teamId), cancellationToken);
    }
}
