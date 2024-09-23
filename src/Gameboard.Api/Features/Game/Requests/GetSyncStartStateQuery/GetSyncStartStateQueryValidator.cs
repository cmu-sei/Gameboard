using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Games.Validators;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;

namespace Gameboard.Api.Features.Games;

internal class GetSyncStartStateQueryValidator : IGameboardRequestValidator<GetSyncStartStateQuery>
{
    private readonly EntityExistsValidator<GetSyncStartStateQuery, Data.Game> _gameExists;
    private readonly IGameService _gameService;
    private readonly IUserRolePermissionsService _permissionsServivce;
    private readonly UserIsPlayingGameValidator<GetSyncStartStateQuery> _userIsPlayingGame;
    private readonly IValidatorService<GetSyncStartStateQuery> _validatorService;

    public GetSyncStartStateQueryValidator
    (
        EntityExistsValidator<GetSyncStartStateQuery, Data.Game> gameExists,
        IGameService gameService,
        IUserRolePermissionsService permissionsService,
        UserIsPlayingGameValidator<GetSyncStartStateQuery> userIsPlayingGame,
        IValidatorService<GetSyncStartStateQuery> validatorService
    )
    {
        _gameExists = gameExists;
        _gameService = gameService;
        _permissionsServivce = permissionsService;
        _userIsPlayingGame = userIsPlayingGame;
        _validatorService = validatorService;
    }

    public async Task Validate(GetSyncStartStateQuery request, CancellationToken cancellationToken)
    {
        // game must exist
        _validatorService.AddValidator(_gameExists.UseProperty(r => r.GameId));

        // user must have registered for the game (or be able to observe)
        if (!await _permissionsServivce.Can(PermissionKey.Teams_Observe))
        {
            _validatorService.AddValidator
            (
                _userIsPlayingGame
                    .UseGameIdProperty(r => r.GameId)
                    .UseUserProperty(r => r.ActingUser)
            );
        }

        // game must be a sync start game
        _validatorService.AddValidator(async (request, context) =>
        {
            var game = await _gameService.Retrieve(request.GameId);
            if (!game.RequireSynchronizedStart)
            {
                context.AddValidationException(new ExternalGameIsNotSyncStart(request.GameId, "Can't read the sync start state of a non-sync-start game."));
            }
        });
        await _validatorService.Validate(request, cancellationToken);
    }
}
