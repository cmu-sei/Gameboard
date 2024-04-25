using System.Linq;
using System.Threading;
using Gameboard.Api.Data;

namespace Gameboard.Api.Features.Reports;

public interface ISiteUsageReportService
{
    public IQueryable<Data.Challenge> GetBaseQuery(SiteUsageReportParameters parameters);
}

internal class SiteUsageReportService : ISiteUsageReportService
{
    private readonly IStore _store;

    public SiteUsageReportService(IStore store)
    {
        _store = store;
    }

    public IQueryable<Data.Challenge> GetBaseQuery(SiteUsageReportParameters parameters)
    {
        // this report is broadly about user behavior, but we fundamentally base that on challenges, so this query
        // centers on challenges
        var query = _store.WithNoTracking<Data.Challenge>();

        if (parameters.StartDate.IsNotEmpty())
            query = query.Where(c => c.StartTime >= parameters.StartDate.Value);

        if (parameters.EndDate.IsNotEmpty())
            query = query.Where(c => c.EndTime <= parameters.EndDate.Value);

        if (parameters.SponsorId.IsNotEmpty())
            query = query.Where(c => c.Player.User.SponsorId == parameters.SponsorId);

        return query;
    }

    
}
