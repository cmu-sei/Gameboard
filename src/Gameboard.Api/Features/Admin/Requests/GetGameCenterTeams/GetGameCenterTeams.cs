using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Scores;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Admin;

public record GetGameCenterTeamsQuery(string GameId, GetGameCenterTeamsArgs Args, PagingArgs PagingArgs) : IRequest<GameCenterTeamsResults>;

internal class GetGameCenterTeamsHandler : IRequestHandler<GetGameCenterTeamsQuery, GameCenterTeamsResults>
{
    private readonly INowService _nowService;
    private readonly IPagingService _pagingService;
    private readonly IStore _store;
    private readonly ITeamService _teamService;
    private readonly TicketService _ticketService;

    public GetGameCenterTeamsHandler
    (
        INowService nowService,
        IPagingService pagingService,
        IStore store,
        ITeamService teamService,
        TicketService ticketService
    )
    {
        _nowService = nowService;
        _pagingService = pagingService;
        _store = store;
        _teamService = teamService;
        _ticketService = ticketService;
    }

    public async Task<GameCenterTeamsResults> Handle(GetGameCenterTeamsQuery request, CancellationToken cancellationToken)
    {
        var nowish = _nowService.Get();

        // a little blarghy because we're counting on the Role, not the whole captain resolution thing
        var query = _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.Mode == PlayerMode.Competition)
            .Where(p => p.GameId == request.GameId);

        if (request.Args.HasScored is not null)
            query = query.Where(p => (p.Score > 0) == request.Args.HasScored.Value);

        if (request.Args.Advancement is not null)
        {
            if (request.Args.Advancement == GetGameCenterTeamsAdvancementFilter.AdvancedToNextGame)
                query = query.Where(p => p.Advanced);
            else if (request.Args.Advancement == GetGameCenterTeamsAdvancementFilter.AdvancedFromPreviousGame)
                query = query.Where(p => p.AdvancedFromGameId != null);
        }

        if (request.Args.SearchTerm.IsNotEmpty() && request.Args.SearchTerm.Length > 2)
        {
            var searchTerm = request.Args.SearchTerm.ToLower();

            query = query
                .Where
                (
                    p =>
                        // guid matches do startswith for speed
                        p.TeamId.ToLower().StartsWith(request.Args.SearchTerm) ||
                        p.UserId.ToLower().StartsWith(request.Args.SearchTerm) ||
                        p.Id.ToLower().StartsWith(request.Args.SearchTerm) ||
                        p.Challenges.Any(c => c.Id.ToLower().StartsWith(request.Args.SearchTerm)) ||

                        // name matches are looser but will take longer
                        p.Sponsor.Name.ToLower().Contains(request.Args.SearchTerm) ||
                        p.ApprovedName.ToLower().Contains(request.Args.SearchTerm) ||
                        p.User.ApprovedName.ToLower().Contains(request.Args.SearchTerm)
                );
        }

        if (request.Args.SessionStatus is not null)
        {
            switch (request.Args.SessionStatus)
            {
                case GameCenterTeamsSessionStatus.NotStarted:
                    query = query.WhereDateIsEmpty(p => p.SessionBegin);
                    break;
                case GameCenterTeamsSessionStatus.Complete:
                    query = query.WhereDateIsNotEmpty(p => p.SessionEnd);
                    break;
                case GameCenterTeamsSessionStatus.Playing:
                    query = query
                        .WhereDateIsNotEmpty(p => p.SessionBegin)
                        .WhereDateIsNotEmpty(p => p.SessionEnd)
                        .Where(p => p.SessionBegin <= nowish)
                        .Where(p => p.SessionEnd >= nowish);
                    break;
            }
        }

