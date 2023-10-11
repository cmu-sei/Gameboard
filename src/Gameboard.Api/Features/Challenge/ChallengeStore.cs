// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Features.Certificates;

namespace Gameboard.Api.Data;

public interface IChallengeStore : IStore<Challenge>
{
    Task<Data.Challenge> Load(NewChallenge model);
    Task<Data.Challenge> Load(string id);
    Task UpdateTeam(string teamId);
    Task UpdateEtd(string specId);
    Task<int> ChallengeGamespaceCount(string teamId);
}

public class ChallengeStore : Store<Challenge>, IChallengeStore
{
    public ChallengeStore(
        IGuidService guids,
        GameboardDbContext dbContext) : base(dbContext, guids)
    { }

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
            .FirstOrDefaultAsync(c => c.Id == id)
        ;
    }

    public async Task<Challenge> Load(NewChallenge model)
    {
        var player = await DbContext.Players.FindAsync(model.PlayerId);

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

        await DbContext
            .ChallengeSpecs
            .Where(s => s.Id == specId)
            .ExecuteUpdateAsync
            (
                s => s.SetProperty(s => s.AverageDeploySeconds, avg)
            );
    }

    public async Task UpdateTeam(string id)
    {
        var challenges = await DbSet.Where(c => c.TeamId == id).ToArrayAsync();

        int score = (int)challenges.Sum(c => c.Score);
        long time = challenges.Sum(c => c.Duration);
        int complete = challenges.Count(c => c.Result == ChallengeResult.Success);
        int partial = challenges.Count(c => c.Result == ChallengeResult.Partial);

        var players = await DbContext.Players.Where(p => p.TeamId == id).ToArrayAsync();

        foreach (var p in players)
        {
            p.Score = score;
            p.Time = time;
            p.CorrectCount = complete;
            p.PartialCount = partial;
        }

        await DbContext.SaveChangesAsync();

        // TODO: consider queuing this for a background process
        await UpdateRanks(players.First().GameId);
    }

    public async Task UpdateRanks(string gameId)
    {
        var players = await DbContext.Players
            .Where(p => p.GameId == gameId)
            .OrderByDescending(p => p.Score)
            .ThenBy(p => p.Time)
            .ThenByDescending(p => p.CorrectCount)
            .ThenByDescending(p => p.PartialCount)
            .ToArrayAsync()
        ;
        int rank = 0;

        foreach (var team in players.GroupBy(p => p.TeamId))
        {
            rank += 1;
            foreach (var player in team)
                player.Rank = rank;
        }

        await DbContext.SaveChangesAsync();
    }

    public Task<int> ChallengeGamespaceCount(string teamId)
    {
        return DbSet.CountAsync(c =>
            c.TeamId == teamId &&
            c.HasDeployedGamespace.Equals(true)
        );
    }
}
