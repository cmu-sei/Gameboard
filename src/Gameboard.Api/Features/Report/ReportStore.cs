using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public interface IReportStore
{
    Task<IEnumerable<string>> GetCompetitions();
    Task<IEnumerable<string>> GetTracks();
    IQueryable<Report> List();
}

internal class ReportStore : IReportStore
{
    private readonly GameboardDbContext _dbContext;
    private readonly IGameStore _gameStore;

    public ReportStore(IGameStore gameStore, GameboardDbContext dbContext)
    {
        _dbContext = dbContext;
        _gameStore = gameStore;
    }

    public async Task<IEnumerable<string>> GetCompetitions()
    {
        return await this._gameStore
            .List()
            .AsNoTracking()
            .Select(g => g.Competition)
            .Distinct()
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .ToArrayAsync();
    }

    public IQueryable<Report> List()
    {
        return _dbContext
            .Reports
            .AsNoTracking()
            .AsQueryable()
            .OrderBy(r => r.Name);
    }

    public async Task<IEnumerable<string>> GetTracks()
    {
        return await this._gameStore
            .List()
            .AsNoTracking()
            .Select(g => g.Track)
            .Distinct()
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToArrayAsync();
    }
}
