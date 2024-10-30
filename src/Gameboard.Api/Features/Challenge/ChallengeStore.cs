// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Common.Services;

namespace Gameboard.Api.Data;

public interface IChallengeStore : IStore<Challenge>
{
    Task<Data.Challenge> Load(NewChallenge model);
    Task<Data.Challenge> Load(string id);
    Task UpdateEtd(string specId);
}

public class ChallengeStore(
    IGuidService guids,
    GameboardDbContext dbContext,
    IStore store) : Store<Challenge>(dbContext, guids), IChallengeStore
{
    private readonly IStore _store = store;

    public override IQueryable<Challenge> List(string term)
    {
        var q = base.List();

        if (term.NotEmpty())
        {
            term = term.ToLower();
            q = q.Include(c => c.Player);
            q = q.Where(c =>
                c.Id.StartsWith(term) || // Challenge Id
                c.Tag.ToLower().StartsWith(term) || // Challenge Tag
                c.PlayerId.StartsWith(term) || // PlayerId
                c.Player.TeamId.StartsWith(term) || // TeamId
                c.Player.UserId.StartsWith(term) || // User Id
                c.Name.ToLower().Contains(term) || // Challenge Title
                c.Player.ApprovedName.ToLower().Contains(term) // Team Name (or indiv. Player Name)
            );
        }

        return q
            .Include(c => c.Game)
            .Include(c => c.Player);
    }

    public async Task<Challenge> Load(string id)
    {
        return await DbSet
            .Include(c => c.Events)
            .SingleOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Challenge> Load(NewChallenge model)
    {
        var player = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.Id == model.PlayerId)
            .SingleOrDefaultAsync();

        return await DbSet
            .Include(c => c.Player)
            .FirstOrDefaultAsync(c =>
                c.SpecId == model.SpecId &&
                (
                    c.PlayerId == model.PlayerId ||
                    c.TeamId == player.TeamId
                )
            );
    }

    public async Task UpdateEtd(string specId)
    {
        var stats = await DbSet.Where(c => c.SpecId == specId)
            .Select(c => new { Created = c.WhenCreated, Started = c.StartTime })
            .Where(c => c.Created > DateTimeOffset.MinValue)
            .OrderByDescending(m => m.Created)
            .Take(20)
            .ToArrayAsync();

        int avg = (int)stats.Average(m =>
            m.Started.Subtract(m.Created).TotalSeconds
        );

        await _store
            .WithNoTracking<Data.ChallengeSpec>()
            .Where(s => s.Id == specId)
            .ExecuteUpdateAsync
            (
                s => s.SetProperty(s => s.AverageDeploySeconds, avg)
            );
    }
}
