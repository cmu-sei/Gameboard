using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.GameEngine;

public interface IGameEngineStore
{
    Task<IEnumerable<GameEngineGameState>> GetGameStatesByChallenge(string challengeId);
    Task<IEnumerable<GameEngineGameState>> GetGameStatesByChallengeSpec(string challengeSpecId);
    Task<IEnumerable<GameEngineGameState>> GetGameStatesByPlayer(string playerId);
    Task<IEnumerable<GameEngineGameState>> GetGameStatesByTeam(string teamId);
}

internal class GameEngineStore : IGameEngineStore
{
    private readonly GameboardDbContext _db;
    private readonly IJsonService _jsonService;

    public GameEngineStore(GameboardDbContext db, IJsonService jsonService)
    {
        _db = db;
        _jsonService = jsonService;
    }

    public Task<IEnumerable<GameEngineGameState>> GetGameStatesByPlayer(string playerId)
        => GetGameStates(playerId: playerId);

    public Task<IEnumerable<GameEngineGameState>> GetGameStatesByChallenge(string challengeId)
        => GetGameStates(challengeId: challengeId);

    public Task<IEnumerable<GameEngineGameState>> GetGameStatesByChallengeSpec(string challengeSpecId)
        => GetGameStates(challengeSpecId: challengeSpecId);

    public Task<IEnumerable<GameEngineGameState>> GetGameStatesByTeam(string teamId)
        => GetGameStates(teamId: teamId);

    private async Task<IEnumerable<GameEngineGameState>> GetGameStates(string playerId = null, string challengeId = null, string challengeSpecId = null, string teamId = null)
    {
        if (playerId.IsEmpty() && challengeId.IsEmpty() && challengeSpecId.IsEmpty() && teamId.IsEmpty())
            throw new ArgumentException("Can't retrieve game engine type without at least one argument.");

        var query = _db.Challenges.AsQueryable();

        if (!string.IsNullOrWhiteSpace(playerId))
            query = query.Where(c => c.PlayerId == playerId);

        if (!string.IsNullOrWhiteSpace(challengeId))
            query = query.Where(c => c.Id == challengeId);

        if (!string.IsNullOrWhiteSpace(challengeSpecId))
            query = query.Where(c => c.SpecId == challengeSpecId);

        if (!string.IsNullOrWhiteSpace(teamId))
            query = query.Where(c => c.TeamId == teamId);

        var results = await query.Select(c => c.State)
            .Distinct()
            .ToArrayAsync();

        return results.Select(stateJson => _jsonService.Deserialize<GameEngineGameState>(stateJson));
    }
}
