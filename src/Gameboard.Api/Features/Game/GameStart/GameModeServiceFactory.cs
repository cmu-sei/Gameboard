using System;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games.External;
using Gameboard.Api.Features.Games.Start;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gameboard.Api.Features.Games;

public interface IGameModeServiceFactory
{
    Task<IGameModeStartService> Get(string gameId);
}

internal class GameModeServiceFactory : IGameModeServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IStore _store;

    public GameModeServiceFactory(IServiceProvider serviceProvider, IStore store)
    {
        _serviceProvider = serviceProvider;
        _store = store;
    }

    public async Task<IGameModeStartService> Get(string gameId)
    {
        var game = await _store
            .WithNoTracking<Data.Game>()
            .Select(g => new
            {
                g.Id,
                g.Mode,
                g.RequireSynchronizedStart
            })
            .SingleAsync(g => g.Id == gameId);

        if (game.Mode == GameEngineMode.External)
        {
            return game.RequireSynchronizedStart ?
                _serviceProvider.GetRequiredService<IExternalSyncGameStartService>() :
                _serviceProvider.GetRequiredService<IExternalGameStartService>();
        }

        throw new NotImplementedException();
    }
}
