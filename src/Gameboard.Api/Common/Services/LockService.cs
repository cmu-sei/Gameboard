// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Concurrent;
using Nito.AsyncEx;

namespace Gameboard.Api.Common.Services;

public interface ILockService
{
    AsyncLock GetArbitraryLock(string key);
    AsyncLock GetChallengeLock(string challengeId);
    AsyncLock GetExternalGameDeployLock(string gameId);
    AsyncLock GetStartSessionLock(string gameId);
    AsyncLock GetSyncStartGameLock(string gameId);
}

public class LockService : ILockService
{
    private readonly ConcurrentDictionary<string, AsyncLock> _arbitraryLocks = new();
    private readonly ConcurrentDictionary<string, AsyncLock> _challengeLocks = new();
    private readonly ConcurrentDictionary<string, AsyncLock> _externalGameDeployLocks = new();
    private readonly ConcurrentDictionary<string, AsyncLock> _sessionStartLocks = new();
    private readonly ConcurrentDictionary<string, AsyncLock> _syncStartGameLocks = new();

    public AsyncLock GetArbitraryLock(string key)
        => _arbitraryLocks.GetOrAdd(key, x => new AsyncLock { });

    public AsyncLock GetChallengeLock(string challengeId)
        => _challengeLocks.GetOrAdd(challengeId, x => { return new AsyncLock(); });

    public AsyncLock GetExternalGameDeployLock(string gameId)
        => _externalGameDeployLocks.GetOrAdd(gameId, x => new AsyncLock());

    public AsyncLock GetStartSessionLock(string gameId)
        => _sessionStartLocks.GetOrAdd(gameId, x => new AsyncLock());

    public AsyncLock GetSyncStartGameLock(string gameId)
        => _syncStartGameLocks.GetOrAdd(gameId, x => new AsyncLock());
}
