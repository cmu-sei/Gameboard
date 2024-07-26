using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Scores;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Admin;

public record GetTeamCenterContextQuery(string TeamId, User User) : IRequest<TeamCenterContext>;

internal class GetTeamCenterContextHandler : IRequestHandler<GetTeamCenterContextQuery, TeamCenterContext>
{
    private readonly IScoringService _scoringService;
    private readonly IStore _store;
    private readonly TeamExistsValidator<GetTeamCenterContextQuery> _teamExists;
    private readonly UserRoleAuthorizer _userRoleAuth;
    private readonly IValidatorService<GetTeamCenterContextQuery> _validatorService;

    public GetTeamCenterContextHandler
    (
        IScoringService scoringService,
        IStore store,
        TeamExistsValidator<GetTeamCenterContextQuery> teamExists,
        UserRoleAuthorizer userRoleAuth,
        IValidatorService<GetTeamCenterContextQuery> validatorService)
    {
        _scoringService = scoringService;
        _store = store;
        _teamExists = teamExists;
        _userRoleAuth = userRoleAuth;
        _validatorService = validatorService;
    }

    public async Task<TeamCenterContext> Handle(GetTeamCenterContextQuery request, CancellationToken cancellationToken)
    {
        _validatorService.AddValidator(_teamExists.UseProperty(r => r.TeamId));

        _userRoleAuth.AllowAllElevatedRoles();
        if (!_userRoleAuth.WouldAuthorize())
        {
            _validatorService.AddValidator(async (req, ctx) =>
            {
                var userIsOnTeam = await _store
                    .WithNoTracking<Data.Player>()
                    .Where(p => p.TeamId == request.TeamId)
                    .Where(p => p.UserId == request.User.Id)
                    .AnyAsync(cancellationToken);

                if (!userIsOnTeam)
                    ctx.AddValidationException(new UserIsntOnTeam(request.User.Id, request.TeamId));
            });
        }

        await _validatorService.Validate(request, cancellationToken);

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

        // load all specs for the game (because we need their max scores and names)
        var specs = await _store
            .WithNoTracking<Data.ChallengeSpec>()
            .Where(s => s.GameId == captain.GameId)
            .Where(s => !s.Disabled && !s.IsHidden)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Points
            })
            .ToDictionaryAsync
            (
                s => s.Id,
                s => s,
                cancellationToken
            );

        var specIds = specs.Select(s => s.Key).ToArray();
        var challenges = await _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => c.TeamId == request.TeamId)
            .Where(c => specIds.Contains(c.SpecId))
            .Select(c => new
            {
                c.Id,
                c.Score,
                Start = c.StartTime > DateTimeOffset.MinValue ? c.StartTime.ToUnixTimeMilliseconds() : default(long?),
                End = c.EndTime > DateTimeOffset.MinValue ? c.EndTime.ToUnixTimeMilliseconds() : default(long?),
                c.SpecId
            })
            .ToArrayAsync(cancellationToken);

        var score = await _scoringService.GetTeamScore(request.TeamId, cancellationToken);
        var challengeScores = score.Challenges
            .Select(s => new { s.Id, s.SpecId, s.Score })
            .Where(s => specIds.Contains(s.SpecId))
            .ToDictionary(s => s.SpecId, s => s);

        return new TeamCenterContext
        {
            Id = request.TeamId,
            Name = captain.Name,
            Captain = new SimpleEntity { Id = captain.Id, Name = captain.Name },
            Challenges = challenges.Select(c => new TeamCenterContextChallenge
            {
                Id = c.Id,
                Start = c.Start,
                End = c.End,
                Score = challengeScores.TryGetValue(c.SpecId, out var value) ? value.Score : null,
                Spec = new SimpleEntity { Id = c.SpecId, Name = specs[c.SpecId].Name }
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
