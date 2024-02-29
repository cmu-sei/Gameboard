using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Scores;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Structure.MediatR.Authorizers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Admin;

public record GetAppActiveTeamsQuery() : IRequest<GetAppActiveTeamsResponse>;

internal class GetAppActiveTeamsHandler : IRequestHandler<GetAppActiveTeamsQuery, GetAppActiveTeamsResponse>
{
    private readonly IAppService _appService;
    private readonly INowService _now;
    private readonly IScoringService _scoringService;
    private readonly IStore _store;
    private readonly ITeamService _teamService;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;

    public GetAppActiveTeamsHandler
    (
        IAppService appService,
        INowService now,
        IScoringService scoringService,
        IStore store,
        ITeamService teamService,
        UserRoleAuthorizer userRoleAuthorizer
    )
    {
        _appService = appService;
        _now = now;
        _scoringService = scoringService;
        _store = store;
        _teamService = teamService;
        _userRoleAuthorizer = userRoleAuthorizer;
    }

    public async Task<GetAppActiveTeamsResponse> Handle(GetAppActiveTeamsQuery request, CancellationToken cancellationToken)
    {
        // authorize
        _userRoleAuthorizer
            .AllowRoles(UserRole.Admin, UserRole.Director, UserRole.Observer, UserRole.Support, UserRole.Designer)
            .Authorize();

        // pull active teams/games
        var nowish = _now.Get();
        var activeTeamsAndGames = await _appService
            .GetActiveChallenges()
            .Select(c => new { c.TeamId, c.GameId, c.Game.Name, c.Game.MaxTeamSize })
            .Distinct()
            .ToArrayAsync(cancellationToken);
        var activeTeamIds = activeTeamsAndGames.Select(t => t.TeamId);

        var captains = await _teamService.ResolveCaptains(activeTeamIds, cancellationToken);
        var challenges = await _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => activeTeamIds.Contains(c.TeamId))
            .GroupBy(c => c.TeamId)
            .ToDictionaryAsync(gr => gr.Key, gr => gr.Count());

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
                IsLateStart = captain.IsLateStart,
                DeployedChallengeCount = challenges.TryGetValue(p.Value.TeamId, out int challengeCount) ? challengeCount : 0,
                Score = scores.TryGetValue(captain.TeamId, out double value) ? value : 0,
                MsRemaining = captain.SessionEnd.IsNotEmpty() && captain.SessionEnd > nowish ? (captain.SessionEnd - nowish).TotalMilliseconds : null
            };
        });

        return new GetAppActiveTeamsResponse(teams);
    }
}
