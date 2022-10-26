// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Features.ChallengeEvents;
using Gameboard.Api.Features.UnityGames;
using Gameboard.Api.Hubs;
using Gameboard.Api.Services;
using Gameboard.Api.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Controllers;

[Authorize]
public class UnityGameController : _Controller
{
    private readonly ConsoleActorMap _actorMap;
    private readonly ChallengeEventService _challengeEventService;
    private readonly IHubContext<AppHub, IAppHubEvent> _hub;
    private readonly IMapper _mapper;
    private readonly UnityGameService _unityGameService;

    public UnityGameController(
        // required by _Controller
        IDistributedCache cache,
        ILogger<ChallengeEventController> logger,
        UnityGamesValidator validator,
        // other stuff
        ConsoleActorMap actorMap,
        UnityGameService unityGameService,
        // ChallengeService challengeService,
        ChallengeEventService challengeEventService,
        IHubContext<AppHub, IAppHubEvent> hub,
        IMapper mapper
    ) : base(logger, cache, validator)
    {
        _actorMap = actorMap;
        _challengeEventService = challengeEventService;
        _hub = hub;
        _mapper = mapper;
        _unityGameService = unityGameService;
    }

    /// <summary>
    ///     Create challenge data for an existing Unity game's gamespace.
    /// </summary>
    /// <param name="model">NewChallengeEvent</param>
    /// <returns>ChallengeEvent</returns>
    [HttpPost("api/unity/challenges")]
    [Authorize]
    public async Task<IList<Data.Challenge>> Create([FromBody] NewUnityChallenge model)
    {
        AuthorizeAny(() => Actor.IsDirector);
        await Validate(model);

        string graderUrl = string.Format(
            "{0}://{1}{2}",
            Request.Scheme,
            Request.Host,
            Url.Action("Grade")
        );

        var result = await _unityGameService.Add(model, Actor);

        foreach (var challenge in result.Select(c => _mapper.Map<Challenge>(c)))
        {
            await _hub.Clients
            .Group(model.TeamId)
            .ChallengeEvent(new HubEvent<Challenge>(challenge, EventAction.Updated));
        }

        return result;
    }
}
