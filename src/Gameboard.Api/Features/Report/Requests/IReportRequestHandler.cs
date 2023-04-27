using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gameboard.Api.Features.Reports;

internal interface IReportRequestHandler<TParams, TEntity, TResults>
{
    IQueryable<TEntity> BuildQuery(TParams reportParams);
    Task<TResults> TransformQueryToResults(IQueryable<TEntity> query);
}
