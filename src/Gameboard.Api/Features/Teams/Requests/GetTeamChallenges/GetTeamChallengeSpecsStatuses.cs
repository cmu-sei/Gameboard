using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Teams;

public record GetTeamChallengeSpecsStatusesQuery(string TeamId) : IRequest<GetTeamChallengeSpecsStatusesResponse>;

internal sealed class GetTeamChallengeSpecsStatusesHandler
(
    INowService now,
    IStore store,
    ITeamService teamService,
    IValidatorService validatorService
) : IRequestHandler<GetTeamChallengeSpecsStatusesQuery, GetTeamChallengeSpecsStatusesResponse>
{
    private readonly INowService _now = now;
    private readonly IStore _store = store;
    private readonly ITeamService _teamService = teamService;
    private readonly IValidatorService _validator = validatorService;

    public async Task<GetTeamChallengeSpecsStatusesResponse> Handle(GetTeamChallengeSpecsStatusesQuery request, CancellationToken cancellationToken)
    {
        var team = await _teamService.GetTeam(request.TeamId) ?? throw new ResourceNotFound<Team>(request.TeamId);
        var game = await _store
            .WithNoTracking<Data.Game>()
            .Where(g => g.Id == team.GameId)
            .Select(g => new SimpleEntity { Id = g.Id, Name = g.Name })
            .SingleOrDefaultAsync(cancellationToken);

        var userIds = team.Members.Select(p => p.UserId).ToArray();
        await _validator
            .Auth(c => c.RequireOneOf(PermissionKey.Admin_View).UnlessUserIdIn(userIds))
            .Validate(cancellationToken);

        var challengeSpecs = await _store
            .WithNoTracking<Data.ChallengeSpec>()
            .Where(s => s.GameId == team.GameId)
            .Where(s => !s.IsHidden && !s.Disabled)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.GameId,
                GameName = s.Game.Name,
                s.Points
            })
            .ToArrayAsync(cancellationToken);
        var specIds = challengeSpecs.Select(s => s.Id).ToArray();

        var teamChallenges = await _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => c.TeamId == request.TeamId)
            .Where(c => specIds.Contains(c.SpecId))
            .Select(c => new
            {
                c.Id,
                c.GameId,
                c.HasDeployedGamespace,
                c.SpecId,
                c.StartTime,
                c.EndTime,
                c.Points,
                c.Score
            })
            .ToDictionaryAsync
            (
                c => c.SpecId,
                c => c,
                cancellationToken
            );

        return new GetTeamChallengeSpecsStatusesResponse
        {
            Game = game,
            Team = new SimpleEntity { Id = team.TeamId, Name = team.ApprovedName },
            ChallengeSpecStatuses = challengeSpecs.Select(s =>
            {
                teamChallenges.TryGetValue(s.Id, out var challenge);

                // they haven't started
                if (challenge is null)
                {
                    return new TeamChallengeSpecStatus
                    {
                        AvailabilityRange = null,
                        ChallengeId = null,
                        Score = null,
                        ScoreMax = s.Points,
                        Spec = new SimpleEntity { Id = s.Id, Name = s.Name },
                        State = TeamChallengeSpecStatusState.NotStarted
                    };
                }

                return new TeamChallengeSpecStatus
                {
                    AvailabilityRange = new DateRange(challenge.StartTime, challenge.EndTime),
                    ChallengeId = challenge.Id,
                    Score = challenge.Score,
                    ScoreMax = challenge.Points,
                    Spec = new SimpleEntity { Id = s.Id, Name = s.Name },
                    State = ResolveStatus(challenge.HasDeployedGamespace, challenge.StartTime, challenge.EndTime)
                };
            })
            .ToArray()
        };
    }

    private TeamChallengeSpecStatusState ResolveStatus(bool isDeployed, DateTimeOffset startTime, DateTimeOffset endTime)
    {
        if (startTime.IsEmpty() && endTime.IsEmpty())
        {
            return TeamChallengeSpecStatusState.NotStarted;
        }

        var nowish = _now.Get();

        if (endTime.IsNotEmpty() && nowish > endTime)
        {
            return TeamChallengeSpecStatusState.Ended;
        }

        return isDeployed ? TeamChallengeSpecStatusState.Deployed : TeamChallengeSpecStatusState.NotDeployed;
    }
}
