using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Player;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Teams;

internal class StartTeamSessionsValidator : IGameboardRequestValidator<StartTeamSessionsCommand>
{
    private readonly User _actingUser;
    private readonly IGameModeServiceFactory _gameModeServiceFactory;
    private readonly IGameService _gameService;
    private readonly INowService _now;
    private readonly IUserRolePermissionsService _permissionsService;
    private readonly ISessionWindowCalculator _sessionWindow;
    private readonly IStore _store;
    private readonly IValidatorService<StartTeamSessionsCommand> _validatorService;

    public StartTeamSessionsValidator
    (
        IActingUserService actingUserService,
        IGameService gameService,
        IGameModeServiceFactory gameModeServiceFactory,
        INowService now,
        IUserRolePermissionsService permissionsService,
        ISessionWindowCalculator sessionWindow,
        IStore store,
        IValidatorService<StartTeamSessionsCommand> validatorService
    )
    {
        _actingUser = actingUserService.Get();
        _gameModeServiceFactory = gameModeServiceFactory;
        _gameService = gameService;
        _now = now;
        _permissionsService = permissionsService;
        _sessionWindow = sessionWindow;
        _store = store;
        _validatorService = validatorService;
    }

    public async Task Validate(StartTeamSessionsCommand request, CancellationToken cancellationToken)
    {
        _validatorService.AddValidator(async (req, ctx) =>
        {
            if (!req.TeamIds.Any())
                ctx.AddValidationException(new MissingRequiredInput<IEnumerable<string>>(nameof(req.TeamIds), req.TeamIds));

            var now = _now.Get();

            var players = await _store
                .WithNoTracking<Data.Player>()
                .Where(p => request.TeamIds.Contains(p.TeamId))
                .Select(p => new
                {
                    p.Id,
                    p.GameId,
                    p.SessionBegin,
                    p.TeamId,
                    p.UserId
                })
                .ToArrayAsync(cancellationToken);

            var gameIds = players.Select(p => p.GameId).Distinct().ToArray();

            // must find team in exactly one game
            if (gameIds.Length == 0)
            {
                foreach (var teamId in request.TeamIds)
                    ctx.AddValidationException(new ResourceNotFound<Team>(teamId));
            }
            else if (gameIds.Length > 1)
                ctx.AddValidationException(new PlayersAreInMultipleGames(gameIds));

            // the rest of this validation doesn't play if we're talking about more than one game, so bail if necessary
            if (gameIds.Length != 1)
                return;

            var game = await _store
                .WithNoTracking<Data.Game>()
                .Where(g => g.Id == gameIds[0])
                .Select(g => new
                {
                    g.Id,
                    g.Name,
                    g.AllowLateStart,
                    g.MinTeamSize,
                    g.MaxTeamSize,
                    g.GameStart,
                    g.GameEnd,
                    g.RequireSynchronizedStart,
                    g.SessionLimit,
                    g.SessionMinutes
                }).SingleAsync(cancellationToken);

            // players must contain all passed team ids
            var unrepresentedTeamIds = request
                .TeamIds
                .Where(tId => !players.Any(p => p.TeamId == tId))
                .ToArray();

            foreach (var teamId in unrepresentedTeamIds)
                ctx.AddValidationException(new ResourceNotFound<Team>(teamId));

            var alreadyStartedPlayers = players.Where(p => p.SessionBegin.IsNotEmpty());
            foreach (var alreadyStartedPlayer in alreadyStartedPlayers)
                ctx.AddValidationException(new SessionAlreadyStarted(alreadyStartedPlayer.Id, "Can't start a session for already started players."));

            // above validation is required, below only matters if you're not elevated
            if (await _permissionsService.Can(PermissionKey.Teams_EditSession))
                return;

            // can only start a session for a team of which the active user is a member
            var teamPlayers = players.GroupBy(p => p.TeamId).ToDictionary(gr => gr.Key, gr => gr.ToArray());
            foreach (var team in teamPlayers)
                if (team.Value.All(p => p.UserId != _actingUser.Id))
                    ctx.AddValidationException(new CantStartSessionOfOtherTeam(team.Key, _actingUser.Id));

            // have to respect team size
            foreach (var team in teamPlayers)
                if (team.Value.Length < game.MinTeamSize || team.Value.Length > game.MaxTeamSize)
                    ctx.AddValidationException(new InvalidTeamSize(team.Key, team.Value.Length, game.MinTeamSize, game.MaxTeamSize));

            // can only play active games
            if (game.GameStart.IsEmpty() || game.GameStart > now || game.GameEnd < now)
                ctx.AddValidationException(new GameNotActive(game.Id, game.GameStart, game.GameEnd));

            // can't exceed the legal session limit if established
            var activeSessions = await _gameService.GetTeamsWithActiveSession(game.Id, cancellationToken);
            if (game.SessionLimit > 0 && activeSessions.Count() >= game.SessionLimit)
                ctx.AddValidationException(new SessionLimitReached(request.TeamIds.First(), game.Id, activeSessions.Count(), game.SessionLimit));

            // can't start late if late start disabled
            var sessionWindow = _sessionWindow.Calculate(game.SessionMinutes, game.GameEnd, await _permissionsService.Can(PermissionKey.Play_IgnoreExecutionWindow), now);
            if (sessionWindow.IsLateStart && !game.AllowLateStart)
                ctx.AddValidationException(new CantLateStart(request.TeamIds, game.Name, game.GameEnd, game.SessionMinutes));

            // enforce mode-specific validation
            var modeService = await _gameModeServiceFactory.Get(game.Id);
            var startRequest = new GameModeStartRequest
            {
                Game = new SimpleEntity { Id = game.Id, Name = game.Name },
                TeamIds = request.TeamIds
            };

            await modeService.ValidateStart(startRequest, cancellationToken);
        });

        await _validatorService.Validate(request, cancellationToken);
    }
}
