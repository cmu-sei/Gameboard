// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Concurrent;
using Nito.AsyncEx;

namespace Gameboard.Api.Services
{
    public interface ILockService
    {
        AsyncLock GetChallengeLock(string challengeId);
    }

    public class LockService : ILockService
    {
        ConcurrentDictionary<string, AsyncLock> ChallengeLocks = new ConcurrentDictionary<string, AsyncLock>();

        public LockService()
        {
        }

        public AsyncLock GetChallengeLock(string challengeId)
        {
            return ChallengeLocks.GetOrAdd(challengeId, x => { return new AsyncLock(); });
        }
    }
}
