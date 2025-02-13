using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TopoMojo.Api.Client;

namespace Gameboard.Api.Features.Challenges;

public interface IChallengeSyncService
{
    Task Sync(Data.Challenge challenge, GameEngineGameState challengeState, string actingUserId, CancellationToken cancellationToken);
    Task SyncExpired(CancellationToken cancellationToken);
}

/// <summary>
/// Used by the Job service to update challenges which have expired
/// </summary>
internal class ChallengeSyncService
(
    ConsoleActorMap consoleActorMap,
    IGameEngineService gameEngine,
    ILogger<IChallengeSyncService> logger,
    IMapper mapper,
    INowService now,
    IStore store
) : IChallengeSyncService
{
    private readonly ConsoleActorMap _consoleActorMap = consoleActorMap;
    private readonly IGameEngineService _gameEngine = gameEngine;
    private readonly ILogger<IChallengeSyncService> _logger = logger;
    private readonly IMapper _mapper = mapper;
    private readonly INowService _now = now;
    private readonly IStore _store = store;

    public Task Sync(Data.Challenge challenge, GameEngineGameState state, string actingUserId, CancellationToken cancellationToken)
        => Sync(cancellationToken, new SyncEntry(actingUserId, challenge, state));

    private async Task Sync(CancellationToken cancellationToken, params SyncEntry[] entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        foreach (var entry in entries)
        {
            // TODO
            // this is currently awkward because the game state that comes back here has the team ID as the subjectId (because that's what we're passing to Topo - see 
            // GameEngine.RegisterGamespace). it's unclear whether topo cares what we pass as the players argument there, but since we're passing team ID 
            // there we need to NOT overwrite the playerId on the entity during the call to Map. Obviously, we could fix this by setting a rule on the map, 
            // but I'm leaving it here because this is the anomalous case.
            var playerId = entry.Challenge.PlayerId;

            // before we map the new state to the challenge, check for changes in HasDeployedGamespace.
            // if they're unequal, log a gamespace event.
            if (entry.Challenge.HasDeployedGamespace != entry.State.HasDeployedGamespace)
            {
                await _store.Create(new ChallengeEvent
                {
                    ChallengeId = entry.Challenge.Id,
                    TeamId = entry.Challenge.TeamId,
                    Text = "Inferred from game engine sync",
                    Timestamp = _now.Get(),
                    Type = entry.Challenge.HasDeployedGamespace ? ChallengeEventType.GamespaceOff : ChallengeEventType.GamespaceOn,
                    UserId = entry.ActingUserId, // can be null, which is what we want for now
                });
            }

            _mapper.Map(entry.State, entry.Challenge);
            entry.Challenge.PlayerId = playerId;
            entry.Challenge.LastSyncTime = _now.Get();
            await _store.SaveUpdate(entry.Challenge, cancellationToken);

            _logger.LogChallengeSync(entry.Challenge.Id, entry.Challenge.TeamId, entry.Challenge.HasDeployedGamespace);
        }
    }

    public async Task SyncExpired(CancellationToken cancellationToken)
    {
        var now = _now.Get();

        // a limitation of the current game engine architecture is that gamespaces can only be loaded one by one
        // (there's no multi-id signature), and we can't parallelize this because DbContext can't be used concurrently.
        // 
        // Just load them all, then sync one by one.
        var challenges = await GetExpiredChallengesForSync(now, cancellationToken);

        var playerIds = challenges.Select(c => c.PlayerId).Distinct().ToArray();
        var playerSessionEnds = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => playerIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.SessionEnd, cancellationToken);

        _logger.LogInformation("The ChallengeSyncService is synchronizing {syncCount} challenges...", challenges.Count());
        foreach (var challenge in challenges)
        {
            try
            {
                var state = await _gameEngine.LoadGamespace(challenge);
                _consoleActorMap.RemoveTeam(challenge.TeamId);
                await Sync(challenge, state, null, cancellationToken);
                _logger.LogInformation($"The challenge sync service sync'd data for challenge {challenge.Id}.");
            }
            catch (ApiException apiEx)
            {
                _logger.LogError(apiEx, $"""Game engine API responded with an error for challenge {challenge.Id}. Removing it from the sync list.""");

                // the game engine doesn't know about this challenge, so by rule, we call it sync'd and let it go
                await _store
                    .WithNoTracking<Data.Challenge>()
                    .Where(c => c.Id == challenge.Id)
                    .ExecuteUpdateAsync
                    (
                        up => up
                            .SetProperty(c => c.LastSyncTime, now)
                            .SetProperty(c => c.EndTime, c => playerSessionEnds.ContainsKey(challenge.PlayerId) ? playerSessionEnds[challenge.PlayerId] : c.EndTime),
                        cancellationToken
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"""Couldn't sync data for challenge {challenge.Id}.""");
            }
        }

        // prune the map just to ensure we don't have any stragglers
        _consoleActorMap.Prune();

        // let them know we're done
        _logger.LogInformation($"The ChallengeSyncService finished synchronizing {challenges.Count()} challenges.");
    }

    internal record SyncEntry(string ActingUserId, Data.Challenge Challenge, GameEngineGameState State);

    internal async Task<IEnumerable<Data.Challenge>> GetExpiredChallengesForSync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        return await _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => c.Player.SessionEnd != DateTimeOffset.MinValue && c.Player.SessionEnd < now && (c.LastSyncTime < c.Player.SessionEnd || c.EndTime == DateTimeOffset.MinValue))
            .ToArrayAsync(cancellationToken);
    }
}
