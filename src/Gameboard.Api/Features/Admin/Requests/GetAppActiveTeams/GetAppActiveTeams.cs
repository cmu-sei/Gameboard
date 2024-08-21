using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Scores;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Admin;

public record GetAppActiveTeamsQuery() : IRequest<GetAppActiveTeamsResponse>;

internal class GetAppActiveTeamsHandler(
    IAppService appService,
    INowService now,
    IScoringService scoringService,
    IStore store,
    ITeamService teamService,
    IValidatorService validatorService
    ) : IRequestHandler<GetAppActiveTeamsQuery, GetAppActiveTeamsResponse>
{
    private readonly IAppService _appService = appService;
    private readonly INowService _now = now;
    private readonly IScoringService _scoringService = scoringService;
    private readonly IStore _store = store;
    private readonly ITeamService _teamService = teamService;
    private readonly IValidatorService _validatorService = validatorService;

    public async Task<GetAppActiveTeamsResponse> Handle(GetAppActiveTeamsQuery request, CancellationToken cancellationToken)
    {
        // authorize
        await _validatorService
            .ConfigureAuthorization(config => config.RequirePermissions(PermissionKey.Admin_View))
            .Validate(cancellationToken);

        // pull active teams/games
        var nowish = _now.Get();
        var activeTeamsAndGames = await _appService
            .GetActiveChallenges()
            .Where(c => c.PlayerMode == PlayerMode.Competition)
            .Select(c => new { c.TeamId, c.GameId, c.Game.Name, c.Game.MaxTeamSize })
            .Distinct()
            .ToArrayAsync(cancellationToken);
        var activeTeamIds = activeTeamsAndGames.Select(t => t.TeamId);

        var captains = await _teamService.ResolveCaptains(activeTeamIds, cancellationToken);
        var challenges = await _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => activeTeamIds.Contains(c.TeamId))
            .GroupBy(c => c.TeamId)
            .ToDictionaryAsync(gr => gr.Key, gr => gr.Count(), cancellationToken);

        var teamIdsWithTickets = await _store
            .WithNoTracking<Data.Ticket>()
            .Where(t => t.Status != "Closed")
            .Select(t => t.TeamId)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        var scores = new Dictionary<string, double>();
        foreach (var captain in captains)
        {
            var teamScore = await _scoringService.GetTeamScore(captain.Key, cancellationToken);
            scores.Add(captain.Key, teamScore.OverallScore.TotalScore);
        }

        var teams = captains.Select(p =>
        {
            var captain = p.Value;

            return new AppActiveTeam
            {
                Id = p.Value.TeamId,
                Name = p.Value.ApprovedName,
                Game = activeTeamsAndGames
                    .Where(d => d.TeamId == captain.TeamId)
                    .Select(d => new AppActiveTeamGame { Id = d.GameId, Name = d.Name, IsTeamGame = d.MaxTeamSize > 1 })
                    .SingleOrDefault(),
                Session = new DateRange
                {
                    Start = captain.SessionBegin,
                    End = captain.SessionEnd
                },
                HasTickets = teamIdsWithTickets.Contains(captain.TeamId),
                IsLateStart = captain.IsLateStart,
                DeployedChallengeCount = challenges.TryGetValue(p.Value.TeamId, out int challengeCount) ? challengeCount : 0,
                Score = scores.TryGetValue(captain.TeamId, out double value) ? value : 0,
                MsRemaining = captain.SessionEnd.IsNotEmpty() && captain.SessionEnd > nowish ? (captain.SessionEnd - nowish).TotalMilliseconds : null
            };
        });

        return new GetAppActiveTeamsResponse(teams);
    }
}
