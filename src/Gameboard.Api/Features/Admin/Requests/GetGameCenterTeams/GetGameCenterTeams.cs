using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
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
            .Where(p => p.Role == PlayerRole.Manager)
            .Where(p => p.GameId == request.GameId);

        if (request.Args.HasScored is not null)
            query = query.Where(p => (p.Score > 0) == request.Args.HasScored.Value);

        if (request.Args.PlayerMode is not null)
            query = query.Where(p => p.Mode == request.Args.PlayerMode);

        if (request.Args.Status is not null)
        {
            switch (request.Args.Status)
            {
                case GameCenterTeamsStatus.Complete:
                    query = query
                        .WhereDateIsNotEmpty(p => p.SessionEnd)
                        .Where(p => p.SessionEnd < nowish);
                    break;
                case GameCenterTeamsStatus.Playing:
                    query = query
                        .WhereDateIsNotEmpty(p => p.SessionBegin)
                        .Where(p => p.SessionBegin <= nowish)
                        .Where(p => p.SessionEnd > nowish);
                    break;
                case GameCenterTeamsStatus.NotStarted:
                    query = query
                        .WhereDateIsEmpty(p => p.SessionBegin);
                    break;
            }
        }

        if (request.Args.Search.IsNotEmpty() && request.Args.Search.Length > 2)
        {
            query = query
                .Where
                (
                    p =>
                        // guid matches do startswith for speed
                        p.TeamId.StartsWith(request.Args.Search) ||
                        p.UserId.StartsWith(request.Args.Search) ||
                        p.Id.StartsWith(request.Args.Search) ||
                        p.Challenges.Any(c => c.Id.StartsWith(request.Args.Search)) ||

                        // name matches are looser but will take longer
                        p.Sponsor.Name.Contains(request.Args.Search) ||
                        p.ApprovedName.Contains(request.Args.Search) ||
                        p.User.ApprovedName.Contains(request.Args.Search)
                );
        }

        var matchingTeams = await query
            .Select(p => new
            {
                Name = p.ApprovedName,
                p.TeamId,
                SessionBeginButts = p.SessionBegin,
                SessionEndButts = p.SessionEnd,
                SessionBegin = p.SessionBegin == DateTimeOffset.MinValue ? default(DateTimeOffset?) : p.SessionBegin,
                SessionEnd = p.SessionEnd == DateTimeOffset.MinValue ? default(DateTimeOffset?) : p.SessionEnd,
                TimeRemaining = nowish < p.SessionEnd ? (p.SessionEnd - nowish).TotalMilliseconds : default(double?),
                TimeSinceStart = nowish > p.SessionBegin && nowish < p.SessionEnd ? (nowish - p.SessionBegin).TotalMilliseconds : default(double?)
            })
            .Distinct()
            .ToDictionaryAsync(t => t.TeamId, t => t, cancellationToken);

        // we'll need this data no matter what, and if we get it here, we can
        // use it to do sorting stuff
        var teamRanks = await _store
            .WithNoTracking<DenormalizedTeamScore>()
            .Where(t => t.GameId == request.GameId)
            .Where(s => matchingTeams.Keys.Contains(s.TeamId))
            .Select(t => new { t.TeamId, t.Rank, t.ScoreOverall })
            .GroupBy(t => t.TeamId)
            .ToDictionaryAsync(gr => gr.Key, gr => gr.Single(), cancellationToken);

        // default sort is pretty much nonsense (todo)
        var sortedTeamIds = matchingTeams.Keys.ToArray();
        if (request.Args.Sort is not null)
        {
            switch (request.Args.Sort)
            {
                case GetGameCenterTeamsSort.Rank:
                    {
                        sortedTeamIds = teamRanks.Keys.Sort(k => teamRanks[k].Rank).ToArray();
                        break;
                    }
                case GetGameCenterTeamsSort.TimeRemaining:
                    {
                        sortedTeamIds = matchingTeams
                            .Sort(t => t.Value.TimeRemaining, request.Args.SortDirection)
                            .Select(t => t.Value.TeamId)
                            .ToArray();

                        break;
                    }
                case GetGameCenterTeamsSort.TimeSinceStart:
                    {
                        sortedTeamIds = matchingTeams
                            .Sort(t => t.Value.TimeSinceStart, request.Args.SortDirection)
                            .Select(t => t.Value.TeamId)
                            .ToArray();

                        break;
                    }
                // default name sort
                default:
                    {
                        sortedTeamIds = matchingTeams
                            .Sort(t => t.Value.Name, request.Args.SortDirection)
                            .Select(t => t.Key)
                            .ToArray();

                        break;
                    }
            }
        }

        // these are the teamIds we need full info for, so pull all players on these teams
        var paged = _pagingService.Page(sortedTeamIds, request.PagingArgs);
        var pagedTeamIds = paged.Items;

        var teamPlayers = await query
            .Select(p => new
            {
                p.Id,
                p.ApprovedName,
                p.Mode,
                p.IsReady,
                p.Role,
                p.SessionBegin,
                p.SessionEnd,
                p.WhenCreated,
                p.TeamId,
                Sponsor = new
                {
                    p.Sponsor.Id,
                    p.Sponsor.Name,
                    p.Sponsor.Logo
                }
            })
            .Where(p => pagedTeamIds.Contains(p.TeamId))
            .GroupBy(p => p.TeamId)
            .ToDictionaryAsync(gr => gr.Key, gr => gr.ToArray(), cancellationToken);

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

        return new GameCenterTeamsResults
        {
            Teams = new PagedEnumerable<GameCenterTeamsResultsTeam>
            {
                Paging = paged.Paging,
                Items = pagedTeamIds.Select(tId =>
                {
                    var players = teamPlayers[tId].Where(p => p.Role != PlayerRole.Member);
                    var captain = teamPlayers[tId].Single(p => p.Role == PlayerRole.Manager);
                    var solves = teamSolves[tId];

                    return new GameCenterTeamsResultsTeam
                    {
                        Id = tId,
                        Name = captain.ApprovedName,
                        IsExtended = gameSessionMinutes < (captain.SessionEnd - captain.SessionBegin).TotalMinutes,
                        Captain = new GameCenterTeamsPlayer
                        {
                            Id = captain.Id,
                            Name = captain.ApprovedName,
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
                            Name = p.ApprovedName,
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
                        RegisteredOn = captain.WhenCreated,
                        Session = new GameCenterTeamsSession
                        {
                            Start = matchingTeams[tId].SessionBegin.HasValue ? matchingTeams[tId].SessionBegin.Value.ToUnixTimeMilliseconds() : default(long?),
                            End = matchingTeams[tId].SessionEnd.HasValue ? matchingTeams[tId].SessionEnd.Value.ToUnixTimeMilliseconds() : default(long?),
                            TimeRemainingMs = matchingTeams[tId].TimeRemaining,
                            TimeSinceStartMs = matchingTeams[tId].TimeSinceStart
                        },
                        TicketCount = ticketCounts.TryGetValue(tId, out int value) ? value : 0
                    };
                })
            }
        };
    }
}
