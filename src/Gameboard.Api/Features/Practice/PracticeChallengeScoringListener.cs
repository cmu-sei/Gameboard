using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Teams;

namespace Gameboard.Api.Features.Practice;

/// <summary>
/// We abstracted some practice-specific behaviors into this separate service because there
/// are cyclical dependencies between PlayerService and PracticeService.
/// </summary>
public interface IPracticeChallengeEventsListener
{
    Task NotifyAttemptsExhausted(Data.Challenge challenge, CancellationToken cancellationToken);
    Task NotifyChallengeCompleted(Data.Challenge challenge, CancellationToken cancellationToken);
}

internal class PracticeChallengeEventsListener : IPracticeChallengeEventsListener
{
    private IActingUserService _actingUser;
    private IGameEngineService _gameEngine;
    private IInternalHubBus _hubBus;
    private IMapper _mapper;
    private INowService _now;
    private IPlayerStore _playerStore;
    private IPracticeService _practiceService;
    private readonly ITeamService _teamService;

    public PracticeChallengeEventsListener
    (
        IActingUserService actingUser,
        IGameEngineService gameEngine,
        IInternalHubBus hubBus,
        IMapper mapper,
        INowService now,
        IPlayerStore playerStore,
        IPracticeService practiceService,
        ITeamService teamService
    )
    {
        _actingUser = actingUser;
        _gameEngine = gameEngine;
        _hubBus = hubBus;
        _mapper = mapper;
        _now = now;
        _playerStore = playerStore;
        _practiceService = practiceService;
        _teamService = teamService;
    }

    public Task NotifyChallengeCompleted(Data.Challenge challenge, CancellationToken cancellationToken)
        => _teamService.EndSession(challenge.TeamId, _actingUser.Get(), cancellationToken);



    public Task NotifyAttemptsExhausted(Data.Challenge challenge, CancellationToken cancellationToken)
        => _teamService.EndSession(challenge.TeamId, _actingUser.Get(), cancellationToken);
}
