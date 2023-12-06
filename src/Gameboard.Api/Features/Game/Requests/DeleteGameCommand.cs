using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.games;

public record DeleteGameCommand(string GameId) : IRequest;

internal class DeleteGameHandler : IRequestHandler<DeleteGameCommand>
{
    private readonly EntityExistsValidator<DeleteGameCommand, Data.Game> _gameExists;
    private readonly IGameService _gameService;
    private readonly IStore _store;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<DeleteGameCommand> _validatorService;

    public DeleteGameHandler
    (
        EntityExistsValidator<DeleteGameCommand, Data.Game> gameExists,
        IGameService gameService,
        IStore store,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<DeleteGameCommand> validatorService
    )
    {
        _gameExists = gameExists;
        _gameService = gameService;
        _store = store;
        _userRoleAuthorizer = userRoleAuthorizer;
        _validatorService = validatorService;
    }

    public async Task Handle(DeleteGameCommand request, CancellationToken cancellationToken)
    {
        // auth/validate
        _userRoleAuthorizer
            .AllowRoles(UserRole.Designer, UserRole.Admin)
            .Authorize();

        _validatorService.AddValidator(_gameExists.UseProperty(r => r.GameId));
        _validatorService.AddValidator
        (
            async (req, ctx) =>
            {
                var playerCount = await _store
                    .WithNoTracking<Data.Player>()
                    .CountAsync(p => p.GameId == request.GameId);

                if (playerCount > 0)
                    ctx.AddValidationException(new CantDeleteGameWithPlayers(request.GameId, playerCount));

            }
        );

        await _validatorService.Validate(request, cancellationToken);

        // do the things
        await _gameService.Delete(request.GameId);
    }
}
