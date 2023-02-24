using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.GameEngine;

public interface IGameEngineStore
{
    Task<IGameEngineGameState> GetGameStateByChallenge(string challengeId);
    Task<IGameEngineGameState> GetGameStateByChallengeSpec(string challengeSpecId);
    Task<IGameEngineGameState> GetGameStateByPlayer(string playerId);
    Task<IGameEngineGameState> GetGameStateByTeam(string teamId);
}

public class GameEngineStore : IGameEngineStore
{
    private readonly GameboardDbContext _db;

    public GameEngineStore(GameboardDbContext db)
    {
        _db = db;
    }

    public Task<IGameEngineGameState> GetGameStateByPlayer(string playerId)
        => GetGameState(playerId: playerId);

    public Task<IGameEngineGameState> GetGameStateByChallenge(string challengeId)
        => GetGameState(challengeId: challengeId);

    public Task<IGameEngineGameState> GetGameStateByChallengeSpec(string challengeSpecId)
        => GetGameState(challengeSpecId: challengeSpecId);

    public Task<IGameEngineGameState> GetGameStateByTeam(string teamId)
        => GetGameState(teamId: teamId);

    private async Task<IGameEngineGameState> GetGameState(string playerId = "", string challengeId = "", string challengeSpecId = "", string teamId = "")
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

        var deserialized = JsonSerializer.Deserialize<GameEngineGameState>(results.First());
        return deserialized;
    }
}