        var matchingTeams = await query
            .Select(p => new
            {
                p.Id,
                Name = p.ApprovedName,
                PendingName = p.Name != p.ApprovedName ? p.Name : null,
                p.IsReady,
                p.Role,
                p.TeamId,
                Advancement = p.AdvancedFromGame == null ? null : new
                {
                    FromGame = new SimpleEntity { Id = p.AdvancedFromGameId, Name = p.AdvancedFromGame.Name },
                    FromTeam = new SimpleEntity { Id = p.AdvancedFromTeamId, Name = p.AdvancedFromPlayer.Name },
                    Score = p.AdvancedWithScore
                },
                IsActive = p.SessionBegin != DateTimeOffset.MinValue && p.SessionBegin < nowish && p.SessionEnd > nowish,
                SessionBegin = p.SessionBegin == DateTimeOffset.MinValue ? default(DateTimeOffset?) : p.SessionBegin,
                SessionEnd = p.SessionEnd == DateTimeOffset.MinValue ? default(DateTimeOffset?) : p.SessionEnd,
                Sponsor = new SimpleSponsor
                {
                    Id = p.SponsorId,
                    Name = p.Sponsor.Name,
                    Logo = p.Sponsor.Logo
                },
                TimeCumulative = p.Time,
                TimeRemaining = nowish < p.SessionEnd ? (p.SessionEnd - nowish).TotalMilliseconds : default(double?),
                TimeSinceStart = nowish > p.SessionBegin && nowish < p.SessionEnd ? (nowish - p.SessionBegin).TotalMilliseconds : default(double?),
                p.WhenCreated
            })
            .GroupBy(p => p.TeamId)
            // have to do pending names filter here because we need all players
            .ToDictionaryAsync(gr => gr.Key, gr => gr.ToArray(), cancellationToken);

        if (request.Args.HasPendingNames is not null)
        {
            matchingTeams = matchingTeams
                .Where(kv => request.Args.HasPendingNames.Value == kv.Value.Any(p => p.PendingName != null && p.PendingName != string.Empty && p.PendingName != p.Name))
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        var matchingTeamIds = matchingTeams.Keys.ToArray();
        var captains = matchingTeams.ToDictionary(kv => kv.Key, kv => kv.Value.Single(p => p.Role == PlayerRole.Manager));

        // we'll need this data no matter what, and if we get it here, we can
        // use it to do sorting stuff
        var teamRanks = await _store
            .WithNoTracking<DenormalizedTeamScore>()
            .Where(t => t.GameId == request.GameId)
            .Where(s => matchingTeamIds.Contains(s.TeamId))
            .Select(t => new { t.TeamId, Rank = t.Rank == 0 ? default(int?) : t.Rank, t.ScoreOverall })
            .GroupBy(t => t.TeamId)
            .ToDictionaryAsync(gr => gr.Key, gr => gr.Single(), cancellationToken);

        var sortedTeamIds = captains
            .OrderBy(kv => kv.Value.Name)
            .Select(kv => kv.Key)
            .ToArray();

        if (request.Args.Sort is not null)
        {
            switch (request.Args.Sort)
            {
                case GetGameCenterTeamsSort.Rank:
                    {
                        sortedTeamIds = sortedTeamIds
                            .Sort(k =>
                            {
                                if (!teamRanks.TryGetValue(k, out var rankData) || rankData.Rank is null)
                                    return int.MaxValue;

                                return rankData.Rank;
                            })
                            .ToArray();
                        break;
                    }
                case GetGameCenterTeamsSort.TimeRemaining:
                    {
                        sortedTeamIds = captains
                            .Sort(t => t.Value.SessionEnd - t.Value.SessionBegin, request.Args.SortDirection)
                            .Select(t => t.Value.TeamId)
                            .ToArray();
                        break;
                    }
                case GetGameCenterTeamsSort.TimeSinceStart:
                    {
                        sortedTeamIds = captains
                            .Sort(t => nowish - t.Value.SessionBegin, request.Args.SortDirection)
                            .Select(t => t.Value.TeamId)
                            .ToArray();

                        break;
                    }
            }
        }

        // compute global stats as efficiently as we can before we load data for the paged results
        var activeTeams = captains.Values
            .Where(t => t.IsActive)
            .Select(t => t.TeamId)
            .ToArray();

        var allPlayerStatuses = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => matchingTeamIds.Contains(p.TeamId))
            .Select(p => new { p.UserId, IsActive = activeTeams.Contains(p.TeamId) })
            .Distinct()
            .ToArrayAsync(cancellationToken);
        var activePlayerCount = allPlayerStatuses.Where(p => p.IsActive).Count();

