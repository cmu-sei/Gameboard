using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Structure;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public interface IReportsService
{
    Task<IEnumerable<ReportViewModel>> List();
    Task<IEnumerable<SimpleEntity>> ListParameterOptionsChallengeSpecs(string gameId = null);
    Task<IEnumerable<string>> ListParameterOptionsCompetitions();
    Task<IEnumerable<SimpleEntity>> ListParameterOptionsGames();
    Task<IEnumerable<string>> ListParameterOptionsTracks();
}

public class ReportsService : IReportsService
{
    private readonly IChallengeSpecStore _challengeSpecStore;
    private readonly IMapper _mapper;
    private readonly IGameStore _gameStore;
    private readonly IReportStore _store;

    public ReportsService
    (
        IChallengeSpecStore challengeSpecStore,
        IGameStore gameStore,
        IMapper mapper,
        IReportStore store
    )
    {
        _challengeSpecStore = challengeSpecStore;
        _gameStore = gameStore;
        _mapper = mapper;
        _store = store;
    }

    public async Task<IEnumerable<ReportViewModel>> List()
        => await _mapper.ProjectTo<ReportViewModel>(_store.List()).ToArrayAsync();

    public async Task<IEnumerable<SimpleEntity>> ListParameterOptionsChallengeSpecs(string gameId)
    {
        var query = _challengeSpecStore.ListWithNoTracking();

        if (gameId.NotEmpty())
            query = query.Where(c => c.GameId == gameId);

        return await query.Select(c => new SimpleEntity { Id = c.Id, Name = c.Name }).ToArrayAsync();
    }

    public async Task<IEnumerable<string>> ListParameterOptionsCompetitions()
        => await _store.GetCompetitions();

    public async Task<IEnumerable<SimpleEntity>> ListParameterOptionsGames()
        => await _gameStore
            .ListWithNoTracking()
            .Select(g => new SimpleEntity { Id = g.Id, Name = g.Name })
            .ToArrayAsync();

    public async Task<IEnumerable<string>> ListParameterOptionsTracks()
        => await _store.GetTracks();
}
