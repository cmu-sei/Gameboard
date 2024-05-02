using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Player;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Teams;

internal class StartTeamSessionsValidator : IGameboardRequestValidator<StartTeamSessionsCommand>
{
    private readonly User _actingUser;
    private readonly IGameService _gameService;
    private readonly INowService _now;
    private readonly ISessionWindowCalculator _sessionWindow;
    private readonly IStore _store;
    private readonly IValidatorService<StartTeamSessionsCommand> _validatorService;

    public StartTeamSessionsValidator
    (
        IActingUserService actingUserService,
        IGameService gameService,
        INowService now,
        ISessionWindowCalculator sessionWindow,
        IStore store,
        IValidatorService<StartTeamSessionsCommand> validatorService
    )
    {
        _actingUser = actingUserService.Get();
        _gameService = gameService;
        _now = now;
        _sessionWindow = sessionWindow;
        _store = store;
        _validatorService = validatorService;
    }

    public async Task Validate(StartTeamSessionsCommand request, CancellationToken cancellationToken)
    {
        _validatorService.AddValidator((req, ctx) =>
        {
            if (!req.TeamIds.Any())
                ctx.AddValidationException(new MissingRequiredInput<IEnumerable<string>>(nameof(req.TeamIds), req.TeamIds));
        });

        var isGameStartSuperUser = _gameService.IsGameStartSuperUser(_actingUser);
        var gameData = await _store
            .WithNoTracking<Data.Game>()
                .Include(g => g.Players)
            .Where(g => g.Players.All(p => request.TeamIds.Contains(p.TeamId)))
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
                g.SessionMinutes,
                Players = g.Players.Select(p => new
                {
                    p.Id,
                    p.TeamId,
                    p.SessionBegin,
                    p.UserId
                })
            }).ToArrayAsync(cancellationToken);

        _validatorService.AddValidator(async (req, ctx) =>
        {
            var now = _now.Get();

            // must find team in exactly one game
            if (gameData.Length == 0)
            {
                foreach (var teamId in request.TeamIds)
                    ctx.AddValidationException(new ResourceNotFound<Team>(teamId));
            }
            else if (gameData.Length > 1)
                ctx.AddValidationException(new PlayersAreInMultipleGames(gameData.Select(g => g.Id)));

            // players must contain all passed team ids
            var unrepresentedTeamIds = request
                .TeamIds
                .Where(tId => !gameData.Any(g => g.Players.Any(p => p.TeamId == tId)))
                .ToArray();

            foreach (var teamId in unrepresentedTeamIds)
                ctx.AddValidationException(new ResourceNotFound<Team>(teamId));

            // the rest of this validation doesn't play if we're talking about more than one game, so bail if necessary
            if (gameData.Length > 1)
                return;

            var game = gameData.Single();

            if (game.RequireSynchronizedStart)
                throw new InvalidOperationException("Can't start a session for a sync start game with this command (use SyncStartService.StartSynchronizedSession).");

            var alreadyStartedPlayers = game.Players.Where(p => p.SessionBegin.IsNotEmpty());
            foreach (var alreadyStartedPlayer in alreadyStartedPlayers)
                ctx.AddValidationException(new SessionAlreadyStarted(alreadyStartedPlayer.Id, "Can't start a session for already started players."));

            // above validation is required, below only matters if you're not elevated
            if (isGameStartSuperUser)
                return;

            // can only start a session for a team of which the active user is a member
            var teamPlayers = game.Players.GroupBy(p => p.TeamId).ToDictionary(gr => gr.Key, gr => gr.ToArray());
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
            var sessionWindow = _sessionWindow.Calculate(game.SessionMinutes, game.GameEnd, isGameStartSuperUser, now);
            if (sessionWindow.IsLateStart && !game.AllowLateStart)
                ctx.AddValidationException(new CantLateStart(request.TeamIds, game.Name, game.GameEnd, game.SessionMinutes));
        });

        await _validatorService.Validate(request, cancellationToken);
    }
}
