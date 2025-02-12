using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Admin;

public sealed record GetAppActiveChallengesQuery(PlayerMode PlayerMode) : IRequest<GetAppActiveChallengesResponse>;
public sealed record GetAppActiveChallengesResponse
(
    IEnumerable<AppActiveChallengeSpec> Specs
);

internal class GetAppActiveChallengesHandler(
    IAppService appOverviewService,
    IStore store,
    ITeamService teamService,
    IValidatorService validatorService
    ) : IRequestHandler<GetAppActiveChallengesQuery, GetAppActiveChallengesResponse>
{
    private readonly IAppService _appOverviewService = appOverviewService;
    private readonly IStore _store = store;
    private readonly ITeamService _teamService = teamService;
    private readonly IValidatorService _validatorService = validatorService;

    public async Task<GetAppActiveChallengesResponse> Handle(GetAppActiveChallengesQuery request, CancellationToken cancellationToken)
    {
        await _validatorService
            .Auth(config => config.Require(PermissionKey.Admin_View))
            .Validate(cancellationToken);

        var challenges = await _appOverviewService
            .GetActiveChallenges()
            .Where(c => c.PlayerMode == request.PlayerMode)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.SpecId,
                c.TeamId,
                c.StartTime,
                HasTickets = c.Tickets.Any(t => t.Status != "closed")
            })
            .ToArrayAsync(cancellationToken);

        var specIds = challenges.Select(c => c.SpecId).Distinct().ToArray();
        var teamIds = challenges.Select(c => c.TeamId).Distinct().ToArray();

        // get specs separately because _ugh_
        var specs = await _store
            .WithNoTracking<Data.ChallengeSpec>()
            .Include(s => s.Game)
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
                var specId = gr.Key;
                var sampleChallenge = gr.Value.First();

                return new AppActiveChallengeSpec
                {
                    Id = gr.Key,
                    Name = gr.Value.First().Name,
                    Tag = specs[specId].Tag,
                    Game = new AppActiveChallengeGame
                    {
                        Id = specs[specId].GameId,
                        Name = specs[specId].Game.Name,
                        IsTeamGame = specs[specId].Game.MaxTeamSize > 1,
                        Engine = specs[specId].GameEngineType
                    },
                    Challenges = gr.Value.Select(c => new AppActiveChallenge
                    {
                        Id = c.Id,
                        StartedAt = c.StartTime,
                        HasTickets = c.HasTickets,
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
                    .OrderBy(s => s.Team.Name)
                };
            })
            .OrderBy(spec => spec.Name);

        return new GetAppActiveChallengesResponse(results);
    }
}
