using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TopoMojo.Api.Client;

namespace Gameboard.Api.Features.UnityGames;

public class UnityGameService : _Service
{
    private readonly IChallengeStore _challengeStore;
    IUnityStore Store { get; }
    ITopoMojoApiClient Mojo { get; }

    private readonly ConsoleActorMap _actorMap;

    public UnityGameService(
            ILogger<UnityGameService> logger,
            IMapper mapper,
            CoreOptions options,
            IChallengeStore challengeStore,
            IUnityStore store,
            ITopoMojoApiClient mojo,
            ConsoleActorMap actorMap
        ) : base(logger, mapper, options)
    {
        Store = store;
        Mojo = mojo;
        _actorMap = actorMap;
        _challengeStore = challengeStore;
    }

    public async Task<IList<Data.Challenge>> AddChallenge(NewUnityChallenge newChallenge, User actor)
    {
        // find the team's players
        var teamPlayers = await Store.DbContext
            .Players
            .Where(p => p.TeamId == newChallenge.TeamId)
            .ToListAsync();

        // load the spec associated with the game
        var challengeSpec = await Store.DbContext.ChallengeSpecs.FirstOrDefaultAsync(c => c.GameId == newChallenge.GameId);

        if (challengeSpec == null)
        {
            throw new SpecNotFound();
        }

        // start building the challenge to insert

        // honestly not sure why this syntax doesn't work?
        // var playerChallenges = teamPlayers.Select<Data.Challenge>(p =>
        // {
        //     return new Data.Challenge
        //     {
        //         Name = $"{teamPlayers.First().ApprovedName} vs. Cubespace",
        //         GameId = challengeSpec.GameId,
        //         TeamId = newChallenge.TeamId,
        //         PlayerId = p.Id,
        //         HasDeployedGamespace = true,
        //         SpecId = challengeSpec.Id,
        //         GraderKey = Guid.NewGuid().ToString("n").ToSha256(),
        //         Points = newChallenge.Points,
        //         Score = 0
        //     };
        // });

        var initialEvent = new Data.ChallengeEvent
        {
            Id = Guid.NewGuid().ToString("n"),
            UserId = actor.Id,
            TeamId = newChallenge.TeamId,
            Timestamp = DateTimeOffset.UtcNow,
            Text = $"{actor.ApprovedName}'s journey into CubeSpace has begun...",
            Type = ChallengeEventType.Started
        };

        // we have to spoof topomojo data here. load the game and related data.
        var game = await this.Store
            .DbContext
            .Games
            .FirstOrDefaultAsync(g => g.Id == newChallenge.GameId);

        var teamCaptain = teamPlayers.FirstOrDefault(t => t.IsManager);

        // this is some guesswork on my part and omits some fields.
        // we'll see how it goes - BS
        var state = new TopoMojo.Api.Client.GameState
        {
            Id = newChallenge.GameId,
            Name = game.Id,
            ManagerId = string.IsNullOrWhiteSpace(teamCaptain?.Id) ? null : teamCaptain.Id,
            ManagerName = string.IsNullOrEmpty(teamCaptain?.Name) ? null : teamCaptain.Name,
            Markdown = game.GameMarkdown,
            Players = teamPlayers.Select(p => new TopoMojo.Api.Client.Player
            {
                SubjectId = p.Id,
                SubjectName = p.ApprovedName,
                Permission = p.IsManager ? TopoMojo.Api.Client.Permission.Manager : TopoMojo.Api.Client.Permission.None
            }).ToArray(),
            StartTime = game.GameStart,
            EndTime = game.GameEnd,
            IsActive = game.IsLive,
            WhenCreated = DateTimeOffset.UtcNow,
        };

        var playerChallenges = from p in teamPlayers
                               select new Data.Challenge
                               {
                                   Id = Guid.NewGuid().ToString("n"),
                                   Name = $"{teamPlayers.First().ApprovedName} vs. Cubespace",
                                   GameId = challengeSpec.GameId,
                                   TeamId = newChallenge.TeamId,
                                   PlayerId = p.Id,
                                   HasDeployedGamespace = true,
                                   SpecId = challengeSpec.Id,
                                   State = JsonSerializer.Serialize(state),
                                   GraderKey = Guid.NewGuid().ToString("n").ToSha256(),
                                   Points = newChallenge.Points,
                                   Score = 0,
                                   Events = new List<Data.ChallengeEvent> { initialEvent },
                                   WhenCreated = DateTimeOffset.UtcNow,
                               };


        Store.DbContext.Challenges.AddRange(playerChallenges);
        await Store.DbContext.SaveChangesAsync();

        return playerChallenges.ToList();
    }

    public async Task<IEnumerable<ChallengeEvent>> AddChallengeEvents(NewUnityChallengeEvent model, string userId)
    {
        var teamPlayers = await Store.DbContext
            .Players
            .Where(p => p.TeamId == model.TeamId)
            .ToListAsync();

        var events = teamPlayers.Select(p => new Data.ChallengeEvent
        {
            ChallengeId = model.ChallengeId,
            UserId = userId,
            TeamId = model.TeamId,
            Text = model.Text,
            Type = model.Type,
            Timestamp = model.Timestamp
        });

        return await Store.AddUnityChallengeEvents(events);
    }
}