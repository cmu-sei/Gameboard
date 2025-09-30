// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Scores;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Admin;

public record GetTeamCenterContextQuery(string TeamId, User User) : IRequest<TeamCenterContext>;

internal class GetTeamCenterContextHandler(
    IScoringService scoringService,
    IStore store,
    TeamExistsValidator<GetTeamCenterContextQuery> teamExists,
    ITeamService teamService,
    IValidatorService<GetTeamCenterContextQuery> validatorService) : IRequestHandler<GetTeamCenterContextQuery, TeamCenterContext>
{
    private readonly IScoringService _scoringService = scoringService;
    private readonly IStore _store = store;
    private readonly TeamExistsValidator<GetTeamCenterContextQuery> _teamExists = teamExists;
    private readonly ITeamService _teamService = teamService;
    private readonly IValidatorService<GetTeamCenterContextQuery> _validatorService = validatorService;

    public async Task<TeamCenterContext> Handle(GetTeamCenterContextQuery request, CancellationToken cancellationToken)
    {
        await _validatorService
            .Auth(config =>
            {
                config
                    .Require(PermissionKey.Admin_View)
                    .Unless
                    (
                        () => _store
                            .WithNoTracking<Data.Player>()
                            .Where(p => p.TeamId == request.TeamId)
                            .Where(p => p.UserId == request.User.Id)
                            .Select(p => p.UserId)
                            .Distinct()
                            .AnyAsync(cancellationToken)
                    );
            })
            .Validate(request, cancellationToken);

        var players = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.TeamId == request.TeamId)
            .Select(p => new
            {
                p.Id,
                Name = p.ApprovedName,
                p.GameId,
                IsCaptain = p.Role == PlayerRole.Manager,
                Sponsor = new SimpleSponsor
                {
                    Id = p.SponsorId,
                    Name = p.Sponsor.Name,
                    Logo = p.Sponsor.Logo
                },
                User = new SimpleEntity { Id = p.UserId, Name = p.User.ApprovedName },
                p.WhenCreated,
            })
            .OrderBy(p => p.IsCaptain ? 0 : 1)
            .ToArrayAsync(cancellationToken);

        // we assume that there's a captain and that they're the first player
        // because that's how things are implemented, but there's no schema constraint
        // that enforces this. either way, we need a representative player, so
        // take the first for name/challenge resolution
        var captain = players.First();

        // var specIds = specs.Select(s => s.Key).ToArray();
        var challenges = await _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => c.Spec.GameId == captain.GameId)
            .Where(c => !c.Spec.Disabled && !c.Spec.IsHidden)
            .Where(c => c.TeamId == request.TeamId)
            .Select(c => new
            {
                c.Id,
                c.Score,
                Start = c.StartTime > DateTimeOffset.MinValue ? c.StartTime.ToUnixTimeMilliseconds() : default(long?),
                End = c.EndTime > DateTimeOffset.MinValue ? c.EndTime.ToUnixTimeMilliseconds() : default(long?),
                Spec = new SimpleEntity { Id = c.SpecId, Name = c.Spec.Name },
            })
            .ToArrayAsync(cancellationToken);

        var score = await _scoringService.GetTeamScore(request.TeamId, cancellationToken);
        var challengeScores = score.Challenges
            .Select(s => new { s.Id, s.SpecId, s.Score })
            .ToDictionary(s => s.SpecId, s => s);

        return new TeamCenterContext
        {
            Id = request.TeamId,
            Name = captain.Name,
            Captain = new SimpleEntity
            {
                Id = captain.Id,
                Name = captain.Name
            },
            Challenges = challenges.Select(c => new TeamCenterContextChallenge
            {
                Id = c.Id,
                Start = c.Start,
                End = c.End,
                Score = challengeScores.TryGetValue(c.Spec.Id, out var value) ? value.Score : null,
                Spec = c.Spec
            }),
            Players = players.Select(p => new TeamCenterContextPlayer
            {
                Id = p.Id,
                Name = p.Name,
                Sponsor = p.Sponsor,
                User = p.User,
            }),
            Score = score.OverallScore,
        };
    }
}
