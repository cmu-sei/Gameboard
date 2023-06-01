using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;

namespace Gameboard.Api.Features.ChallengeBonuses;

public record DeleteManualBonusCommand(string ManualBonusId) : IRequest;

internal class DeleteManualBonusCommandHandler : IRequestHandler<DeleteManualBonusCommand>
{
    private readonly IStore<ManualChallengeBonus> _challengeBonusStore;

    // validators
    private readonly EntityExistsValidator<DeleteManualBonusCommand, ManualChallengeBonus> _bonusExists;
    private readonly IValidatorService<DeleteManualBonusCommand> _validatorService;

    // authorizers 
    private readonly UserRoleAuthorizer _roleAuthorizer;

    public DeleteManualBonusCommandHandler(
        IStore<ManualChallengeBonus> challengeBonusStore,
        EntityExistsValidator<DeleteManualBonusCommand, ManualChallengeBonus> bonusExists,
        UserRoleAuthorizer roleAuthorizer,
        IValidatorService<DeleteManualBonusCommand> validatorService)
    {
        _bonusExists = bonusExists;
        _challengeBonusStore = challengeBonusStore;
        _roleAuthorizer = roleAuthorizer;

        roleAuthorizer.AllowedRoles = new UserRole[] { UserRole.Admin, UserRole.Support, UserRole.Designer };
        _validatorService = validatorService;
    }

    public async Task Handle(DeleteManualBonusCommand request, CancellationToken cancellationToken)
    {
        _roleAuthorizer.Authorize();
        _validatorService.AddValidator(_bonusExists.UseProperty(r => r.ManualBonusId));
        await _challengeBonusStore.Delete(request.ManualBonusId);
    }
}
