using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Services;
using Gameboard.Api.Structure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public class GetChallengeReportHandler : IRequestHandler<GetChallengeReportQuery, ChallengesReportResults>
{
    private readonly IChallengeStore _challengeStore;
    private readonly IChallengeSpecStore _challengeSpecStore;
    private readonly IGameStore _gameStore;
    private readonly IMapper _mapper;
    private readonly INowService _now;
    private readonly IPlayerStore _playerStore;

    public GetChallengeReportHandler
    (
        IChallengeStore challengeStore,
        IChallengeSpecStore challengeSpecStore,
        IGameStore gameStore,
        IMapper mapper,
        INowService now,
        IPlayerStore playerStore
    )
    {
        _challengeStore = challengeStore;
        _challengeSpecStore = challengeSpecStore;
        _gameStore = gameStore;
        _mapper = mapper;
        _now = now;
        _playerStore = playerStore;
    }

    public async Task<ChallengesReportResults> Handle(GetChallengeReportQuery request, CancellationToken cancellationToken)
    {
        // TODO: validation
        var hasCompetition = request.Args.Competition.NotEmpty();
        var hasGameId = request.Args.GameId.NotEmpty();
        var hasSpecId = request.Args.ChallengeSpecId.NotEmpty();
        var hasTrack = request.Args.Track.NotEmpty();

        // parameters resolve to challenge specs
        var specs = await _challengeSpecStore
            .ListWithNoTracking()
            .Include(s => s.Game)
            .Where(s => !hasCompetition || s.Game.Competition == request.Args.Competition)
            .Where(s => !hasGameId || s.GameId == request.Args.GameId)
            .Where(s => !hasTrack || s.Game.Track == request.Args.Track)
            .Where(s => !hasSpecId || s.Id == request.Args.ChallengeSpecId)
            .ToDictionaryAsync
            (
                s => s.Id,
                s => new ChallengeReportSpec
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
            .Where(c => specIds.Contains(c.SpecId))
            .Select(c => new ChallengeReportChallenge
            {
                Challenge = _mapper.Map<SimpleEntity>(c),
                Game = _mapper.Map<SimpleEntity>(c.Game),
                Player = new ChallengeReportPlayer
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

                    return new ChallengeReportMeanChallengeStats
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
            .ToDictionary(c => c.Key, c => c.Select(c => new ChallengeReportPlayerSolve
            {
                Player = c.Player,
                SolveTimeMs = c.SolveTime
            }).ToList().FirstOrDefault());

        return new ChallengesReportResults
        {
            MetaData = new ReportMetaData
            {
                Id = "tbd",
                Title = "Challenge Report",
                RunAt = _now.Get()
            },
            Records = specs.Values.Select(spec => BuildRecord(spec, allPlayers, challengesBySpec, fastestSolves, meanStats))
        };
    }

    private ChallengeReportRecord BuildRecord
    (
        ChallengeReportSpec spec,
        Data.Player[] allPlayers,
        Dictionary<string, List<ChallengeReportChallenge>> challengesBySpec,
        Dictionary<string, ChallengeReportPlayerSolve> fastestSolves,
        Dictionary<string, ChallengeReportMeanChallengeStats> meanStats
    )
    {
        var hasPlayers = challengesBySpec.Keys.Contains(spec.Id);
        var hasSolves = fastestSolves.Keys.Contains(spec.Id);
        var hasStats = meanStats.Keys.Contains(spec.Id);

        return new ChallengeReportRecord
        {
            ChallengeSpec = new SimpleEntity { Id = spec.Id, Name = spec.Name },
            Game = new SimpleEntity { Id = spec.Game.Id, Name = spec.Game.Name },
            PlayersEligible = allPlayers
                .Where(p => p.GameId == spec.Game.Id)
                .Count(),
            PlayersStarted = !hasPlayers ? 0 : challengesBySpec[spec.Id]
                .Where(c => c.Player.StartTime > DateTimeOffset.MinValue)
                .Count(),
            PlayersWithCompleteSolve = !hasPlayers ? 0 : challengesBySpec[spec.Id]
                .Where(p => p.Player.Result == ChallengeResult.Success)
                .Count(),
            PlayersWithPartialSolve = !hasPlayers ? 0 : challengesBySpec[spec.Id]
                .Where(p => p.Player.Result == ChallengeResult.Partial)
                .Count(),
            FastestSolve = !hasSolves ? null : fastestSolves[spec.Id],
            MaxPossibleScore = spec.MaxPoints,
            MeanCompleteSolveTimeMs = !hasStats ? null : meanStats[spec.Id].MeanCompleteSolveTimeMs,
            MeanScore = !hasStats ? null : meanStats[spec.Id].MeanScore
        };
    }
}
