using System.Threading.Tasks;
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

    public async Task Validate(GetSyncStartStateQuery request)
    {
        // game must exist
        _validatorService.AddValidator(_gameExists.UseProperty(r => r.gameId));

        // user must have registered for the game
        _userIsPlayingGame.GetGameId = r => r.gameId;
        _userIsPlayingGame.GetUser = r => r.ActingUser;
        _validatorService.AddValidator(_userIsPlayingGame);

        // game must be a sync start game
        _validatorService.AddValidator(async (request, context) =>
        {
            var game = await _gameService.Retrieve(request.gameId);
            if (!game.RequireSynchronizedStart)
            {
                context.AddValidationException(new GameIsNotSyncStart(request.gameId, "Can't read the sync start state of a non-sync-start game."));
            }
        });
        await _validatorService.Validate(request);
    }
}
