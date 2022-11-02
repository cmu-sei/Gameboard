// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Features.UnityGames;
using Gameboard.Api.Hubs;
using Gameboard.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Controllers;

[Authorize]
public class UnityGameController : _Controller
{
    private readonly ConsoleActorMap _actorMap;
    private readonly GameService _gameService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHubContext<AppHub, IAppHubEvent> _hub;
    private readonly IMapper _mapper;
    private readonly IUnityGameService _unityGameService;

    public UnityGameController(
        // required by _Controller
        IDistributedCache cache,
        ILogger<UnityGameController> logger,
        UnityGamesValidator validator,
        // other stuff
        ConsoleActorMap actorMap,
        GameService gameService,
        IHttpClientFactory httpClientFactory,
        IUnityGameService unityGameService,
        IHubContext<AppHub, IAppHubEvent> hub,
        IMapper mapper
    ) : base(logger, cache, validator)
    {
        _actorMap = actorMap;
        _gameService = gameService;
        _httpClientFactory = httpClientFactory;
        _hub = hub;
        _mapper = mapper;
        _unityGameService = unityGameService;
    }

    [HttpGet("/api/unity/{gid}/{tid}")]
    [Authorize]
    public async Task<IActionResult> GetGamespace([FromRoute] string gid, [FromRoute] string tid)
    {
        AuthorizeAny(
            () => _gameService.UserIsTeamPlayer(Actor.Id, gid, tid).Result
        );

        var gb = await CreateGamebrain();
        var m = await gb.GetAsync($"admin/deploy/{gid}/{tid}");

        if (m.IsSuccessStatusCode)
        {
            var stringContent = await m.Content.ReadAsStringAsync();

            if (!stringContent.IsEmpty())
            {
                return new JsonResult(stringContent);
            }

            return Ok();
        }

        return BuildError(m, $"Bad response from Gamebrain: {m.Content} : {m.ReasonPhrase}");
    }

    [HttpPost("/api/unity/deploy/{gid}/{tid}")]
    [Authorize]
    public async Task<string> DeployUnitySpace([FromRoute] string gid, [FromRoute] string tid)
    {
        AuthorizeAny(
            () => Actor.IsDirector,
            () => _gameService.UserIsTeamPlayer(Actor.Id, gid, tid).Result
        );

        var gb = await CreateGamebrain();
        var m = await gb.PostAsync($"admin/deploy/{gid}/{tid}", null);
        return await m.Content.ReadAsStringAsync();
    }

    [HttpPost("/api/unity/undeploy/{gid}/{tid}")]
    [Authorize]
    public async Task<string> UndeployUnitySpace([FromQuery] string gid, [FromRoute] string tid)
    {
        AuthorizeAny(
            () => Actor.IsAdmin,
            () => _gameService.UserIsTeamPlayer(Actor.Id, gid, tid).Result
        );

        var accessToken = await HttpContext.GetTokenAsync("access_token");
        var gb = await CreateGamebrain();

        var m = await gb.GetAsync($"admin/undeploy/{tid}");
        return await m.Content.ReadAsStringAsync();
    }

    /// <summary>
    ///     Create challenge data for an existing Unity game's gamespace.
    /// </summary>
    /// <param name="model">NewChallengeEvent</param>
    /// <returns>ChallengeEvent</returns>
    [Authorize]
    [HttpPost("api/unity/challenges")]
    public async Task<Data.Challenge> CreateChallenge([FromBody] NewUnityChallenge model)
    {
        AuthorizeAny(
            () => Actor.IsDirector,
            () => Actor.IsAdmin,
            () => Actor.IsDesigner
        );

        await Validate(model);
        var result = await _unityGameService.AddChallenge(model, Actor);

        await _hub.Clients
                .Group(model.TeamId)
                .ChallengeEvent(new HubEvent<Challenge>(_mapper.Map<Challenge>(result), EventAction.Updated));

        return result;
    }

    /// <summary>
    ///     Log a challenge event for all members of the specified team.
    /// </summary>
    /// <param name="model">NewChallengeEvent</param>
    /// <returns>ChallengeEvent</returns>
    [HttpPost("api/unity/challengeEvents")]
    [Authorize]
    public async Task<IEnumerable<Data.ChallengeEvent>> CreateChallengeEvent([FromBody] NewUnityChallengeEvent model)
    {
        AuthorizeAny(
            () => Actor.IsDirector,
            () => Actor.IsAdmin,
            () => Actor.IsDesigner
        );

        await Validate(model);
        return await _unityGameService.AddChallengeEvents(model, Actor.Id);
    }

    [Authorize]
    [HttpPost("api/unity/mission-update")]
    public async Task CreateMissionEvent([FromBody] UnityMissionUpdate model)
    {
        AuthorizeAny(
            () => Actor.IsDirector,
            () => Actor.IsAdmin,
            () => Actor.IsDesigner
        );

        await Validate(model);

        await _unityGameService.CreateMissionEvent(model, Actor);
    }

    [Authorize]
    [HttpPost("api/unity/admin/deleteChallengeData")]
    public async Task<IActionResult> DeleteChallengeData(string gameId)
    {
        AuthorizeAny(
            () => Actor.IsDirector,
            () => Actor.IsAdmin,
            () => Actor.IsDesigner
        );

        await _unityGameService.DeleteChallengeData(gameId);
        return Ok();
    }

    private ActionResult<T> BuildError<T>(HttpResponse response, string message = null)
    {
        var result = new ObjectResult(message);
        result.StatusCode = response.StatusCode;
        return result;
    }

    private ActionResult BuildError(HttpResponseMessage response, string message)
    {
        var result = new ObjectResult(message);
        result.StatusCode = (int)response.StatusCode;
        return result;
    }

    private async Task<HttpClient> CreateGamebrain()
    {
        var gb = _httpClientFactory.CreateClient("Gamebrain");
        gb.DefaultRequestHeaders.Add("Authorization", $"Bearer {await HttpContext.GetTokenAsync("access_token")}");
        return gb;
    }
}
