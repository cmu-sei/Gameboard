using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Games.External;

public interface IExternalSyncGameStartService
{
    Task Start(ExternalSyncGameStartRequest request);
}

internal class ExternalSyncGameStartService : IExternalSyncGameStartService
{
    private readonly ChallengeService _challengeService;
    private readonly IChallengeSpecStore _challengeSpecStore;
    private readonly IGamebrainService _gamebrainService;
    private readonly IGameEngineService _gameEngineService;
    private readonly IGameStore _gameStore;
    private readonly IJsonService _jsonService;
    private readonly ILockService _lockService;
    private ILogger<ExternalSyncGameStartService> _logger;
    private readonly IMapper _mapper;
    private readonly IPlayerStore _playerStore;
    private readonly INowService _now;
    private readonly ITeamService _teamService;

    public ExternalSyncGameStartService
    (
        ChallengeService challengeService,
        IChallengeSpecStore challengeSpecStore,
        IGamebrainService gamebrainService,
        IGameEngineService gameEngineService,
        IGameStore gameStore,
        IJsonService jsonService,
        ILockService lockService,
        ILogger<ExternalSyncGameStartService> logger,
        IMapper mapper,
        INowService now,
        IPlayerStore playerStore,
        ITeamService teamService
    )
    {
        _challengeService = challengeService;
        _challengeSpecStore = challengeSpecStore;
        _gamebrainService = gamebrainService;
        _gameEngineService = gameEngineService;
        _gameStore = gameStore;
        _jsonService = jsonService;
        _lockService = lockService;
        _logger = logger;
        _mapper = mapper;
        _now = now;
        _playerStore = playerStore;
        _teamService = teamService;
    }

    public async Task Start(ExternalSyncGameStartRequest request)
    {
        Log("Gathering data...", request.GameId);
        var game = await _gameStore.Retrieve(request.GameId);
        var specs = await _challengeSpecStore.ListAsNoTracking().Where(cs => cs.GameId == request.GameId).ToArrayAsync();
        var players = _mapper.Map<IEnumerable<Api.Player>>(await _playerStore.ListAsNoTracking().Where(p => p.GameId == request.GameId).ToArrayAsync());
        var teams = players.GroupBy(p => p.TeamId).ToDictionary(g => g.Key, g => _teamService.ResolveCaptain(g.Key, g.ToList()));
        var teamDeployedChallenges = new Dictionary<string, List<Api.Challenge>>();
        var challengeGamespaces = new Dictionary<string, ExternalGameStartTeamGamespace>();
        Log($"Data gathered: {players.Count()} players on {teams.Keys.Count()}.", request.GameId);

        Log("Identifying team captains...", request.GameId);
        // validate that we have a team captain for every team before we do anything
        foreach (var teamId in teams.Keys)
        {
            if (teams[teamId] == null)
                throw new CaptainResolutionFailure(teamId, "Couldn't resolve captain during external sync game start.");
        }

        Log("Deploying challenges...", request.GameId);
        // deploy all challenges
        foreach (var teamId in teams.Keys)
        {
            // hold onto each team's challenges
            Log($"""Deploying challenges for team "{teamId}".""", request.GameId);
            teamDeployedChallenges.Add(teamId, new List<Challenge>());

            foreach (var specId in specs.Select(s => s.Id))
            {
                Log($"""Creating challenge for spec "{specId}"...""", request.GameId);

                var challenge = await _challengeService.Create
                (
                    new NewChallenge
                    {
                        PlayerId = teams[teamId].Id,
                        SpecId = specId,
                        StartGamespace = false, // hold gamespace startup until we're sure we've created all challenges
                        Variant = 0
                    },
                    teams.Values.First().Id, // for now, actor is first captain
                    _challengeService.BuildGraderUrl()
                );

                request.Context.DeployedChallenges.Add(challenge);
                teamDeployedChallenges[teamId].Add(challenge);
                Log($"Spec created.", request.GameId);
            }
        }

        // start all gamespaces
        Log("Deploying gamespaces...", request.GameId);
        foreach (var deployedChallenge in request.Context.DeployedChallenges)
        {
            _logger.LogInformation($"""Starting gamespace for challenge "{deployedChallenge.Id}" (teamId "{deployedChallenge.TeamId}")... """);
            var challengeState = await _gameEngineService.StartGamespace(deployedChallenge);
            _logger.LogInformation($"""Gamespace started for challenge "{deployedChallenge.Id}".""");

            var vms = _gameEngineService.GetGamespaceVms(challengeState);
            challengeGamespaces.Add(deployedChallenge.Id, new ExternalGameStartTeamGamespace
            {
                Id = challengeState.Id,
                Challenge = new SimpleEntity { Id = deployedChallenge.Id, Name = deployedChallenge.Name },
                VmUrls = vms.Select(vm => vm.Url)
            });

            request.Context.DeployedGamespaces.Add(challengeState);
        }
    }

    private void Log(string message, string gameId)
    {
        var prefix = $"""[EXTERNAL / SYNC - START GAME "{gameId}"] - {_now.Get()} - """;
        _logger.LogInformation($"{prefix} {message}");
    }
}
