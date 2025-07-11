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
        private static readonly ConcurrentDictionary<string, ConsoleActor> _cache = new();

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
                    .ToArray()
                ;

                foreach (var item in stale)
                    _cache.TryRemove(item.UserId, out _);

                await Task.Delay(20000);
            }
        }

        public void Update(ConsoleActor actor)
        {
            _cache.AddOrUpdate(actor.UserId, actor, (i, a) => actor);
        }

        public ConsoleActor[] Find(string gid = "")
        {
            var q = gid.NotEmpty()
                ? _cache.Values.Where(a => a.GameId == gid)
                : _cache.Values
            ;

            return [.. q];
        }

        public ConsoleActor FindActor(string uid)
        {
            return _cache.GetValueOrDefault(uid, null);
        }

        public void Prune()
        {
            var ts = DateTimeOffset.UtcNow.AddHours(-4);

            var stale = _cache.Values
                .Where(o => o.Timestamp < ts)
                .ToArray()
            ;

            foreach (var actor in stale)
                _cache.TryRemove(actor.UserId, out _);
        }

        public void RemoveTeam(string id)
        {
            var team = _cache.Values
                .Where(a => a.TeamId == id)
                .ToArray()
            ;

            foreach (var item in team)
                _cache.TryRemove(item.UserId, out _);
        }
    }
}
