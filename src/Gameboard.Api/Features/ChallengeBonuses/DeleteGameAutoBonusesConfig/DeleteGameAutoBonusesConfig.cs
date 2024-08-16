using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.ChallengeBonuses;

public record DeleteGameAutoBonusesConfigCommand(string GameId) : IRequest;

internal class DeleteGameAutoBonusesConfigHandler : IRequestHandler<DeleteGameAutoBonusesConfigCommand>
{
    private readonly IStore _store;
    private readonly IGameboardRequestValidator<DeleteGameAutoBonusesConfigCommand> _validator;

    public DeleteGameAutoBonusesConfigHandler
    (
        IStore store,
        IGameboardRequestValidator<DeleteGameAutoBonusesConfigCommand> validator
    )
    {
        _store = store;
        _validator = validator;
    }

    public async Task Handle(DeleteGameAutoBonusesConfigCommand request, CancellationToken cancellationToken)
    {
        await _validator.Validate(request, cancellationToken);

        await _store
            .WithNoTracking<ChallengeBonus>()
            .Where(s => s.ChallengeSpec.GameId == request.GameId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
