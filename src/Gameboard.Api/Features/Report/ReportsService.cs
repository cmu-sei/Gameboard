using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public interface IReportsService
{
    Task<IEnumerable<Report>> List();
    Task<IEnumerable<string>> ListParameterOptionsCompetitions();
    Task<IEnumerable<string>> ListParameterOptionsTracks();
}

public class ReportsService : IReportsService
{
    private readonly IReportStore _store;

    public ReportsService(IReportStore store)
    {
        _store = store;
    }

    public async Task<IEnumerable<Report>> List()
        => await _store.List().ToArrayAsync();

    public async Task<IEnumerable<string>> ListParameterOptionsCompetitions()
        => await _store.GetCompetitions();

    public async Task<IEnumerable<string>> ListParameterOptionsTracks()
        => await _store.GetTracks();
}
