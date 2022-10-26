// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading.Tasks;
using Gameboard.Api.Features.ChallengeEvents;
using Gameboard.Api.Services;
using Gameboard.Api.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Controllers;

[Authorize]
public class ChallengeEventController : _Controller
{
    private ConsoleActorMap _actorMap;
    private ChallengeEventService _challengeEventService;

    public ChallengeEventController(
        // required by _Controller
        IDistributedCache cache,
        ILogger<ChallengeEventController> logger,
        ChallengeEventValidator validator,
        // other stuff
        ChallengeEventService challengeEventService,
        PlayerService playerService,
        ConsoleActorMap actorMap
    ) : base(logger, cache, validator)
    {
        this._actorMap = actorMap;
        this._challengeEventService = challengeEventService;
    }

    /// <summary>
    ///     Log an event for a given challenge.
    /// </summary>
    /// <param name="model">NewChallengeEvent</param>
    /// <returns>ChallengeEvent</returns>
    [HttpPost("api/challengeEvent")]
    [Authorize]
    public async Task<Data.ChallengeEvent> Create([FromBody] NewChallengeEvent model)
    {
        AuthorizeAny(() => Actor.IsDirector);
        await Validate(model);

        return await _challengeEventService.Add(model);
    }
}
