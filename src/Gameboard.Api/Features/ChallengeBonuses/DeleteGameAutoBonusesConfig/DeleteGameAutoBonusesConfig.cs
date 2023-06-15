using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Structure.MediatR.Authorizers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.ChallengeBonuses;

public record DeleteGameAutoBonusesConfigCommand(string GameId) : IRequest;

internal class DeleteGameAutoBonusesConfigHandler : IRequestHandler<DeleteGameAutoBonusesConfigCommand>
{
    private readonly UserRoleAuthorizer _authorizer;
    private readonly IChallengeBonusStore _challengeBonusStore;
    private readonly DeleteGameAutoBonusesConfigValidator _validator;

    public DeleteGameAutoBonusesConfigHandler
    (
        UserRoleAuthorizer authorizer,
        IChallengeBonusStore challengeBonusStore,
        DeleteGameAutoBonusesConfigValidator validator
    )
    {
        _authorizer = authorizer;
        _challengeBonusStore = challengeBonusStore;
        _validator = validator;
    }

    public async Task Handle(DeleteGameAutoBonusesConfigCommand request, CancellationToken cancellationToken)
    {
        _authorizer
            .AllowRoles(UserRole.Admin, UserRole.Designer, UserRole.Tester)
            .Authorize();
        _authorizer.Authorize();

        await _validator
            .UseGameIdProperty(r => r.GameId)
            .Validate(request);

        await _challengeBonusStore
            .DbContext
            .ChallengeBonuses
            .Where(s => s.ChallengeSpec.GameId == request.GameId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
