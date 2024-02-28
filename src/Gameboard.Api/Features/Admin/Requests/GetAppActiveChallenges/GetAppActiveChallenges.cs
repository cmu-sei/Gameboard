using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Structure.MediatR.Authorizers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Admin;

public sealed record GetAppActiveChallengesQuery(PlayerMode PlayerMode) : IRequest<GetAppActiveChallengesResponse>;
public sealed record GetAppActiveChallengesResponse
(
    IEnumerable<AppActiveChallengeSpec> Specs
);

internal class GetAppActiveChallengesHandler : IRequestHandler<GetAppActiveChallengesQuery, GetAppActiveChallengesResponse>
{
    private readonly IAppOverviewService _appOverviewService;
    private readonly IStore _store;
    private readonly ITeamService _teamService;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;

    public GetAppActiveChallengesHandler
    (
        IAppOverviewService appOverviewService,
        IStore store,
        ITeamService teamService,
        UserRoleAuthorizer userRoleAuthorizer
    )
    {
        _appOverviewService = appOverviewService;
        _store = store;
        _teamService = teamService;
        _userRoleAuthorizer = userRoleAuthorizer;
    }

    public async Task<GetAppActiveChallengesResponse> Handle(GetAppActiveChallengesQuery request, CancellationToken cancellationToken)
    {
        _userRoleAuthorizer
            .AllowRoles(UserRole.Observer, UserRole.Tester, UserRole.Designer, UserRole.Director, UserRole.Registrar, UserRole.Support)
            .Authorize();

        var challenges = await _appOverviewService
            .GetActiveChallenges()
            .Include(c => c.Game)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.SpecId,
                c.TeamId,
                Game = new SimpleEntity { Id = c.GameId, Name = c.Game.Name },
                GameEngine = c.GameEngineType,
                c.StartTime,
            })
            .ToArrayAsync(cancellationToken);

        var specIds = challenges.Select(c => c.SpecId).Distinct().ToArray();
        var teamIds = challenges.Select(c => c.TeamId).Distinct().ToArray();

        // get specs separately because _ugh_
        var specs = await _store
            .WithNoTracking<Data.ChallengeSpec>()
            .Where(s => specIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s, cancellationToken);

        // get teams
        var teams = (await _teamService.GetTeams(teamIds))
            .GroupBy(t => t.TeamId)
            .ToDictionary(gr => gr.Key, gr => gr.Single());

        // project results
        var results = challenges
            .GroupBy(c => c.SpecId)
            .ToDictionary(gr => gr.Key, gr => gr.ToArray())
            .Where(gr => gr.Value.Length > 0)
            .Select(gr =>
            {
                // the dictionary is a key/value store of spec Id to various challenge data
                var sampleChallenge = gr.Value.First();

                return new AppActiveChallengeSpec
                {
                    Id = gr.Key,
                    Name = gr.Value.First().Name,
                    Challenges = gr.Value.Select(c => new AppActiveChallenge
                    {
                        Id = c.Id,
                        Game = c.Game,
                        GameEngine = c.GameEngine,
                        StartedAt = c.StartTime,
                        Team = new AppActiveChallengeTeam
                        {
                            Id = c.TeamId,
                            Name = teams.ContainsKey(c.TeamId) ? teams[c.TeamId].ApprovedName : "",
                            Session = teams.ContainsKey(c.TeamId) ? new DateRange
                            {
                                Start = teams[c.TeamId].SessionBegin,
                                End = teams[c.TeamId].SessionEnd
                            } : null
                        }
                    })
                };
            });

        return new GetAppActiveChallengesResponse(results);
    }
}
