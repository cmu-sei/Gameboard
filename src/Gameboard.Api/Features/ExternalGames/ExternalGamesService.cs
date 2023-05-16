using System.Threading.Tasks;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Services;

namespace Gameboard.Api.Features.ExternalGames;

internal interface IExternalGamesService
{
    Task StartExternalGame(string gameId);
}

internal class ExternalGamesService : IExternalGamesService
{
    private readonly IGameService _gameService;

    public ExternalGamesService(IGameService gameService)
    {
        _gameService = gameService;
    }

    public async Task StartExternalGame(string gameId)
    {
        var game = await _gameService.Retrieve(gameId);

        if (game.Mode != GameMode.External)
            throw new GameModeIsntExternal(gameId, game.Mode);

        var challenges = await _gameService.RetrieveChallenges(gameId);
    }

    // public async Task BuildGameMetaData(Data.Game game)
    // {

    // }
}
