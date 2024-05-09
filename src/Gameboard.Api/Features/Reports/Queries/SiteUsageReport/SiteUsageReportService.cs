using System.Linq;
using Gameboard.Api.Data;

namespace Gameboard.Api.Features.Reports;

public interface ISiteUsageReportService
{
    public IQueryable<Data.Challenge> GetBaseQuery(SiteUsageReportParameters parameters);
}

internal class SiteUsageReportService : ISiteUsageReportService
{
    private readonly IReportsService _reportsService;
    private readonly IStore _store;

    public SiteUsageReportService(IReportsService reportsService, IStore store)
    {
        _reportsService = reportsService;
        _store = store;
    }

    public IQueryable<Data.Challenge> GetBaseQuery(SiteUsageReportParameters parameters)
    {
        // this report is broadly about user behavior, but we fundamentally base that on challenges, so this query
        // centers on challenges
        var query = _store.WithNoTracking<Data.Challenge>();

        // Defend against teamId denormalization
        query = query.Where(c => c.TeamId != null && c.TeamId != string.Empty);

        if (parameters.StartDate.IsNotEmpty())
            query = query.Where(c => c.StartTime >= parameters.StartDate.Value.ToUniversalTime());

        if (parameters.EndDate.IsNotEmpty())
            query = query.Where(c => c.EndTime <= parameters.EndDate.Value.ToEndDate().ToUniversalTime());

        var sponsorIds = _reportsService.ParseMultiSelectCriteria(parameters.Sponsors);
        if (sponsorIds.IsNotEmpty())
            query = query.Where(c => sponsorIds.Contains(c.Player.SponsorId));

        return query;
    }
}
