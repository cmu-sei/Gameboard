// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Features.UnityGames;
using Gameboard.Api.Hubs;
using Gameboard.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
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
    private readonly LinkGenerator _linkGenerator;
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
        LinkGenerator link,
        IMapper mapper
    ) : base(logger, cache, validator)
    {
        _actorMap = actorMap;
        _gameService = gameService;
        _httpClientFactory = httpClientFactory;
        _hub = hub;
        _linkGenerator = link;
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
            () => Actor.IsSupport,
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
    [HttpPost("api/unity/challenge")]
    public async Task<Data.Challenge> CreateChallenge([FromBody] NewUnityChallenge model)
    {
        AuthorizeAny(
            () => _gameService.UserIsTeamPlayer(Actor.Id, model.GameId, model.TeamId).Result
        );

        await Validate(model);
        var result = await _unityGameService.AddChallenge(model, Actor);

        // now that we have challenge IDs, we can update gamebrain's console urls
        var gamebrainClient = await CreateGamebrain();

        var vmData = model.Vms.Select(vm =>
        {
            var consoleHost = new UriBuilder(Request.Scheme, Request.Host.Host, Request.Host.Port ?? -1, $"{Request.PathBase}/mks");
            consoleHost.Query = $"f=1&s={result.Id}&v={vm.Name}";

            return new UnityGameVm
            {
                Id = vm.Id,
                Url = consoleHost.Uri.ToString(),
                Name = vm.Name,
            };
        }).ToArray();

        try
        {
            await gamebrainClient.PostAsync($"admin/update_console_urls/{model.TeamId}", JsonContent.Create<UnityGameVm[]>(vmData, mediaType: MediaTypeHeaderValue.Parse("application/json")));
        }
        catch (Exception ex)
        {
            Console.Write("Calling gamebrain failed with", ex);
        }

        // notify the hub (if there is one)
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
    [HttpPost("api/unity/challengeEvent")]
    [Authorize]
    public async Task<Data.ChallengeEvent> CreateChallengeEvent([FromBody] NewUnityChallengeEvent model)
    {
        AuthorizeAny(
            () => Actor.IsDirector,
            () => Actor.IsAdmin,
            () => Actor.IsDesigner
        );

        await Validate(model);
        return await _unityGameService.AddChallengeEvent(model, Actor.Id);
    }

    [HttpPost("api/unity/mission-update")]
    [Authorize]
    public async Task CreateMissionEvent([FromBody] UnityMissionUpdate model)
    {
        AuthorizeAny(
            () => Actor.IsAdmin
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
