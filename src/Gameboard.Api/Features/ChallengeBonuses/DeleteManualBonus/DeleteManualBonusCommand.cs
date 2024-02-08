using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Scores;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.ChallengeBonuses;

public record DeleteManualBonusCommand(string ManualBonusId) : IRequest;

internal class DeleteManualBonusCommandHandler : IRequestHandler<DeleteManualBonusCommand>
{
    private readonly IMediator _mediator;
    private readonly IStore _store;

    // validators
    private readonly EntityExistsValidator<DeleteManualBonusCommand, ManualBonus> _bonusExists;
    private readonly IValidatorService<DeleteManualBonusCommand> _validatorService;

    // authorizers 
    private readonly UserRoleAuthorizer _roleAuthorizer;

    public DeleteManualBonusCommandHandler(
        EntityExistsValidator<DeleteManualBonusCommand, ManualBonus> bonusExists,
        IMediator mediator,
        UserRoleAuthorizer roleAuthorizer,
        IStore store,
        IValidatorService<DeleteManualBonusCommand> validatorService)
    {
        _bonusExists = bonusExists;
        _mediator = mediator;
        _roleAuthorizer = roleAuthorizer;
        _store = store;
        _validatorService = validatorService;
    }

    public async Task Handle(DeleteManualBonusCommand request, CancellationToken cancellationToken)
    {
        // authorize and validate
        _roleAuthorizer
            .AllowRoles(UserRole.Admin, UserRole.Support, UserRole.Designer)
            .Authorize();

        _validatorService.AddValidator(_bonusExists.UseProperty(r => r.ManualBonusId));

        // before we delete, we need to know whose score is changing
        var bonus = await _store.WithNoTracking<ManualBonus>().SingleAsync(b => b.Id == request.ManualBonusId);
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
