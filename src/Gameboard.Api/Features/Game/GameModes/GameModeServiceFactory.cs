// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games.External;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gameboard.Api.Features.Games;

public interface IGameModeServiceFactory
{
    Task<IGameModeService> Get(string gameId);
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

    public async Task<IGameModeService> Get(string gameId)
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
                _serviceProvider.GetRequiredService<IExternalSyncGameModeService>() :
                _serviceProvider.GetRequiredService<IExternalGameModeService>();
        }

        if (!game.RequireSynchronizedStart)
            return _serviceProvider.GetRequiredService<IStandardGameModeService>();

        throw new NotImplementedException($"Game {gameId} has an unsupported mode ({game.RequireSynchronizedStart}/{game.Mode})");
    }
}
