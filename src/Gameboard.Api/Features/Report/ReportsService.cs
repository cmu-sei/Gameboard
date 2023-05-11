using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Services;
using Gameboard.Api.Structure;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public interface IReportsService
{
    Task<IEnumerable<ChallengesReportRecord>> GetChallengesReportRecords(GetChallengesReportQueryArgs parameters);
    IQueryable<Data.Player> GetPlayersReportBaseQuery(PlayersReportQueryParameters parameters);
    Task<IEnumerable<ReportViewModel>> List();
    Task<IEnumerable<SimpleEntity>> ListParameterOptionsChallengeSpecs(string gameId = null);
    Task<IEnumerable<string>> ListParameterOptionsCompetitions();
    Task<IEnumerable<SimpleEntity>> ListParameterOptionsGames();
    Task<IEnumerable<string>> ListParameterOptionsTracks();
    Task<IEnumerable<string>> ListTicketStatuses();
}

public class ReportsService : IReportsService
{
    private readonly IChallengeStore _challengeStore;
    private readonly IChallengeSpecStore _challengeSpecStore;
    private readonly IMapper _mapper;
    private readonly IGameStore _gameStore;
    private readonly INowService _now;
    private readonly IPlayerStore _playerStore;
    private readonly IReportStore _store;
    private readonly ITicketStore _ticketStore;

