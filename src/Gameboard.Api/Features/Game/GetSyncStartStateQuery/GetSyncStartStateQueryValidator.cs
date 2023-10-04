using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Games.Validators;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;

namespace Gameboard.Api.Features.Games;

internal class GetSyncStartStateQueryValidator : IGameboardRequestValidator<GetSyncStartStateQuery>
{
    private readonly EntityExistsValidator<GetSyncStartStateQuery, Data.Game> _gameExists;
    private readonly IGameService _gameService;
    private readonly UserIsPlayingGameValidator<GetSyncStartStateQuery> _userIsPlayingGame;
    private readonly IValidatorService<GetSyncStartStateQuery> _validatorService;

    public GetSyncStartStateQueryValidator
    (
        EntityExistsValidator<GetSyncStartStateQuery, Data.Game> gameExists,
        IGameService gameService,
        UserIsPlayingGameValidator<GetSyncStartStateQuery> userIsPlayingGame,
        IValidatorService<GetSyncStartStateQuery> validatorService
    )
    {
        _gameExists = gameExists;
        _gameService = gameService;
        _userIsPlayingGame = userIsPlayingGame;
        _validatorService = validatorService;
    }

    public async Task Validate(GetSyncStartStateQuery request, CancellationToken cancellationToken)
    {
        // game must exist
        _validatorService.AddValidator(_gameExists.UseProperty(r => r.GameId));

        // user must have registered for the game
        _validatorService.AddValidator
        (
            _userIsPlayingGame
                .UseGameIdProperty(r => r.GameId)
                .UseUserIdProperty(r => r.ActingUser)
        );

        // game must be a sync start game
        _validatorService.AddValidator(async (request, context) =>
        {
            var game = await _gameService.Retrieve(request.GameId);
            if (!game.RequireSynchronizedStart)
            {
                context.AddValidationException(new GameIsNotSyncStart(request.GameId, "Can't read the sync start state of a non-sync-start game."));
            }
        });
        await _validatorService.Validate(request, cancellationToken);
    }
}
