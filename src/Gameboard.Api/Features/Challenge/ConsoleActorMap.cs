// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gameboard.Api.Services
{
    public class ConsoleActorMap
    {
        ConcurrentDictionary<string, ConsoleActor> _cache = new ConcurrentDictionary<string, ConsoleActor>();

        public ConsoleActorMap()
        {
            Task.Run(() => Cleanup());
        }
        private async Task Cleanup()
        {
            while (true)
            {
                var ts = DateTimeOffset.UtcNow;

                var stale = _cache.Values
                    .Where(a =>
                        string.IsNullOrEmpty(a.VmName) &&
                        ts.Subtract(a.Timestamp).TotalMinutes > 1
                    )
                ;

                foreach (var item in stale)
                    _cache.TryRemove(item.UserId, out ConsoleActor discard);

                await Task.Delay(20000);
            }
        }
        public void Update(ConsoleActor actor)
        {
            _cache.AddOrUpdate(actor.UserId, actor, (i, a) => actor);
        }

        public ConsoleActor[] Find(string gid = "")
        {
            var q = gid.HasValue()
                ? _cache.Values.Where(a => a.GameId == gid)
                : _cache.Values
            ;

            return q
                .OrderBy(a => a.GameId)
                .ThenBy(a => a.PlayerName)
                .ThenBy(a => a.UserName)
                .ToArray();
        }
    }
}