    public ReportsService
    (
        IChallengeStore challengeStore,
        IChallengeSpecStore challengeSpecStore,
        IGameStore gameStore,
        IMapper mapper,
        INowService now,
        IPlayerStore playerStore,
        IReportStore store,
        ITicketStore ticketStore
    )
    {
        _challengeStore = challengeStore;
        _challengeSpecStore = challengeSpecStore;
        _gameStore = gameStore;
        _mapper = mapper;
        _now = now;
        _playerStore = playerStore;
        _store = store;
        _ticketStore = ticketStore;
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

    public async Task<IEnumerable<ChallengesReportRecord>> GetChallengesReportRecords(GetChallengesReportQueryArgs args)
    {
        // TODO: validation
        var hasCompetition = args.Competition.NotEmpty();
        var hasGameId = args.GameId.NotEmpty();
        var hasSpecId = args.ChallengeSpecId.NotEmpty();
        var hasTrack = args.Track.NotEmpty();

        // parameters resolve to challenge specs
        var specs = await _challengeSpecStore
            .ListWithNoTracking()
            .Include(s => s.Game)
            .Where(s => !hasCompetition || s.Game.Competition == args.Competition)
            .Where(s => !hasGameId || s.GameId == args.GameId)
            .Where(s => !hasTrack || s.Game.Track == args.Track)
            .Where(s => !hasSpecId || s.Id == args.ChallengeSpecId)
            .ToDictionaryAsync
            (
                s => s.Id,
                s => new ChallengesReportSpec
                {
                    Id = s.Id,
                    Game = new SimpleEntity { Id = s.GameId, Name = s.Game.Name },
                    Name = s.Name,
                    MaxPoints = s.Points
                }
            );

        var specIds = specs.Keys;
        var gameIds = specs.Values.Select(v => v.Game.Id).Distinct();

        // this separate because players may be registered by not deploy this challenge, and we need to know that for reporting
        var allPlayers = await _playerStore
            .ListWithNoTracking()
            .Where(p => gameIds.Contains(p.GameId))
            .ToArrayAsync();

        var challenges = await _challengeStore
            .List()
            .AsNoTracking()
            .Include(c => c.Game)
            .Include(c => c.Player)
            .Include(c => c.Tickets)
            .Where(c => specIds.Contains(c.SpecId))
            .Select(c => new ChallengesReportChallenge
            {
                Challenge = _mapper.Map<SimpleEntity>(c),
                Game = _mapper.Map<SimpleEntity>(c.Game),
                Player = new ChallengesReportPlayer
                {
                    Player = new SimpleEntity { Id = c.PlayerId, Name = c.Player.ApprovedName },
                    StartTime = c.Player.SessionBegin,
                    EndTime = c.Player.SessionEnd,
                    Result = c.Result,
                    SolveTimeMs =
                    (
                        c.StartTime == DateTimeOffset.MinValue || c.LastScoreTime == DateTimeOffset.MinValue ?
                        null :
                        (c.LastScoreTime - c.StartTime).TotalMilliseconds
                    ),
                    Score = c.Player.Score
                },
                SpecId = c.SpecId,
                TicketCount = c.Tickets.Count()
            }).ToArrayAsync();

        var challengesBySpec = challenges
            .GroupBy(c => c.SpecId)
            .ToDictionary(c => c.Key, c => c.ToList());

        // computed columns
        var meanStats = challenges
            .Where(c => c.Player.SolveTimeMs != null)
            .GroupBy(c => c.SpecId)
            .ToDictionary
            (
                c => c.Key,
                c =>
                {
                    var playersCompleteSolved = c.Where(p => p.Player.Result == ChallengeResult.Success);

                    var meanCompleteSolveTime = playersCompleteSolved.Count() > 0 ? playersCompleteSolved.Average(p => p.Player.SolveTimeMs.Value) : null as Nullable<double>;
                    var meanScore = c.Count() > 0 ? c.Average(c => c.Player.Score) : null as Nullable<double>;

                    return new ChallengesReportMeanChallengeStats
                    {
                        MeanCompleteSolveTimeMs = meanCompleteSolveTime,
                        MeanScore = meanScore
                    };
                }
            );

        var fastestSolves = challenges
            .Where(c => c.Player.SolveTimeMs != null)
            .Select(c => new
            {
                SpecId = c.SpecId,
                Player = c.Player.Player,
                SolveTime = c.Player.SolveTimeMs.Value
            })
            .GroupBy(specPlayerSolve => specPlayerSolve.SpecId)
            .ToDictionary(c => c.Key, c => c.Select(c => new ChallengesReportPlayerSolve
            {
                Player = c.Player,
                SolveTimeMs = c.SolveTime
            }).ToList().FirstOrDefault());

        return specs.Values.Select(spec => BuildRecord(spec, allPlayers, challengesBySpec, fastestSolves, meanStats));
    }

    private ChallengesReportRecord BuildRecord
    (
        ChallengesReportSpec spec,
        Data.Player[] allPlayers,
        Dictionary<string, List<ChallengesReportChallenge>> challengesBySpec,
        Dictionary<string, ChallengesReportPlayerSolve> fastestSolves,
        Dictionary<string, ChallengesReportMeanChallengeStats> meanStats
    )
    {
        var hasChallenges = challengesBySpec.Keys.Contains(spec.Id);
        var challenge = hasChallenges ? challengesBySpec[spec.Id].Where(s => s.Game.Id == spec.Game.Id).FirstOrDefault() : null;
        var hasSolves = fastestSolves.Keys.Contains(spec.Id);
        var hasStats = meanStats.Keys.Contains(spec.Id);

        return new ChallengesReportRecord
        {
            ChallengeSpec = new SimpleEntity { Id = spec.Id, Name = spec.Name },
            Game = new SimpleEntity { Id = spec.Game.Id, Name = spec.Game.Name },
            Challenge = challenge == null ? null : new SimpleEntity { Id = challenge.Challenge.Id, Name = challenge.Challenge.Name },
            PlayersEligible = allPlayers
                .Where(p => p.GameId == spec.Game.Id)
                .Count(),
            PlayersStarted = !hasChallenges ? 0 : challengesBySpec[spec.Id]
                .Where(c => c.Player.StartTime > DateTimeOffset.MinValue)
                .Count(),
            PlayersWithCompleteSolve = !hasChallenges ? 0 : challengesBySpec[spec.Id]
                .Where(p => p.Player.Result == ChallengeResult.Success)
                .Count(),
            PlayersWithPartialSolve = !hasChallenges ? 0 : challengesBySpec[spec.Id]
                .Where(p => p.Player.Result == ChallengeResult.Partial)
                .Count(),
            FastestSolve = !hasSolves ? null : fastestSolves[spec.Id],
            MaxPossibleScore = spec.MaxPoints,
            MeanCompleteSolveTimeMs = !hasStats ? null : meanStats[spec.Id].MeanCompleteSolveTimeMs,
            MeanScore = !hasStats ? null : meanStats[spec.Id].MeanScore,
            TicketCount = hasChallenges ? challengesBySpec[spec.Id].Select(c => c.TicketCount).Sum() : 0
        };
    }

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

        if (parameters.TrackName.NotEmpty())
        {
            baseQuery = baseQuery
                .Where(p => p.Game.Track == parameters.TrackName);
        }

        if (parameters.ChallengeSpecId.NotEmpty())
        {
            baseQuery = baseQuery
                .Include(p => p.Challenges.Where(c => c.SpecId == parameters.ChallengeSpecId));
        }

        if (parameters.GameId.NotEmpty())
            baseQuery = baseQuery
                .Where(p => p.GameId == parameters.GameId);

        if (parameters.TrackName.NotEmpty())
            if (parameters.TrackModifier == PlayersReportTrackModifier.CompetedInOnlyThisTrack)
            {
                //baseQuery = baseQuery.GroupBy(p => new { Id = p.Id, TrackName = p.Game.Track }).Where
            }

        return baseQuery;
    }

    public async Task<IEnumerable<string>> ListTicketStatuses()
    {
        return await _ticketStore
            .ListWithNoTracking()
            .Select(t => t.Status)
            .Distinct()
            .ToArrayAsync();
    }
}
