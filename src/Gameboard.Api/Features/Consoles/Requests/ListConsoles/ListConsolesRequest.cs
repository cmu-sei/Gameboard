#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Consoles;

public sealed class ListConsolesQuery : IRequest<ListConsolesResponse>
{
    public string? ChallengeSpecId { get; set; }
    public string? GameId { get; set; }
    public string? TeamId { get; set; }
    public PlayerMode? PlayerMode { get; set; }
    public string? SearchTerm { get; set; }
    public ListConsolesRequestSort? SortBy { get; set; }
}

internal sealed class GetConsolesHandler
(
    IActingUserService actingUserService,
    ConsoleActorMap consoleActorMap,
    IGameEngineService gameEngine,
    EntityExistsValidator<Data.Game> gameExists,
    IStore store,
    ITeamService teamsService,
    IUserRolePermissionsService userPermissions,
    IValidatorService validator
) : IRequestHandler<ListConsolesQuery, ListConsolesResponse>
{
    public async Task<ListConsolesResponse> Handle(ListConsolesQuery request, CancellationToken cancellationToken)
    {
        var canObserveTeams = await userPermissions.Can(PermissionKey.Teams_Observe);

        validator
            .Auth(c => c.RequireAuthentication())
            .AddValidator(async ctx =>
            {
                if (request.TeamId.IsNotEmpty())
                {
                    if (canObserveTeams || await teamsService.IsOnTeam(request.TeamId, actingUserService.Get().Id))
                    {
                        return;
                    }

                    ctx.AddValidationException(new ConsoleTeamNoAccessException(request.TeamId));
                }
            });

        // validate game existence if supplied
        if (request.GameId.IsNotEmpty())
        {
            validator.AddValidator(gameExists.UseValue(request.GameId));
        }

        // run validation
        await validator.Validate(cancellationToken);

        // normalize arguments
        request.SortBy ??= ListConsolesRequestSort.Rank;
        if (request.SearchTerm.IsEmpty())
        {
            request.SearchTerm = null;
        }
        else
        {
            request.SearchTerm = request.SearchTerm!.ToLower();
        }

        // we need to which teams the caller is on to determine which consoles they can access
        var userTeams = await teamsService.GetUserTeamIds(actingUserService.Get().Id);

        // if you have the Team Observe permission, we just follow your parameters.
        // if you don't, we use your parameters but restrict results to teams you're on.
        var queryTeamIds = Array.Empty<string>();
        if (canObserveTeams)
        {
            queryTeamIds = request.TeamId.IsNotEmpty() ? [request.TeamId!] : [];
        }
        else
        {
            queryTeamIds = userTeams;
        }

        // load active challenges in the game
        var challenges = await store
            .WithNoTracking<Data.Challenge>()
            .Where(c => c.HasDeployedGamespace)
            .Where(c => request.GameId == null || c.GameId == request.GameId)
            .Where(c => request.ChallengeSpecId == null || c.SpecId == request.ChallengeSpecId)
            .Where(c => queryTeamIds.Length == 0 || queryTeamIds.Contains(c.TeamId))
            .Where(c => request.PlayerMode == null || request.PlayerMode == c.PlayerMode)
            .Where(c => request.SearchTerm == null || c.TeamId.ToLower() == request.SearchTerm || c.Id.ToLower() == request.SearchTerm)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.EndTime,
                c.PlayerMode,
                c.SpecId,
                c.State,
                c.TeamId
            })
            .ToDictionaryAsync(c => c.Id, c => c, cancellationToken);

        // constitute teams, captains, and consoles from challenge data
        var gameEngineType = GameEngineType.TopoMojo;
        var teamIds = challenges.Values.Select(c => c.TeamId).Distinct().ToArray();
        var captains = await teamsService.ResolveCaptains(teamIds, cancellationToken);

        // also need scores and ranks for the teams
        var teamScoreData = await store
            .WithNoTracking<DenormalizedTeamScore>()
            .Where(s => teamIds.Contains(s.TeamId))
            .Select(s => new
            {
                s.TeamId,
                s.Rank,
                s.ScoreOverall
            })
            .ToDictionaryAsync(gr => gr.TeamId, gr => gr, cancellationToken);

        // and the map which knows who's using which consoles
        var gameConsoleUsers = consoleActorMap.Find().GroupBy(a => a.ChallengeId).ToDictionary(gr => gr.Key, gr => gr.DistinctBy(a => a.UserId).ToArray());

        // load console stuff last, because access tickets are time-sensitive
        var consoleIds = new List<ConsoleId>();
        foreach (var challenge in challenges.Values)
        {
            var state = await gameEngine.GetChallengeState(gameEngineType, challenge.State);
            var challengeVms = gameEngine.GetVmsFromState(state);
            consoleIds.AddRange(challengeVms.Select(vm => new ConsoleId { ChallengeId = challenge.Id, Name = vm.Name }));
        }

        var consoles = await gameEngine.GetConsoles(gameEngineType, [.. consoleIds], cancellationToken);

        // construct and return the response
        var responseConsoles = new List<ListConsolesResponseConsole>();
        foreach (var c in consoles)
        {
            if (!challenges.TryGetValue(c.Id.ChallengeId, out var challenge))
            {
                continue;
            }

            gameConsoleUsers.TryGetValue(c.Id.ChallengeId, out var consoleUsers);
            consoleUsers ??= [];

            responseConsoles.Add(new ListConsolesResponseConsole
            {
                ConsoleId = c.Id,
                AccessTicket = c.AccessTicket,
                ActiveUsers = [.. consoleUsers.Where(u => u.VmName == c.Id.Name && u.ChallengeId == challenge.Id).Select(u => new SimpleEntity { Id = u.UserId, Name = u.PlayerName })],
                Challenge = new ListConsolesResponseChallenge
                {
                    Id = challenge.Id,
                    Name = challenge.Name,
                    IsPractice = challenge.PlayerMode == PlayerMode.Practice,
                    SpecId = challenge.SpecId
                },
                IsViewOnly = !userTeams.Contains(challenge.TeamId),
                Team = new ListConsolesResponseTeam
                {
                    Id = challenge.TeamId,
                    Name = captains[challenge.TeamId].ApprovedName,
                    Rank = teamScoreData.ContainsKey(challenge.TeamId) ? teamScoreData[challenges[c.Id.ChallengeId].TeamId].Rank : null,
                    Score = teamScoreData.ContainsKey(challenge.TeamId) ? teamScoreData[challenges[c.Id.ChallengeId].TeamId].ScoreOverall : null,
                },
                TeamId = challenges[c.Id.ChallengeId].TeamId,
                Url = c.Url
            });
        }

        // respect sort by
        if (request.SortBy == ListConsolesRequestSort.Rank)
        {
            responseConsoles = [.. responseConsoles
                .OrderBy(c => c.Team.Rank)
                .ThenBy(c => c.Challenge.Name)
                .ThenBy(c => c.ConsoleId.Name)];
        }
        else
        {
            responseConsoles = [.. responseConsoles
                .OrderBy(c => c.Team.Name)
                .ThenBy(c => c.Challenge.Name)
                .ThenBy(c => c.ConsoleId.Name)];
        }

        return new ListConsolesResponse { Consoles = [.. responseConsoles] };
    }
}
