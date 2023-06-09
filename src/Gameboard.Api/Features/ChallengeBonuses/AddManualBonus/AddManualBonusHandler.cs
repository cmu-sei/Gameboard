using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.ChallengeBonuses;
using Gameboard.Api.Structure.MediatR.Authorizers;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Gameboard.Api.Features.ChallengeBonuses;

public record AddManualBonusCommand(string ChallengeId, CreateManualChallengeBonus Model) : IRequest;

internal class AddManualBonusHandler : IRequestHandler<AddManualBonusCommand>
{
    private readonly IStore<ManualChallengeBonus> _challengeBonusStore;
    private readonly User _actor;

    // validators
    private readonly AddManualBonusValidator _validator;

    // authorizers 
    private readonly UserRoleAuthorizer _roleAuthorizer;

    public AddManualBonusHandler(
        IStore<ManualChallengeBonus> challengeBonusStore,
        UserRoleAuthorizer roleAuthorizer,
        AddManualBonusValidator validator,
        IHttpContextAccessor httpContextAccessor)
    {
        _actor = httpContextAccessor.HttpContext.User.ToActor();
        _challengeBonusStore = challengeBonusStore;
        _roleAuthorizer = roleAuthorizer;
        _validator = validator;
    }

    public async Task Handle(AddManualBonusCommand request, CancellationToken cancellationToken)
    {
        _roleAuthorizer
            .AllowRoles(UserRole.Admin, UserRole.Support, UserRole.Designer)
            .Authorize();

        await _validator.Validate(request);

        await _challengeBonusStore.Create(new ManualChallengeBonus
        {
            ChallengeId = request.ChallengeId,
            Description = request.Model.Description,
            EnteredByUserId = _actor.Id,
            PointValue = request.Model.PointValue,
        });
    }
}
