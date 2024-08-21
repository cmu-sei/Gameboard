using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.games;

public record DeleteGameCommand(string GameId) : IRequest;

internal class DeleteGameHandler(
    EntityExistsValidator<DeleteGameCommand, Data.Game> gameExists,
    IGameService gameService,
    IStore store,
    IValidatorService<DeleteGameCommand> validatorService
    ) : IRequestHandler<DeleteGameCommand>
{
    private readonly EntityExistsValidator<DeleteGameCommand, Data.Game> _gameExists = gameExists;
    private readonly IGameService _gameService = gameService;
    private readonly IStore _store = store;
    private readonly IValidatorService<DeleteGameCommand> _validatorService = validatorService;

    public async Task Handle(DeleteGameCommand request, CancellationToken cancellationToken)
    {
        // auth/validate
        await _validatorService
            .ConfigureAuthorization(config => config.RequirePermissions(PermissionKey.Games_CreateEditDelete))
            .AddValidator(_gameExists.UseProperty(r => r.GameId))
            .AddValidator
            (
                async (req, ctx) =>
                {
                    var playerCount = await _store
                        .WithNoTracking<Data.Player>()
                        .CountAsync(p => p.GameId == request.GameId);

                    if (playerCount > 0)
                        ctx.AddValidationException(new CantDeleteGameWithPlayers(request.GameId, playerCount));

                }
            )
            .Validate(request, cancellationToken);

        // do the things
        await _gameService.Delete(request.GameId);
    }
}
