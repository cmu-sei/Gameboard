// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data.Abstractions;
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
    private static SemaphoreSlim SP_CHALLENGE_DATA = new SemaphoreSlim(1, 1);
    private readonly IChallengeStore _challengeStore;
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
        PlayerService playerService,
        IChallengeStore challengeStore,
        IHttpClientFactory httpClientFactory,
        IUnityGameService unityGameService,
        IHubContext<AppHub, IAppHubEvent> hub,
        IMapper mapper
    ) : base(logger, cache, validator)
    {
        _actorMap = actorMap;
        _challengeStore = challengeStore;
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

        // each _team_ will only get one copy of the challenge, and by rule, that challenge must have the id
        // of the topo gamespace ID. If it's already in the DB, send them on their way with the challenge we've already got
        // 
        // semaphore locking because, if i don't, may not sleep during the competition
        Data.Challenge challengeData = null;
        try
        {
            Console.Write("Entering the Unity challenge data semaphore");
            await SP_CHALLENGE_DATA.WaitAsync();

            challengeData = await _unityGameService.HasChallengeData(model);
            if (challengeData != null)
            {
                return challengeData;
            }

            // otherwise, add new challenge data and send gamebrain the ids of the consoles (which are based on the challenge id)
            challengeData = await _unityGameService.AddChallenge(model, Actor);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error inside the Unity challenge data semaphore:", ex);
            throw;
        }
        finally
        {
            SP_CHALLENGE_DATA.Release();
        }

        // now that we have challenge IDs, we can update gamebrain's console urls
        var gamebrainClient = await CreateGamebrain();

        var vmData = model.Vms.Select(vm =>
        {
            var consoleHost = new UriBuilder(Request.Scheme, Request.Host.Host, Request.Host.Port ?? -1, $"{Request.PathBase}/mks");
            consoleHost.Query = $"f=1&s={challengeData.Id}&v={vm.Name}";

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
            throw;
        }

        // notify the hub (if there is one)
        await _hub.Clients
            .Group(model.TeamId)
            .ChallengeEvent(new HubEvent<Challenge>(_mapper.Map<Challenge>(challengeData), EventAction.Updated));

        return challengeData;
    }

    [HttpPost("api/unity/mission-update")]
    [Authorize]
    public async Task<IActionResult> CreateMissionEvent([FromBody] UnityMissionUpdate model)
    {
        AuthorizeAny(
            () => Actor.IsAdmin
        );

        await Validate(model);
        var challengeEvent = await _unityGameService.CreateMissionEvent(model, Actor);

        if (challengeEvent == null)
        {
            // this means that everything went fine, but that we've already been told the team completed this challenge
            return Accepted();
        }

        // this means we actually created an event, so also update player scores
        await _challengeStore.UpdateTeam(model.TeamId);

        // call back with the event
        return Ok(challengeEvent);
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
