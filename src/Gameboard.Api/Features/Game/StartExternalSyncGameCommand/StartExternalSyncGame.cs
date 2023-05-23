using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.Common;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Games;

public record StartExternalSyncGameCommand(string GameId) : IRequest<ExternalGameStartMetaData>;

internal class ExternalSyncGameDeployContext
{
    public required IEnumerable<string> TeamIds { get; set; }
    public required IList<Challenge> DeployedChallenges { get; set; }
    public required IList<GameEngineGameState> DeployedGamespaces { get; set; }
}

internal class StartExternalSyncGameHandler : IRequestHandler<StartExternalSyncGameCommand, ExternalGameStartMetaData>
{
    private readonly ChallengeService _challengeService;
    private readonly IGameEngineService _gameEngineService;
    private readonly IGameService _gameService;
    private readonly IGameStore _gameStore;
    private ILogger<StartExternalSyncGameHandler> _logger;
    private readonly IMapper _mapper;
    private readonly INowService _now;
    private readonly PlayerService _playerService;
    private readonly ITeamService _teamService;
    private readonly IValidatorService<StartExternalSyncGameCommand> _validator;

    public StartExternalSyncGameHandler
    (
        IGameService gameService,
        IGameStore gameStore,
        ILogger<StartExternalSyncGameHandler> logger,
        IMapper mapper,
        INowService now,
        PlayerService playerService,
        IValidatorService<StartExternalSyncGameCommand> validator
    )
    {
        _gameService = gameService;
        _gameStore = gameStore;
        _logger = logger;
        _mapper = mapper;
        _now = now;
        _playerService = playerService;
        _validator = validator;
    }

    public async Task<ExternalGameStartMetaData> Handle(StartExternalSyncGameCommand request, CancellationToken cancellationToken)
    {
        // TODO: validation
        var game = await _gameStore.Retrieve(request.GameId);
        var specs = await _gameService.RetrieveChallengeSpecs(request.GameId);
        var players = _mapper.Map<IEnumerable<Api.Player>>(await _playerService.List(new PlayerDataFilter { gid = request.GameId }));
        var teams = players.GroupBy(p => p.TeamId).ToDictionary(g => g.Key, g => _teamService.ResolveCaptain(g.Key, g.ToList()));
        var teamDeployedChallenges = new Dictionary<string, List<Api.Challenge>>();
        var challengeGamespaces = new Dictionary<string, ExternalGameStartTeamGamespace>();

        // this context holds everything we create along the way so we can try to roll it back upon exception
        var deployContext = new ExternalSyncGameDeployContext()
        {
            DeployedChallenges = new List<Challenge>(),
            DeployedGamespaces = new List<GameEngineGameState>(),
            TeamIds = teams.Keys
        };

        try
        {
            // validate that we have a team captain for every team before we do anything
            foreach (var teamId in teams.Keys)
            {
                if (teams[teamId] == null)
                    throw new CaptainResolutionFailure(teamId, "Couldn't resolve captain during external sync game start.");
            }

            // deploy all challenges
            foreach (var teamId in teams.Keys)
            {
                // hold onto each team's challenges
                teamDeployedChallenges.Add(teamId, new List<Challenge>());

                foreach (var specId in specs.Select(s => s.Id))
                {
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

                    deployContext.DeployedChallenges.Add(challenge);
                    teamDeployedChallenges[teamId].Add(challenge);
                }
            }

            // start all gamespaces
            foreach (var deployedChallenge in deployContext.DeployedChallenges)
            {
                _logger.LogInformation($"""Starting gamespace for challenge "{deployedChallenge.Id}" (teamId "{deployedChallenge.TeamId}")... """);
                var challengeState = await _gameEngineService.StartGamespace(deployedChallenge);
                _logger.LogInformation($"""Gamespace started for challenge "{deployedChallenge.Id}".""");

                challengeGamespaces.Add(deployedChallenge.Id, new ExternalGameStartTeamGamespace
                {
                    Id = challengeState.Id,
                    Challenge = new SimpleEntity { Id = deployedChallenge.Id, Name = deployedChallenge.Name },
                    // VmUrls = challengeState.Vms.Select(vm => vm.)
                });
                deployContext.DeployedGamespaces.Add(challengeState);
            }

            // establish all sessions
            var syncStartState = await _gameService.StartSynchronizedSession(game.Id);

            // TODO: notify gameboard to move players along

            // notify external client

            // build team objects to return
            var teamsToReturn = new List<ExternalGameStartMetaDataTeam>();
            foreach (var teamId in teams.Keys)
            {
                var teamToReturn = new ExternalGameStartMetaDataTeam
                {
                    Id = teamId,
                    Name = teams[teamId].ApprovedName
                };
            }

            return new ExternalGameStartMetaData
            {
                Game = new SimpleEntity { Id = game.Id, Name = game.Name },
                Session = new ExternalGameStartMetaDataSession
                {
                    Now = _now.Get(),
                    SessionBegin = syncStartState.SessionBegin,
                    SessionEnd = syncStartState.SessionEnd
                },
                Teams = teamsToReturn
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"""Error during external sync game deploy: "{ex.Message}".""");
            await TryCleanupFailedDeploy(deployContext);
        }
    }

    private Task TryCleanupFailedDeploy(ExternalSyncGameDeployContext ctx)
    {
        // TODO
        return Task.CompletedTask;
    }
}
