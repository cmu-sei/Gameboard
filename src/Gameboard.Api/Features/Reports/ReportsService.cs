// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Practice;
using Gameboard.Api.Features.Teams;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public interface IReportsService
{
    ReportResults<TRecord> BuildResults<TRecord>(ReportRawResults<TRecord> rawResults);
    ReportResults<TOverallStats, TRecord> BuildResults<TOverallStats, TRecord>(ReportRawResults<TOverallStats, TRecord> rawResults);
    Task<string> GetDescription(string key);
    Task<IDictionary<string, ReportTeamViewModel>> GetTeamsByPlayerIds(IEnumerable<string> playerIds, CancellationToken cancellationToken);
    Task<IEnumerable<ReportViewModel>> List();
    Task<IEnumerable<SimpleEntity>> ListChallengeSpecs(string gameId = null);
    Task<string[]> ListChallengeTags(CancellationToken cancellationToken);
    Task<IEnumerable<SimpleEntity>> ListGames();
    Task<IEnumerable<string>> ListSeasons();
    Task<IEnumerable<string>> ListSeries();
    Task<IEnumerable<ReportSponsorViewModel>> ListSponsors();
    Task<IEnumerable<string>> ListTracks();
    Task<IEnumerable<string>> ListTicketStatuses();
    IEnumerable<string> ParseMultiSelectCriteria(string criteria);
}

