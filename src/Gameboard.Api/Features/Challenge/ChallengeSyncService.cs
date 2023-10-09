using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TopoMojo.Api.Client;

namespace Gameboard.Api.Features.Challenges;

public interface IChallengeSyncService
{
    Task Sync(Data.Challenge challenge, GameEngineGameState challengeState, CancellationToken cancellationToken);
    Task SyncExpired(CancellationToken cancellationToken);
}

internal class ChallengeSyncService : IChallengeSyncService
{
    private readonly ConsoleActorMap _consoleActorMap;
    private readonly IGameEngineService _gameEngine;
    private readonly ILogger<IChallengeSyncService> _logger;
    private readonly IMapper _mapper;
    private readonly INowService _now;
    private readonly IStore _store;

    public ChallengeSyncService
    (
        ConsoleActorMap consoleActorMap,
        IGameEngineService gameEngine,
        ILogger<IChallengeSyncService> logger,
        IMapper mapper,
        INowService now,
        IStore store
    )
    {
        _consoleActorMap = consoleActorMap;
        _logger = logger;
        _gameEngine = gameEngine;
        _mapper = mapper;
        _now = now;
        _store = store;
    }

    public Task Sync(Data.Challenge challenge, GameEngineGameState state, CancellationToken cancellationToken)
        => Sync(cancellationToken, new SyncEntry(challenge, state));

    private async Task Sync(CancellationToken cancellationToken, params SyncEntry[] entries)
    {
        if (entries is null)
            throw new ArgumentNullException(nameof(entries));

        foreach (var entry in entries)
        {
            // TODO
            // this is currently awkward because the game state that comes back here has the team ID as the subjectId (because that's what we're passing to Topo - see 
            // GameEngine.RegisterGamespace). it's unclear whether topo cares what we pass as the players argument there, but since we're passing team ID 
            // there we need to NOT overwrite the playerId on the entity during the call to Map. Obviously, we could fix this by setting a rule on the map, 
            // but I'm leaving it here because this is the anomalous case.
            var playerId = entry.Challenge.PlayerId;
            _mapper.Map(entry.State, entry.Challenge);
            entry.Challenge.PlayerId = playerId;
            entry.Challenge.LastSyncTime = _now.Get();
            await _store.Update(entry.Challenge, cancellationToken);
        }
    }

    public async Task SyncExpired(CancellationToken cancellationToken)
    {
        var now = _now.Get();

        // a limitation of the current game engine architecture is that gamespaces can only be loaded one by one
        // (there's no multi-id signature), and we can't parallelize this because DbContext can't be used concurrently.
        // 
        // Just load them all, then sync one by one.
        var challenges = await _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => c.LastSyncTime < c.Player.SessionEnd && c.Player.SessionEnd < now)
            .ToArrayAsync(cancellationToken);

        _logger.LogInformation($"Syncing data for {challenges.Length} expired challenges...");
        foreach (var challenge in challenges)
        {
            try
            {
                var state = await _gameEngine.LoadGamespace(challenge);
                _consoleActorMap.RemoveTeam(challenge.TeamId);
                await Sync(challenge, state, cancellationToken);
                _logger.LogInformation($"The challenge sync service sync'd data for challenge {challenge.Id}.");
            }
            catch (ApiException apiEx)
            {
                _logger.LogError(apiEx, $"""Game engine API responded with an error for challenge {challenge.Id}.""");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"""Couldn't sync data for challenge {challenge.Id}.""");
            }
        }

        // prune the map just to ensure we don't have any stragglers
        _consoleActorMap.Prune();
    }

    internal class SyncEntry
    {
        public Data.Challenge Challenge { get; private set; }
        public GameEngineGameState State { get; private set; }

        public SyncEntry(Data.Challenge challenge, GameEngineGameState state) =>
            (Challenge, State) = (challenge, state);
    }
}
