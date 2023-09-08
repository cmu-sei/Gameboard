using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public interface IReportsService
{
    ReportResults<TRecord> BuildResults<TRecord>(ReportRawResults<TRecord> rawResults);
    ReportResults<TOverallStats, TRecord> BuildResults<TOverallStats, TRecord>(ReportRawResults<TOverallStats, TRecord> rawResults);
    Task<IEnumerable<ChallengesReportRecord>> GetChallengesReportRecords(GetChallengesReportQueryArgs parameters);
    Task<IDictionary<string, ReportTeamViewModel>> GetTeamsByPlayerIds(IEnumerable<string> playerIds, CancellationToken cancellationToken);
    Task<IEnumerable<ReportViewModel>> List();
    Task<IEnumerable<SimpleEntity>> ListChallengeSpecs(string gameId = null);
    Task<IEnumerable<SimpleEntity>> ListGames();
    Task<IEnumerable<string>> ListSeasons();
    Task<IEnumerable<string>> ListSeries();
    Task<IEnumerable<ReportSponsorViewModel>> ListSponsors();
    Task<IEnumerable<string>> ListTracks();
    Task<IEnumerable<string>> ListTicketStatuses();
    IEnumerable<string> ParseMultiSelectCriteria(string criteria);
}

public class ReportsService : IReportsService
{
    private static readonly string MULTI_SELECT_DELIMITER = ",";
    public static readonly PagingArgs DEFAULT_PAGING = new()
    {
        PageNumber = 0,
        PageSize = 20,
    };

    private readonly IMapper _mapper;
    private readonly INowService _now;
    private readonly IPagingService _paging;
    private readonly IStore _store;
    private readonly ITeamService _teamService;

    public ReportsService
    (
        IMapper mapper,
        INowService now,
        IPagingService paging,
        IStore store,
        ITeamService teamService
    )
    {
        _mapper = mapper;
        _now = now;
        _paging = paging;
        _store = store;
        _teamService = teamService;
    }

    public Task<IEnumerable<ReportViewModel>> List()
    {
        var reports = new ReportViewModel[]
        {
            new() {
                Name = "Enrollment",
                Key = ReportKey.Enrollment,
                Description = "View a summary of player enrollment - who enrolled when, which sponsors do they represent, and how many of them actually played challenges.",
                ExampleFields = new string[]
                {
                    "Player & Sponsor",
                    "Games Enrolled",
                    "Challenge Performance",
                },
                ExampleParameters = new string[]
                {
                    "Season",
                    "Series",
                    "Sponsor",
                    "Track",
                    "Game",
                    "Enrollment Date Range",
                }
            },
            new() {
                Name = "Practice Area",
                Key = ReportKey.PracticeArea,
                Description = "Check in on players who are spending free time honing their skills on Gameboard. See which challenges are practiced most, success rates, and which players are logging in to practice.",
                ExampleFields = new string[]
                {
                    "Challenge Performance",
                    "Player Performance",
                    "Scoring",
                    "Trends",
                    "Practice vs. Competitive"
                },
                ExampleParameters = new string[]
                {
                    "Practice Date",
                    "Series",
                    "Track",
                    "Season",
                    "Game",
                    "Sponsor"
                }
            },
            new() {
                Name = "Support",
                Key = ReportKey.Support,
                Description = "View a summary of the support tickets that have been created in Gameboard, including closer looks at submission times, ticket categories, and associated challenges.",
                ExampleFields = new string[]
                {
                    "Summary Info",
                    "Status",
                    "Label",
                    "Challenge",
                    "Time Windows",
                    "Assignment Info"
                },
                ExampleParameters = new string[]
                {
                    "Status & Label",
                    "Game & Challenge",
                    "Creation Date",
                    "Time Since Opened / Updated",
                }
            },
        };

        return Task.FromResult<IEnumerable<ReportViewModel>>(reports);
    }

    /// <summary>
    /// Given a list of player Ids, return a dictionary of the teams that those players are assigned to.
    /// 
    /// Note that the string key of the dictionary is the player's teamId, NOT their playerId.
    /// </summary>
    /// <param name="playerIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IDictionary<string, ReportTeamViewModel>> GetTeamsByPlayerIds(IEnumerable<string> playerIds, CancellationToken cancellationToken)
    {
        var sponsors = await _store
            .List<Data.Sponsor>()
            .Select(s => new ReportSponsorViewModel
            {
                Id = s.Id,
                Name = s.Name,
                LogoFileName = s.Logo
            })
            .ToDictionaryAsync(s => s.LogoFileName, s => s, cancellationToken);

        var teamPlayers = await _store
            .List<Data.Player>()
                .Include(p => p.Sponsor)
            .Where(p => playerIds.Contains(p.Id))
            .ToArrayAsync(cancellationToken);

        // note that none of this actually runs back to the DB, but this was difficult
        // to do with typical projection because ResolveCaptain is async to handle
        // a signature that accepts a teamId
        var teamDict = new Dictionary<string, ReportTeamViewModel>();
        foreach (var team in teamPlayers.GroupBy(p => p.TeamId))
        {
            var captain = await _teamService.ResolveCaptain(team.ToList());

            teamDict.Add(team.Key, new ReportTeamViewModel
            {
                Id = captain.TeamId,
                Name = captain.ApprovedName,
                Captain = new SimpleEntity { Id = captain.Id, Name = captain.Name },
                Players = team.Select(p => new SimpleEntity { Id = p.Id, Name = p.ApprovedName }),
                Sponsors = team
                    .OrderBy(p => p.IsManager ? 0 : 1)
                    .Select(p => p.Sponsor is null ? null : new ReportSponsorViewModel
                    {
                        Id = p.Sponsor.Id,
                        Name = p.Sponsor.Name,
                        LogoFileName = p.Sponsor.Logo
                    })
            });
        }

        return teamDict;
    }

