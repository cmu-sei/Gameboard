using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Services;
using MediatR;

namespace Gameboard.Api.Features.Games;

public interface IGameStartService
{
    Task Start(string gameId, User actingUser = null);
}

internal class GameStartService : IGameStartService
{
    private readonly IGameService _gameService;
    private readonly IGameStore _gameStore;
    private readonly IMediator _mediator;
    private readonly PlayerService _playerService;
    private readonly IPlayerStore _playerStore;

    public GameStartService
    (
        IGameService gameService,
        IGameStore gameStore,
        IMediator mediator,
        PlayerService playerService,
        IPlayerStore playerStore
    )
    {
        _gameService = gameService;
        _gameStore = gameStore;
        _mediator = mediator;
        _playerService = playerService;
        _playerStore = playerStore;
    }

    public async Task Start(string gameId, User actingUser = null)
    {
        var game = await _gameStore.Retrieve(gameId);

        // three cases currently accommodated: standard challenges, sync start + external (unity), and
        // non-sync-start + external (cubespace)
        if (game.Mode == GameMode.Standard && !game.RequireSynchronizedStart)
        {
            await _mediator.Send(new StartStandardNonSyncGameCommand(gameId, actingUser));
        }

        if (game.Mode == GameMode.External && game.RequireSynchronizedStart)
        {

        }

        throw new System.NotImplementedException();
    }
}
