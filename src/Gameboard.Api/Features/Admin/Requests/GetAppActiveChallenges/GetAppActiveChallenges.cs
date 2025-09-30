// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

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
public sealed record GetAppActiveChallengesResponse(IEnumerable<AppActiveChallengeSpec> Specs);

internal class GetAppActiveChallengesHandler
(
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
                Spec = new
                {
                    Id = c.SpecId,
                    c.Spec.Name,
                    c.Spec.Tag,
                    c.Spec.GameEngineType,
                    Game = new
                    {
                        Id = c.Spec.GameId,
                        c.Spec.Game.Name,
                        c.Spec.Game.MaxTeamSize,
                    }
                },
                c.TeamId,
                c.StartTime,
                HasTickets = c.Tickets.Any(t => t.Status != "closed")
            })
            .ToArrayAsync(cancellationToken);

        var teamIds = challenges.Select(c => c.TeamId).Distinct().ToArray();

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
                    Tag = sampleChallenge.Spec.Tag,
                    Game = new AppActiveChallengeGame
                    {
                        Id = sampleChallenge.Spec.Game.Id,
                        Name = sampleChallenge.Spec.Game.Name,
                        IsTeamGame = sampleChallenge.Spec.Game.MaxTeamSize > 1,
                        Engine = sampleChallenge.Spec.GameEngineType
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
