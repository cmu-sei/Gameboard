using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Features.Api.Reports;

public interface IReportStore
{
    Task<IEnumerable<string>> GetCompetitions();
    Task<IEnumerable<Report>> GetReports();
    Task<IEnumerable<string>> GetTracks();
}

internal class ReportStore : IReportStore
{
    private readonly IGameStore _gameStore;

    public ReportStore(IGameStore gameStore)
    {
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

    public Task<IEnumerable<Report>> GetReports()
    {
        throw new NotImplementedException();
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
