using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Structure;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Games;

internal class IsSyncStartReadyQueryHandler : IRequestHandler<IsSyncStartReadyQuery, SyncStartState>
{
    private readonly EntityExistsValidator<IsSyncStartReadyQuery, Data.Game> _gameExists;
    private readonly IGameStore _gameStore;
    private readonly IPlayerStore _playerStore;
    private readonly IValidatorService<IsSyncStartReadyQuery> _validatorService;

    public IsSyncStartReadyQueryHandler
    (
        EntityExistsValidator<IsSyncStartReadyQuery, Data.Game> gameExists,
        IGameStore gameStore,
        IPlayerStore playerStore,
        IValidatorService<IsSyncStartReadyQuery> validatorService
    )
    {
        _gameExists = gameExists;
        _gameStore = gameStore;
        _playerStore = playerStore;
        _validatorService = validatorService;
    }

    public async Task<SyncStartState> Handle(IsSyncStartReadyQuery request, CancellationToken cancellationToken)
    {
        _validatorService.AddValidator(_gameExists.UseProperty(r => r.gameId));
        await _validatorService.Validate(request);

        // a game and its challenges are "sync start ready" if either of the following are true:
        // - the game is NOT a sync-start game
        // - the game is sync-start game, and all registered players have set their IsReady flag to true.

        var game = await _gameStore.Retrieve(request.gameId);
        if (!game.RequireSynchronizedStart)
        {
            return new SyncStartState
            {
                Game = new SimpleEntity { Id = game.Id, Name = game.Name },
                Teams = new SyncStartTeam[] { },
                IsReady = true
            };
        }

        var teams = new List<SyncStartTeam>();
        var teamPlayers = await _playerStore
            .List()
            .Where(p => p.GameId == request.gameId)
            .GroupBy(p => p.TeamId)
            .ToDictionaryAsync(tp => tp.Key);
        var allTeamsReady = teamPlayers.All(team => team.Value.All(p => p.IsReady));

        return new SyncStartState
        {
            Game = new SimpleEntity { Id = game.Id, Name = game.Name },
            Teams = teamPlayers.Keys.Select(teamId => new SyncStartTeam
            {
                Id = teamId,
                Name = teamPlayers[teamId].Single(p => p.IsManager).ApprovedName,
                Players = teamPlayers[teamId].Select(p => new SyncStartPlayer
                {
                    Id = p.Id,
                    Name = p.ApprovedName,
                    IsReady = p.IsReady
                }),
                IsReady = teamPlayers[teamId].All(p => p.IsReady)
            }),
            IsReady = allTeamsReady
        };
    }
}
