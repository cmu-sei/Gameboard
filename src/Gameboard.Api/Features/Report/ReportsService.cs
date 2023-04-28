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
    IQueryable<Data.Player> GetPlayersReportBaseQuery(PlayersReportQueryParameters parameters);
}

public class ReportsService : IReportsService
{
    private readonly IChallengeSpecStore _challengeSpecStore;
    private readonly IMapper _mapper;
    private readonly IGameStore _gameStore;
    private readonly IPlayerStore _playerStore;
    private readonly IReportStore _store;

    public ReportsService
    (
        IChallengeSpecStore challengeSpecStore,
        IGameStore gameStore,
        IMapper mapper,
        IPlayerStore playerStore,
        IReportStore store
    )
    {
        _challengeSpecStore = challengeSpecStore;
        _gameStore = gameStore;
        _mapper = mapper;
        _playerStore = playerStore;
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

    public IQueryable<Data.Player> GetPlayersReportBaseQuery(PlayersReportQueryParameters parameters)
    {
        var baseQuery = _playerStore
            .ListWithNoTracking()
            .Include(p => p.Game)
            .Include(p => p.Challenges)
            .Include(p => p.User)
            .Where(p => p.Game.PlayerMode == PlayerMode.Competition)
            .AsQueryable();

        if (parameters.SessionStartWindow?.DateStart != null)
        {
            baseQuery = baseQuery.Where(p => p.SessionBegin >= parameters.SessionStartWindow.DateStart);
        }

        if (parameters.SessionStartWindow?.DateEnd != null)
        {
            baseQuery = baseQuery.Where(p => p.SessionBegin >= parameters.SessionStartWindow.DateEnd);
        }

        if (parameters.Competition.NotEmpty())
        {
            baseQuery = baseQuery
                .Where(p => p.Game.Competition == parameters.Competition);
        }

        if (parameters.Track.NotEmpty())
        {
            baseQuery = baseQuery
                .Where(p => p.Game.Track == parameters.Track);
        }

        if (parameters.ChallengeSpecId.NotEmpty())
        {
            baseQuery = baseQuery
                .Include(p => p.Challenges.Where(c => c.SpecId == parameters.ChallengeSpecId));
        }

        if (parameters.GameId.NotEmpty())
            baseQuery = baseQuery
                .Where(p => p.GameId == parameters.GameId);

        return baseQuery;
    }
}
