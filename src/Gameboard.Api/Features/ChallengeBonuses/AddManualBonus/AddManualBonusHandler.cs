using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using MediatR;

namespace Gameboard.Api.Features.ChallengeBonuses;

public record AddManualBonusCommand(string ChallengeId, string TeamId, CreateManualBonus Model) : IRequest;

internal class AddManualBonusHandler : IRequestHandler<AddManualBonusCommand>
{
    private readonly IActingUserService _actingUserService;
    private readonly INowService _now;
    private readonly IStore _store;

    // validators
    private readonly IGameboardRequestValidator<AddManualBonusCommand> _validator;

    // authorizers 
    private readonly UserRoleAuthorizer _roleAuthorizer;

    public AddManualBonusHandler(
        IActingUserService actingUserService,
        INowService now,
        UserRoleAuthorizer roleAuthorizer,
        IStore store,
        IGameboardRequestValidator<AddManualBonusCommand> validator)
    {
        _actingUserService = actingUserService;
        _now = now;
        _roleAuthorizer = roleAuthorizer;
        _store = store;
        _validator = validator;
    }

    public async Task Handle(AddManualBonusCommand request, CancellationToken cancellationToken)
    {
        _roleAuthorizer
            .AllowRoles(UserRole.Admin, UserRole.Support, UserRole.Designer)
            .Authorize();

        await _validator.Validate(request, cancellationToken);

        // this endpoint can either of two entities (using EF table-per-hierarchy)
        // if the challengeId is set, it's a manual challenge bonus, otherwise it's
        // a manual team bonus
        if (request.ChallengeId.IsNotEmpty())
            await _store.Create(new ManualChallengeBonus
            {
                ChallengeId = request.ChallengeId,
                Description = request.Model.Description,
                EnteredOn = _now.Get(),
                EnteredByUserId = _actingUserService.Get().Id,
                PointValue = request.Model.PointValue
            });
        else
            await _store.Create(new ManualTeamBonus
            {
                TeamId = request.TeamId,
                Description = request.Model.Description,
                EnteredOn = _now.Get(),
                EnteredByUserId = _actingUserService.Get().Id,
                PointValue = request.Model.PointValue
            });
    }
}
