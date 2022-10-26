using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
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

    public async Task<IList<Data.Challenge>> Add(NewUnityChallenge newChallenge, User actor)
    {
        // find the team's players and make sure their data is what we expect
        var teamPlayers = await Store.DbContext
            .Players
            .Where(p => p.TeamId == newChallenge.TeamId)
            .ToListAsync();

        if (teamPlayers.Count == 0)
        {
            throw new TeamHasNoPlayersException();
        }

        if (teamPlayers.Any(p => p.GameId != newChallenge.GameId))
        {
            throw new PlayerWrongGameIDException();
        }

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
            Type = ChallengeEventType.Started
        };

        var playerChallenges = from p in teamPlayers
                               select new Data.Challenge
                               {
                                   Name = $"{teamPlayers.First().ApprovedName} vs. Cubespace",
                                   GameId = challengeSpec.GameId,
                                   TeamId = newChallenge.TeamId,
                                   PlayerId = p.Id,
                                   HasDeployedGamespace = true,
                                   SpecId = challengeSpec.Id,
                                   GraderKey = Guid.NewGuid().ToString("n").ToSha256(),
                                   Points = newChallenge.Points,
                                   Score = 0,
                                   Events = new List<Data.ChallengeEvent>
                                   { initialEvent }
                               };

        Store.DbContext.Challenges.AddRange(playerChallenges);
        await Store.DbContext.SaveChangesAsync();

        return playerChallenges.ToList();
    }
}