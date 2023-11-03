using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.ChallengeBonuses;

public record DeleteGameAutoBonusesConfigCommand(string GameId) : IRequest;

internal class DeleteGameAutoBonusesConfigHandler : IRequestHandler<DeleteGameAutoBonusesConfigCommand>
{
    private readonly UserRoleAuthorizer _authorizer;
    private readonly IStore _store;
    private readonly IGameboardRequestValidator<DeleteGameAutoBonusesConfigCommand> _validator;

    public DeleteGameAutoBonusesConfigHandler
    (
        UserRoleAuthorizer authorizer,
        IStore store,
        IGameboardRequestValidator<DeleteGameAutoBonusesConfigCommand> validator
    )
    {
        _authorizer = authorizer;
        _store = store;
        _validator = validator;
    }

    public async Task Handle(DeleteGameAutoBonusesConfigCommand request, CancellationToken cancellationToken)
    {
        _authorizer
            .AllowRoles(UserRole.Admin, UserRole.Designer, UserRole.Tester)
            .Authorize();

        await _validator.Validate(request, cancellationToken);

        await _store
            .WithNoTracking<Data.ChallengeBonus>()
            .Where(s => s.ChallengeSpec.GameId == request.GameId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
