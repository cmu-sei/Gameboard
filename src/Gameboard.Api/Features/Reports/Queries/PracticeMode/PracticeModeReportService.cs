using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

internal class PracticeModeReportService
{
    private readonly INowService _now;
    private readonly IStore _store;

    public PracticeModeReportService
    (
        INowService now,
        IStore store
    ) => (_now, _store) = (now, store);

    // public async Task<IEnumerable<PracticeModeReportRecord>> BuildResults(PracticeModeReportParameters parameters)
    // {
    //     return await _store
    //         .List<Data.Player>()
    //         .Where(p => parameters.AttemptDateRange.HasDateStartValue)
    //         .ToListAsync();
    // }
}
