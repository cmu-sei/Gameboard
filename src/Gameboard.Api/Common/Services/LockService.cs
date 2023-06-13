// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Concurrent;
using Nito.AsyncEx;

namespace Gameboard.Api.Common.Services
{
    public interface ILockService
    {
        AsyncLock GetChallengeLock(string challengeId);
        AsyncLock GetSyncStartGameLock(string gameId);
    }

    public class LockService : ILockService
    {
        private readonly ConcurrentDictionary<string, AsyncLock> _challengeLocks = new ConcurrentDictionary<string, AsyncLock>();
        private readonly ConcurrentDictionary<string, AsyncLock> _syncStartGameLocks = new ConcurrentDictionary<string, AsyncLock>();

        public AsyncLock GetChallengeLock(string challengeId)
        {
            return _challengeLocks.GetOrAdd(challengeId, x => { return new AsyncLock(); });
        }

        public AsyncLock GetSyncStartGameLock(string gameId)
        {
            return _syncStartGameLocks.GetOrAdd(gameId, x => new AsyncLock());
        }
    }
}
