using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Games.Start;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceStack;

namespace Gameboard.Api.Features.Teams;

public record StartTeamSessionsCommand(IEnumerable<string> TeamIds, bool ForceSynchronization = false) : IRequest<StartTeamSessionsResult>;

internal sealed class StartTeamSessionsHandler : IRequestHandler<StartTeamSessionsCommand, StartTeamSessionsResult>
{
    private readonly User _actingUser;
    private readonly IGameService _gameService;
    private readonly IGameStartService _gameStartService;
    private readonly IInternalHubBus _internalHubBus;
    private readonly ILogger<StartTeamSessionsHandler> _logger;
    private readonly INowService _now;
    private readonly ISessionWindowCalculator _sessionWindow;
    private readonly IStore _store;
    private readonly ITeamService _teamService;
    private readonly IGameboardRequestValidator<StartTeamSessionsCommand> _validator;

    public StartTeamSessionsHandler
    (
        IActingUserService actingUserService,
        IGameService gameService,
        IGameStartService gameStartService,
        IInternalHubBus internalHubBus,
        ILogger<StartTeamSessionsHandler> logger,
        INowService now,
        ISessionWindowCalculator sessionWindowCalculator,
        IStore store,
        ITeamService teamService,
        IGameboardRequestValidator<StartTeamSessionsCommand> validator
    )
    {
        _actingUser = actingUserService.Get();
        _now = now;
        _gameService = gameService;
        _gameStartService = gameStartService;
        _logger = logger;
        _internalHubBus = internalHubBus;
        _sessionWindow = sessionWindowCalculator;
        _store = store;
        _teamService = teamService;
        _validator = validator;
    }

    public async Task<StartTeamSessionsResult> Handle(StartTeamSessionsCommand request, CancellationToken cancellationToken)
    {
        await _validator.Validate(request, cancellationToken);

        var teams = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => request.TeamIds.Contains(p.TeamId))
            .Select(p => new
            {
                p.Id,
                p.ApprovedName,
                p.IsManager,
                p.UserId,
                p.TeamId,
                p.GameId
            })
            .GroupBy(p => p.TeamId)
            .ToDictionaryAsync(gr => gr.Key, gr => gr.ToArray(), cancellationToken);

        var gameId = teams.SelectMany(kv => kv.Value.Select(p => p.GameId)).Single();
        var gameData = await _store
            .WithNoTracking<Data.Game>()
            .Where(g => g.Id == gameId)
            .Select(g => new
            {
                g.Mode,
                g.SessionMinutes,
                g.GameEnd
            })
            .SingleAsync(cancellationToken);

        if (gameData.Mode == GameEngineMode.External)
        {
            await _gameStartService.Start(new GameStartRequest { TeamIds = teams.Keys }, cancellationToken);
        }

        var sessionWindow = _sessionWindow.Calculate(gameData.SessionMinutes, gameData.GameEnd, _gameService.IsGameStartSuperUser(_actingUser), _now.Get());

        // start all sessions
        await _store
            .WithNoTracking<Data.Player>()
            .Where(p => request.TeamIds.Contains(p.TeamId))
            .ExecuteUpdateAsync
            (
                up => up
                    .SetProperty(p => p.IsLateStart, sessionWindow.IsLateStart)
                    .SetProperty(p => p.SessionMinutes, sessionWindow.LengthInMinutes)
                    .SetProperty(p => p.SessionBegin, sessionWindow.Start)
                    .SetProperty(p => p.SessionEnd, sessionWindow.End),
                cancellationToken
            );

        var dict = new Dictionary<string, StartTeamSessionsResultTeam>();
        var finalTeams = teams.Select(kv => new StartTeamSessionsResultTeam
        {
            Id = kv.Key,
            Name = kv.Value.Single(p => p.IsManager).ApprovedName,
            ResourcesDeploying = gameData.Mode == GameEngineMode.External,
            Captain = kv.Value.Single(p => p.IsManager).ToSimpleEntity(p => p.Id, p => p.ApprovedName),
            Players = kv.Value.Select(p => new SimpleEntity
            {
                Id = p.Id,
                Name = p.ApprovedName
            }),
            SessionWindow = sessionWindow
        }).ToArray();

        foreach (var team in finalTeams)
        {
            dict.Add(team.Id, team);
            await _internalHubBus.SendTeamSessionStarted(team, gameId, _actingUser);
        }

        if (request.ForceSynchronization)
        {
            _logger.LogInformation($"Synchronizing session window for {request.TeamIds.Count()} teams...");
            foreach (var teamId in request.TeamIds)
                await _teamService.UpdateSessionStartAndEnd(teamId, sessionWindow.Start, sessionWindow.End, cancellationToken);

            _logger.LogInformation($"Sessions synchronized.");
        }

        return new StartTeamSessionsResult
        {
            SessionWindow = sessionWindow,
            Teams = dict
        };
    }
}
