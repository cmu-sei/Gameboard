using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.ChallengeBonuses;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Gameboard.Api.Features.GameEngine.Requests;

internal class DeleteManualBonusCommandHandler : IRequestHandler<DeleteManualBonusCommand>
{
    private readonly IStore<ManualChallengeBonus> _challengeBonusStore;
    private readonly User _actor;

    // validators
    private readonly EntityExistsValidator<DeleteManualBonusCommand, ManualChallengeBonus> _bonusExists;

    // authorizers 
    private readonly UserRoleAuthorizer _roleAuthorizer;

    public DeleteManualBonusCommandHandler(
        IStore<ManualChallengeBonus> challengeBonusStore,
        EntityExistsValidator<DeleteManualBonusCommand, ManualChallengeBonus> bonusExists,
        UserRoleAuthorizer roleAuthorizer,
        IHttpContextAccessor httpContextAccessor)
    {
        _actor = httpContextAccessor.HttpContext.User.ToActor();
        _bonusExists = bonusExists;
        _challengeBonusStore = challengeBonusStore;
        _roleAuthorizer = roleAuthorizer;

        roleAuthorizer.AllowedRoles = new UserRole[] { UserRole.Admin, UserRole.Support, UserRole.Designer };
    }

    public async Task Handle(DeleteManualBonusCommand request, CancellationToken cancellationToken)
    {
        _roleAuthorizer.Authorize();
        await _bonusExists.Validate(request);
        await _challengeBonusStore.Delete(request.ManualBonusId);
    }
}
