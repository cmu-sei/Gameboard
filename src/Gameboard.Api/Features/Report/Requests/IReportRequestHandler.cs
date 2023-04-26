using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gameboard.Api.Features.Reports;

internal interface IReportRequestHandler<TParams, TRecord, TResults>
{
    IQueryable<TRecord> BuildQuery(TParams reportParams);
    Task<IEnumerable<TRecord>> FetchRecords(IQueryable<TRecord> query);
    Task<TResults> BuildResults(IEnumerable<TRecord> records);
}
