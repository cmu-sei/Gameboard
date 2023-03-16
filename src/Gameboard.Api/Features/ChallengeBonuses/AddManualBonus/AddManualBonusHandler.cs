using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.ChallengeBonuses;
using Gameboard.Api.Structure.MediatR.Authorizers;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Gameboard.Api.Features.GameEngine.Requests;

internal class AddManualBonusHandler : IRequestHandler<AddManualBonusCommand>
{
    private readonly IChallengeBonusStore _challengeBonusStore;
    private readonly User _actor;

    // validators
    private readonly AddManualBonusValidator _validator;

    // authorizers 
    private readonly UserRoleAuthorizer _roleAuthorizer;

    public AddManualBonusHandler(
        IChallengeBonusStore challengeBonusStore,
        UserRoleAuthorizer roleAuthorizer,
        AddManualBonusValidator validator,
        IHttpContextAccessor httpContextAccessor)
    {
        _actor = httpContextAccessor.HttpContext.User.ToActor();
        _challengeBonusStore = challengeBonusStore;
        _roleAuthorizer = roleAuthorizer;
        _validator = validator;

        roleAuthorizer.AllowedRoles = new UserRole[] { UserRole.Admin, UserRole.Support, UserRole.Designer };
    }

    public async Task Handle(AddManualBonusCommand request, CancellationToken cancellationToken)
    {
        if (!_roleAuthorizer.Authorize(_actor))
            throw new ActionForbidden();

        await _validator.Validate(request);
        await _challengeBonusStore.AddManualBonus(request.challengeId, request.model, _actor);
    }
}