    public async Task<IEnumerable<SimpleEntity>> ListChallengeSpecs(string gameId)
    {
        var query = _store.List<Data.ChallengeSpec>();

        if (gameId.NotEmpty())
            query = query.Where(c => c.GameId == gameId);

        return await query.Select(c => new SimpleEntity { Id = c.Id, Name = c.Name })
            .OrderBy(s => s.Name)
            .ToArrayAsync();
    }

    public Task<IEnumerable<string>> ListSeasons()
        => GetGameStringPropertyOptions(g => g.Season);

    public async Task<IEnumerable<string>> ListSeries()
        =>
        (
            await _store
                .List<Data.Game>()
                .Select(g => g.Competition)
                .Distinct()
                .Where(c => c != null && c != "")
                .ToArrayAsync()
        ).Where(s => s.NotEmpty());

    public async Task<IEnumerable<SimpleEntity>> ListGames()
        => await _store.List<Data.Game>()
            .Select(g => new SimpleEntity { Id = g.Id, Name = g.Name })
            .ToArrayAsync();

    public async Task<IEnumerable<ReportSponsorViewModel>> ListSponsors()
        => await _store.List<Data.Sponsor>()
            .Select(s => new ReportSponsorViewModel
            {
                Id = s.Id,
                Name = s.Name,
                LogoFileName = s.Logo
            })
            .OrderBy(s => s.Name)
            .ToArrayAsync();

    public Task<IEnumerable<string>> ListTracks()
        => GetGameStringPropertyOptions(g => g.Track);

    public async Task<IEnumerable<string>> ListTicketStatuses()
    {
        return await _store.List<Data.Ticket>()
            .Select(t => t.Status)
            .Distinct()
            .ToArrayAsync();
    }

    public IEnumerable<string> ParseMultiSelectCriteria(string criteria)
    {
        if (criteria.IsEmpty())
            return Array.Empty<string>();

        return criteria
            .ToLower()
            .Split(MULTI_SELECT_DELIMITER, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public ReportResults<TRecord> BuildResults<TRecord>(ReportRawResults<TRecord> rawResults)
    {
        var pagedResults = _paging.Page(rawResults.Records, rawResults.PagingArgs);

        return new ReportResults<TRecord>
        {
            MetaData = new ReportMetaData
            {
                Title = rawResults.Title,
                Key = rawResults.ReportKey,
                ParametersSummary = null,
                RunAt = _now.Get()
            },
            Paging = pagedResults.Paging,
            Records = pagedResults.Items
        };
    }

    public ReportResults<TOverallStats, TRecord> BuildResults<TOverallStats, TRecord>(ReportRawResults<TOverallStats, TRecord> rawResults)
    {
        var pagedResults = _paging.Page(rawResults.Records, rawResults.PagingArgs);

        return new ReportResults<TOverallStats, TRecord>
        {
            MetaData = new ReportMetaData
            {
                Title = rawResults.Title,
                Key = rawResults.ReportKey,
                ParametersSummary = null,
                RunAt = _now.Get()
            },
            OverallStats = rawResults.OverallStats,
            Paging = pagedResults.Paging,
            Records = pagedResults.Items
        };
    }

    public async Task<IEnumerable<ChallengesReportRecord>> GetChallengesReportRecords(GetChallengesReportQueryArgs args)
    {
        // TODO: validation
        var hasCompetition = args.Competition.NotEmpty();
        var hasGameId = args.GameId.NotEmpty();
        var hasSpecId = args.ChallengeSpecId.NotEmpty();
        var hasTrack = args.TrackName.NotEmpty();

        // parameters resolve to challenge specs
        var specs = await _store.List<Data.ChallengeSpec>()
            .Include(s => s.Game)
            .Where(s => !hasCompetition || s.Game.Competition == args.Competition)
            .Where(s => !hasGameId || s.GameId == args.GameId)
            .Where(s => !hasTrack || s.Game.Track == args.TrackName)
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

        // this separate because players may be registered but not deploy this challenge, and we need to know that for reporting
        var allPlayers = await _store.List<Data.Player>()
            .Where(p => gameIds.Contains(p.GameId))
            .ToArrayAsync();

        var challenges = await _store.List<Data.Challenge>()
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

                    var meanCompleteSolveTime = playersCompleteSolved.Any() ? playersCompleteSolved.Average(p => p.Player.SolveTimeMs.Value) : null as double?;
                    var meanScore = c.Any() ? c.Average(c => c.Player.Score) : null as double?;

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
                c.SpecId,
                c.Player.Player,
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
        var hasChallenges = challengesBySpec.ContainsKey(spec.Id);
        var challenge = hasChallenges ? challengesBySpec[spec.Id].Where(s => s.Game.Id == spec.Game.Id).FirstOrDefault() : null;
        var hasSolves = fastestSolves.ContainsKey(spec.Id);
        var hasStats = meanStats.ContainsKey(spec.Id);

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

    private async Task<IEnumerable<string>> GetGameStringPropertyOptions(Expression<Func<Data.Game, string>> property)
        =>
        (
            await _store
                .List<Data.Game>()
                .Select(property)
                .Distinct()
                // catch as many blanks as we can here, but have to use
                // client side eval to distinguish long blanks
                .Where(s => s != null && s != string.Empty)
                .OrderBy(s => s)
                .ToArrayAsync()
        ).Where(s => s.NotEmpty());
}