        // these are the teamIds we need full info for, so pull more specific data for these teams
        var paged = _pagingService.Page(sortedTeamIds, request.PagingArgs);
        var pagedTeamIds = paged.Items;
        var scores = await _store
            .WithNoTracking<DenormalizedTeamScore>()
            .Where(s => pagedTeamIds.Contains(s.TeamId))
            .Select(s => new
            {
                s.TeamId,
                AdvancedScore = s.ScoreAdvanced,
                BonusScore = s.ScoreAutoBonus,
                CompletionScore = s.ScoreChallenge,
                ManualBonusScore = s.ScoreManualBonus,
                TotalScore = s.ScoreOverall,
            })
            .ToDictionaryAsync
            (
                s => s.TeamId,
                s => new Score
                {
                    AdvancedScore = s.AdvancedScore,
                    BonusScore = s.BonusScore,
                    CompletionScore = s.CompletionScore,
                    ManualBonusScore = s.ManualBonusScore,
                    TotalScore = s.TotalScore
                },
                cancellationToken
            );

        // pull other return data for the matching teams
        var ticketCounts = await _ticketService
            .GetTeamTickets(pagedTeamIds)
            .Where(t => t.Status != "Closed")
            .GroupBy(t => t.TeamId)
            .ToDictionaryAsync(gr => gr.Key, gr => gr.Count(), cancellationToken);

        // we also need to know the expected length of a session in the game to determine
        // if teams have been extended
        var gameSessionMinutes = await _store
            .WithNoTracking<Data.Game>()
            .Where(g => g.Id == request.GameId)
            .Select(g => g.SessionMinutes)
            .SingleAsync(cancellationToken);

        var teamSolves = await _teamService.GetSolves(pagedTeamIds, cancellationToken);

        // last, we check to see if the game has any pending approvals, as we need them for the screen
        var pendingNameCount = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.GameId == request.GameId)
            .Where(p => p.Name != null && p.Name != string.Empty && p.Name != p.ApprovedName)
            .CountAsync(cancellationToken);

        return new GameCenterTeamsResults
        {
            NamesPendingApproval = pendingNameCount,
            Teams = new PagedEnumerable<GameCenterTeamsResultsTeam>
            {
                Paging = paged.Paging,
                Items = pagedTeamIds.Select(tId =>
                {
                    var players = matchingTeams[tId];
                    var captain = captains[tId];
                    var solves = teamSolves[tId];

                    return new GameCenterTeamsResultsTeam
                    {
                        Id = tId,
                        Name = captain.Name,
                        IsExtended = captain.SessionEnd is not null && captain.SessionBegin is not null && (captain.SessionEnd.Value - captain.SessionBegin.Value).TotalMinutes > gameSessionMinutes,
                        Captain = new GameCenterTeamsPlayer
                        {
                            Id = captain.Id,
                            PendingName = captain.PendingName,
                            Name = captain.Name,
                            IsReady = captain.IsReady,
                            Sponsor = new SimpleSponsor
                            {
                                Id = captain.Sponsor.Id,
                                Name = captain.Sponsor.Name,
                                Logo = captain.Sponsor.Logo
                            }
                        },
                        Players = players.Select(p => new GameCenterTeamsPlayer
                        {
                            Id = p.Id,
                            PendingName = p.PendingName,
                            Name = p.Name,
                            IsReady = p.IsReady,
                            Sponsor = new SimpleSponsor
                            {
                                Id = p.Sponsor.Id,
                                Name = p.Sponsor.Name,
                                Logo = p.Sponsor.Logo
                            }
                        }),
                        ChallengesCompleteCount = solves.Complete,
                        ChallengesPartialCount = solves.Partial,
                        ChallengesRemainingCount = solves.Unscored,
                        IsReady = captain.IsReady && players.All(p => p.IsReady),
                        Rank = teamRanks.TryGetValue(tId, out var teamRankData) ? teamRankData.Rank : null,
                        RegisteredOn = captain.WhenCreated.ToUnixTimeMilliseconds(),
                        Score = scores.TryGetValue(tId, out var score) ? score : Score.Default,
                        Session = new GameCenterTeamsSession
                        {
                            Start = captains[tId].SessionBegin.HasValue ? captains[tId].SessionBegin.Value.ToUnixTimeMilliseconds() : default(long?),
                            End = captains[tId].SessionEnd.HasValue ? captains[tId].SessionEnd.Value.ToUnixTimeMilliseconds() : default(long?),
                            TimeCumulativeMs = captains[tId].TimeCumulative > 0 ? captains[tId].TimeCumulative : default(long?),
                            TimeRemainingMs = captains[tId].TimeRemaining,
                            TimeSinceStartMs = captains[tId].TimeSinceStart
                        },
                        TicketCount = ticketCounts.TryGetValue(tId, out int value) ? value : 0
                    };
                })
            }
        };
    }
}
