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

namespace Gameboard.Api.Features.Admin;

public sealed class GetAppOverviewStatsResponse
{
    public required int ActiveCompetitiveChallenges { get; set; }
    public required int ActivePracticeChallenges { get; set; }
    public required int ActiveCompetitiveTeams { get; set; }
    public required int RegisteredUsers { get; set; }
}

public record GetAppOverviewStatsQuery() : IRequest<GetAppOverviewStatsResponse>;

internal class GetAppOverviewStatsHandler(
    IAppService appOverviewService,
    INowService nowService,
    IStore store,
    IValidatorService validatorService
    ) : IRequestHandler<GetAppOverviewStatsQuery, GetAppOverviewStatsResponse>
{
    private readonly IAppService _appOverviewService = appOverviewService;
    private readonly INowService _nowService = nowService;
    private readonly IStore _store = store;
    private readonly IValidatorService _validatorService = validatorService;

    public async Task<GetAppOverviewStatsResponse> Handle(GetAppOverviewStatsQuery request, CancellationToken cancellationToken)
    {
        // authorize
        await _validatorService
            .Auth(config => config.RequirePermissions(PermissionKey.Admin_View))
            .Validate(cancellationToken);

        // pull data
        var now = _nowService.Get();
        var challengeData = await _appOverviewService
            .GetActiveChallenges()
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

        return new GetAppOverviewStatsResponse
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