public class ReportsService
(
    INowService now,
    IPagingService paging,
    IPracticeService practiceService,
    IStore store,
    ITeamService teamService
) : IReportsService
{
    private static readonly string MULTI_SELECT_DELIMITER = ",";

    private readonly INowService _now = now;
    private readonly IPagingService _paging = paging;
    private readonly IPracticeService _practiceService = practiceService;
    private readonly IStore _store = store;
    private readonly ITeamService _teamService = teamService;

    public async Task<string> GetDescription(string key)
    {
        var reports = await List();
        return reports.SingleOrDefault(r => r.Key == key)?.Description;
    }

    public Task<IEnumerable<ReportViewModel>> List()
    {
        var reports = new ReportViewModel[]
        {
            new()
            {
                Name = "Challenges",
                Key = ReportKey.Challenges,
                Description = "View a summary about the challenges running on Gameboard. See summaries describing scoring and popularity.",
                ExampleFields =
                [
                    "Challenge Name",
                    "Game",
                    "Season",
                    "Series",
                    "Track",
                    "Scoring Distribution",
                    "Unique players"
                ],
                ExampleParameters =
                [
                    "Season",
                    "Series",
                    "Track",
                    "Game"
                ]
            },
            new()
            {
                Name = "Enrollment",
                Key = ReportKey.Enrollment,
                Description = "View a summary of player enrollment - who enrolled when, which sponsors they represent, and how many of them actually played challenges.",
                ExampleFields =
                [
                    "Player & Sponsor",
                    "Games Enrolled",
                    "Challenge Performance",
                ],
                ExampleParameters =
                [
                    "Season",
                    "Series",
                    "Track",
                    "Sponsor",
                    "Game",
                    "Enrollment Date Range",
                ]
            },
            new()
            {
                Name = "Feedback",
                Key = ReportKey.Feedback,
                Description = "See feedback from your players grouped by template. Compare how your games and challenges are being received and get inside the minds of your players",
                ExampleFields =
                [
                    "Responses",
                    "Aggregated Stats"
                ],
                ExampleParameters =
                [
                    "Feedback Template",
                    "Season",
                    "Series",
                    "Track",
                    "Sponsor",
                    "Game",
                    "Submission Date"
                ]
            },
            new()
            {
                Name = "Feedback (Legacy)",
                Key = ReportKey.FeedbackLegacy,
                Description = "Learn more about how your games are landing with your players. (Requires configuration of feedback in the Game Center. For games played before the existence of feedback templates.)",
                ExampleFields =
                [
                    "Question Info",
                    "Response Summary",
                    "Individual Responses"
                ],
                ExampleParameters =
                [
                    "Game",
                    "Sponsor"
                ]
            },
            new()
            {
                Name = "Players",
                Key = ReportKey.Players,
                Description = "View a summary of your players. See their basic information, their sponsors, and how many challenges they're playing.",
                ExampleFields =
                [
                    "Player & Sponsor",
                    "Challenges Deployed",
                    "Distinct Competitions Played"
                ],
                ExampleParameters =
                [
                    "Season",
                    "Series",
                    "Track",
                    "Game",
                    "Account Created",
                    "Last Played",
                    "Sponsor"
                ]
            },
            new()
            {
                Name = "Practice Area",
                Key = ReportKey.PracticeArea,
                Description = "Check in on players who are spending free time honing their skills on Gameboard. See which challenges are practiced most, success rates, and which players are logging in to practice.",
                ExampleFields =
                [
                    "Challenge Performance",
                    "Player Performance",
                    "Scoring",
                    "Trends",
                    "Competitive vs. Practice"
                ],
                ExampleParameters =
                [
                    "Practice Date",
                    "Series",
                    "Track",
                    "Season",
                    "Game",
                    "Sponsor"
                ]
            },
            new()
            {
                Name = "Site Usage",
                Key = ReportKey.SiteUsage,
                Description = "View a high-level overview of how the app is being used, optionally filtered by date range and user sponsor.",
                IsExportable = false,
                ExampleFields =
                [
                    "Avg. Completed Challenges Per Player",
                    "Competitive vs. Practice",
                    "Deployed Challenge Count",
                    "Unique Sponsors"
                ],
                ExampleParameters =
                [
                    "Activity Start / End Dates",
                    "Sponsor"
                ]
            },
            new()
            {
                Name = "Support",
                Key = ReportKey.Support,
                Description = "View a summary of the support tickets that have been created in Gameboard, including closer looks at submission times, ticket categories, and associated challenges.",
                ExampleFields =
                [
                    "Summary Info",
                    "Status",
                    "Label",
                    "Challenge",
                    "Time Windows",
                    "Assignment Info"
                ],
                ExampleParameters =
                [
                    "Status & Label",
                    "Game & Challenge",
                    "Creation Date",
                    "Time Since Opened / Updated",
                ]
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
        var teamPlayers = await _store
            .WithNoTracking<Data.Player>()
                .Include(p => p.Sponsor)
            .Where(p => playerIds.Contains(p.Id))
            .ToArrayAsync(cancellationToken);

        var teamDict = new Dictionary<string, ReportTeamViewModel>();
        foreach (var team in teamPlayers.GroupBy(p => p.TeamId))
        {
            var captain = _teamService.ResolveCaptain(team.ToList());

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
        var query = _store.WithNoTracking<Data.ChallengeSpec>();

        if (gameId.NotEmpty())
            query = query.Where(c => c.GameId == gameId);

        return await query.Select(c => new SimpleEntity { Id = c.Id, Name = c.Name })
            .Distinct()
            .OrderBy(s => s.Name)
            .ToArrayAsync();
    }

    public Task<string[]> ListChallengeTags(CancellationToken cancellationToken)
        => _practiceService.GetVisibleChallengeTags(cancellationToken);

    public Task<IEnumerable<string>> ListSeasons()
        => GetGameStringPropertyOptions(g => g.Season);

    public Task<IEnumerable<string>> ListSeries()
        => GetGameStringPropertyOptions(g => g.Division);

    public async Task<IEnumerable<SimpleEntity>> ListGames()
        => await _store.WithNoTracking<Data.Game>()
            .Select(g => new SimpleEntity { Id = g.Id, Name = g.Name })
            .OrderBy(g => g.Name)
            .ToArrayAsync();

    public async Task<IEnumerable<ReportSponsorViewModel>> ListSponsors()
        => await _store.WithNoTracking<Data.Sponsor>()
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
        return await _store.WithNoTracking<Data.Ticket>()
            .Select(t => t.Status)
            .Distinct()
            .OrderBy(t => t)
            .ToArrayAsync();
    }

    public IEnumerable<string> ParseMultiSelectCriteria(string criteria)
    {
        if (criteria.IsEmpty())
            return [];

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
                Key = rawResults.Key,
                Description = rawResults.Description,
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
                Key = rawResults.Key,
                Description = rawResults.Description,
                ParametersSummary = null,
                RunAt = _now.Get()
            },
            OverallStats = rawResults.OverallStats,
            Paging = pagedResults.Paging,
            Records = pagedResults.Items
        };
    }

    private async Task<IEnumerable<string>> GetGameStringPropertyOptions(Expression<Func<Data.Game, string>> property)
        =>
        (
            await _store
                .WithNoTracking<Data.Game>()
                .Select(property)
                .Distinct()
                // catch as many blanks as we can here, but have to use
                // client side eval to distinguish long blanks
                .Where(s => s != null && s != string.Empty)
                .OrderBy(s => s)
                .ToArrayAsync()
        ).Where(s => s.NotEmpty());
}
