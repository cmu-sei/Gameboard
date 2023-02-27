using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Services;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.GameEngine;

public interface IGameEngineStore
{
    Task<GameEngineGameState> GetGameStateByChallenge(string challengeId);
    Task<GameEngineGameState> GetGameStateByChallengeSpec(string challengeSpecId);
    Task<GameEngineGameState> GetGameStateByPlayer(string playerId);
    Task<GameEngineGameState> GetGameStateByTeam(string teamId);
}

public class GameEngineStore : IGameEngineStore
{
    private readonly GameboardDbContext _db;
    private readonly IJsonService _jsonService;

    public GameEngineStore(GameboardDbContext db, IJsonService jsonService)
    {
        _db = db;
        _jsonService = jsonService;
    }

    public Task<GameEngineGameState> GetGameStateByPlayer(string playerId)
        => GetGameState(playerId: playerId);

    public Task<GameEngineGameState> GetGameStateByChallenge(string challengeId)
        => GetGameState(challengeId: challengeId);

    public Task<GameEngineGameState> GetGameStateByChallengeSpec(string challengeSpecId)
        => GetGameState(challengeSpecId: challengeSpecId);

    public Task<GameEngineGameState> GetGameStateByTeam(string teamId)
        => GetGameState(teamId: teamId);

    private async Task<GameEngineGameState> GetGameState(string playerId = "", string challengeId = "", string challengeSpecId = "", string teamId = "")
    {
        if (string.IsNullOrWhiteSpace(string.Concat(playerId, challengeId, challengeSpecId, teamId)))
        {
            throw new ArgumentException("Can't retrieve game engine type without at least one argument.");
        }

        var query = _db.Challenges.AsQueryable();

        if (!string.IsNullOrWhiteSpace(playerId))
            query = query.Where(c => c.PlayerId == playerId);

        if (!string.IsNullOrWhiteSpace(challengeId))
            query = query.Where(c => c.Id == challengeId);

        if (!string.IsNullOrWhiteSpace(challengeSpecId))
            query = query.Where(c => c.SpecId == challengeSpecId);

        if (!string.IsNullOrWhiteSpace(teamId))
            query = query.Where(c => c.TeamId == teamId);

        var things = await _db.Challenges.Where(c => c.Id != "").ToListAsync();

        var results = await query.Select(c => c.State)
            .Distinct()
            .ToArrayAsync();

        if (results.Length != 1)
            throw new ArgumentException($"Couldn't resolve game engine type for arguments player ({playerId}), challenge ({challengeId}), challengeSpec ({challengeSpecId}), team ({teamId}).");

        return _jsonService.Deserialize<GameEngineGameState>(results[0]);
    }
}
