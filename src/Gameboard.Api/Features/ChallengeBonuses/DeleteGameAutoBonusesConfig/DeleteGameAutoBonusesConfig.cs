using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.ChallengeBonuses;

public record DeleteGameAutoBonusesConfigCommand(string GameId) : IRequest;

internal class DeleteGameAutoBonusesConfigHandler : IRequestHandler<DeleteGameAutoBonusesConfigCommand>
{
    private readonly UserRoleAuthorizer _authorizer;
    private readonly IChallengeBonusStore _challengeBonusStore;
    private readonly EntityExistsValidator<DeleteGameAutoBonusesConfigCommand, Data.Game> _gameExists;
    private readonly GameHasNoAwardedAutoBonuses<DeleteGameAutoBonusesConfigCommand> _gameHasNoAwardedAutoBonuses;
    private readonly IValidatorService<DeleteGameAutoBonusesConfigCommand> _validatorService;

    public DeleteGameAutoBonusesConfigHandler
    (
        UserRoleAuthorizer authorizer,
        IChallengeBonusStore challengeBonusStore,
        EntityExistsValidator<DeleteGameAutoBonusesConfigCommand, Data.Game> gameExists,
        GameHasNoAwardedAutoBonuses<DeleteGameAutoBonusesConfigCommand> gameHasNoAwardedAutoBonuses,
        IValidatorService<DeleteGameAutoBonusesConfigCommand> validatorService
    )
    {
        _authorizer = authorizer;
        _challengeBonusStore = challengeBonusStore;
        _gameHasNoAwardedAutoBonuses = gameHasNoAwardedAutoBonuses;
        _gameExists = gameExists;
        _validatorService = validatorService;
    }

    public async Task Handle(DeleteGameAutoBonusesConfigCommand request, CancellationToken cancellationToken)
    {
        _authorizer.AllowedRoles = UserRoleAuthorizer.RoleList(UserRole.Admin, UserRole.Designer, UserRole.Tester);
        _authorizer.Authorize();

        // TODO: validate that no bonuses have been distributed with this game id
        _validatorService.AddValidator(_gameExists.UseProperty(req => req.GameId));
        _validatorService.AddValidator(_gameHasNoAwardedAutoBonuses.UseProperty(req => req.GameId));

        await _validatorService.Validate(request);

        await _challengeBonusStore
            .DbContext
            .ChallengeBonuses
            .Where(s => s.ChallengeSpec.GameId == request.GameId)
            .ExecuteDeleteAsync();
    }
}
