using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public record GetSiteUsageReportQuery(SiteUsageReportParameters Parameters) : IRequest<SiteUsageReportRecord>, IReportQuery;

internal class GetSiteUsageReportHandler : IRequestHandler<GetSiteUsageReportQuery, SiteUsageReportRecord>
{
    private readonly ReportsQueryValidator _reportsQueryValidator;
    private readonly ISiteUsageReportService _siteUsageReportService;
    private readonly IStore _store;
    private readonly IValidatorService<GetSiteUsageReportQuery> _validatorService;

    public GetSiteUsageReportHandler
    (
        ReportsQueryValidator reportsQueryValidator,
        ISiteUsageReportService siteUsageReportService,
        IStore store,
        IValidatorService<GetSiteUsageReportQuery> validatorService
    )
    {
        _reportsQueryValidator = reportsQueryValidator;
        _siteUsageReportService = siteUsageReportService;
        _store = store;
        _validatorService = validatorService;
    }

    public async Task<SiteUsageReportRecord> Handle(GetSiteUsageReportQuery request, CancellationToken cancellationToken)
    {
        // auth/validate
        await _reportsQueryValidator.Validate(request, cancellationToken);

        // TODO better composability for reportsqueryvalidator
        _validatorService.AddValidator((req, ctx) =>
        {
            if (req.Parameters.StartDate.IsNotEmpty() && req.Parameters.EndDate.IsNotEmpty() && req.Parameters.StartDate > req.Parameters.EndDate)
                ctx.AddValidationException(new InvalidDateRange(new DateRange(req.Parameters.StartDate.Value.ToUniversalTime(), req.Parameters.EndDate.Value.ToUniversalTime())));
        });

        await _validatorService.Validate(request, cancellationToken);

        // let's party
        var challenges = await _siteUsageReportService
            .GetBaseQuery(request.Parameters)
            .Select(c => new
            {
                c.Id,
                DeployedDate = c.StartTime,
                IsCompetitive = c.PlayerMode == PlayerMode.Competition,
                c.SpecId,
                c.TeamId
            })
            .ToArrayAsync(cancellationToken);

        var teamIds = challenges
            .Select(r => r.TeamId)
            .Where(tId => tId != null && tId != string.Empty)
            .Distinct()
            .ToArray();
        var teamIdsUserIds = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => teamIds.Contains(p.TeamId))
            .Where(p => p.TeamId != null && p.TeamId != string.Empty)
            .Select(p => new
            {
                p.TeamId,
                p.UserId
            })
            .Distinct()
            .ToArrayAsync(cancellationToken);

        var teamUsers = teamIdsUserIds.GroupBy(e => e.TeamId).ToDictionary(gr => gr.Key, gr => gr.Select(e => e.UserId).ToArray());
        var userTeams = teamIdsUserIds.GroupBy(e => e.UserId).ToDictionary(gr => gr.Key, gr => gr.Select(e => e.TeamId).ToArray());
        var teamChallengeCounts = challenges
            .GroupBy(c => c.TeamId)
            .ToDictionary(c => c.Key, c => new
            {
                CompetitiveChallengeCount = c.Where(thing => thing.IsCompetitive).Count(),
                PracticeChallengeCount = c.Where(thing => !thing.IsCompetitive).Count(),
                // teamId may not be present because of crazy denormalization issues
                UserIds = teamUsers.TryGetValue(c.Key, out string[] value) ? value : Array.Empty<string>()
            });

        var competitiveTeamIds = teamChallengeCounts.Where(t => t.Value.CompetitiveChallengeCount > 0).Select(kv => kv.Key).ToArray();
        var competitiveUserIds = competitiveTeamIds.SelectMany(tId => teamUsers[tId]).Distinct().ToArray();
        var practiceTeamIds = teamChallengeCounts.Where(t => t.Value.PracticeChallengeCount > 0).Select(kv => kv.Key).ToArray();
        var practiceUserIds = practiceTeamIds.SelectMany(tId => teamUsers[tId]).Distinct().ToArray();
        var competitiveStrictTeamIds = teamChallengeCounts.Where(kv => kv.Value.CompetitiveChallengeCount > 0 && kv.Value.PracticeChallengeCount == 0);

        var userSponsorCount = await _store.WithNoTracking<Data.User>().Select(u => u.SponsorId).Distinct().CountAsync(cancellationToken);

        return new SiteUsageReportRecord
        {
            AvgCompetitiveChallengesPerCompetitiveUser = competitiveUserIds.Length == 0 ? null : challenges.Where(c => c.IsCompetitive).Count() / competitiveUserIds.Length,
            AvgPracticeChallengesPerPracticeUser = practiceUserIds.Length == 0 ? null : challenges.Where(c => !c.IsCompetitive).Count() / practiceUserIds.Length,
            CompetitiveUsersWithNoPracticeCount = competitiveUserIds.Where(uId => !practiceUserIds.Contains(uId)).Count(),
            DeployedChallengesCount = challenges.Length,
            DeployedChallengesCompetitiveCount = challenges.Where(c => c.IsCompetitive).Count(),
            DeployedChallengesPracticeCount = challenges.Where(c => !c.IsCompetitive).Count(),
            DeployedChallengesSpecCount = challenges.Select(c => c.SpecId).Distinct().Count(),
            PracticeUsersWithNoCompetitiveCount = practiceUserIds.Where(uId => !competitiveUserIds.Contains(uId)).Count(),
            SponsorCount = userSponsorCount,
            UserCount = userTeams.Keys.Count,
            UsersWithCompetitiveChallengeCount = competitiveUserIds.Length,
            UsersWithPracticeChallengeCount = practiceUserIds.Length
        };
    }
}
