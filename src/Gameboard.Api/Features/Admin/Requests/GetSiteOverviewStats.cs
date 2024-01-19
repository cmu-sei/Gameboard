using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR.Authorizers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Admin;

public sealed class GetSiteOverviewStatsResponse
{
    public required int ActiveCompetitiveChallenges { get; set; }
    public required int ActivePracticeChallenges { get; set; }
    public required int ActiveCompetitiveTeams { get; set; }
    public required int RegisteredUsers { get; set; }
}

public record GetSiteOverviewStatsQuery() : IRequest<GetSiteOverviewStatsResponse>;

internal class GetSiteOverviewStatsHandler : IRequestHandler<GetSiteOverviewStatsQuery, GetSiteOverviewStatsResponse>
{
    private readonly INowService _nowService;
    private readonly IStore _store;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;

    public GetSiteOverviewStatsHandler
    (
        INowService nowService,
        IStore store,
        UserRoleAuthorizer userRoleAuthorizer
    )
    {
        _nowService = nowService;
        _store = store;
        _userRoleAuthorizer = userRoleAuthorizer;
    }

    public async Task<GetSiteOverviewStatsResponse> Handle(GetSiteOverviewStatsQuery request, CancellationToken cancellationToken)
    {
        // authorize
        _userRoleAuthorizer
            .AllowRoles(UserRole.Admin, UserRole.Director, UserRole.Observer, UserRole.Support, UserRole.Designer)
            .Authorize();

        // pull data
        var now = _nowService.Get();
        var challengeData = await _store
            .WithNoTracking<Data.Challenge>()
            .WhereDateIsNotEmpty(c => c.StartTime)
            .Where(c => c.StartTime <= now)
            .Where(c => c.EndTime >= now || c.EndTime == DateTimeOffset.MinValue)
            .Select(c => new
            {
                c.Id,
                c.PlayerMode,
                c.TeamId
            })
            .ToArrayAsync(cancellationToken);

        var userCount = await _store
            .WithNoTracking<Data.User>()
            .CountAsync(cancellationToken);

        return new GetSiteOverviewStatsResponse
        {
            ActiveCompetitiveChallenges = challengeData
                .Where(c => c.PlayerMode == PlayerMode.Competition)
                .Count(),
            ActivePracticeChallenges = challengeData
                .Where(c => c.PlayerMode == PlayerMode.Practice)
                .Count(),
            ActiveCompetitiveTeams = challengeData
                .Where(c => c.PlayerMode == PlayerMode.Competition)
                .Select(c => c.TeamId)
                .Distinct()
                .Count(),
            RegisteredUsers = userCount
        };
    }
}
