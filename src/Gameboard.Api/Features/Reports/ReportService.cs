using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Feedback;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Services
{
    public class ReportServiceLegacy(
        ILogger<ReportServiceLegacy> logger,
        IMapper mapper,
        CoreOptions options,
        Defaults defaults,
        IStore store,
        TicketService ticketService
        ) : _Service(logger, mapper, options)
    {
        Defaults Defaults { get; } = defaults;

        string blankName = "N/A";
        private readonly IStore _store = store;
        private readonly TicketService _ticketService = ticketService;

        internal async Task<UserReport> GetUserStats()
            => new UserReport
            {
                Timestamp = DateTime.UtcNow,
                EnrolledUserCount = await _store.WithNoTracking<Data.User>().Where(u => u.Enrollments.Count > 0).CountAsync(),
                UnenrolledUserCount = await _store.WithNoTracking<Data.User>().Where(u => u.Enrollments.Count == 0).CountAsync()
            };

        internal Task<PlayerReport> GetPlayerStats()
        {
            var ps = from games in _store.WithNoTracking<Data.Game>()
                     select new PlayerStat
                     {
                         GameId = games.Id,
                         GameName = games.Name,
                         // games.Players - people who have enrolled in the game (not necessarily those who played it)
                         PlayerCount = games.Players.Count,
                         // If a player has a session year later than 0001, they've started a session
                         SessionPlayerCount = games.Players.Where(p => p.SessionBegin.ToString() != "-infinity" && p.SessionBegin > DateTimeOffset.MinValue).Count()
                     };

            PlayerReport playerReport = new()
            {
                Timestamp = DateTime.UtcNow,
                Stats = ps.ToArray()
            };

            return Task.FromResult(playerReport);
        }

        internal async Task<SponsorReport> GetSponsorStats()
        {
            return new SponsorReport
            {
                Timestamp = DateTime.UtcNow,
                Stats = await _store.WithNoTracking<Data.Sponsor>().Select(s => new SponsorStat
                {
                    Id = s.Id,
                    Name = s.Name,
                    Logo = s.Logo,
                    Count = s.SponsoredUsers.Count
                })
                .OrderByDescending(s => s.Count).ThenBy(s => s.Name)
                .ToArrayAsync()
            };
        }

        internal Task<GameSponsorReport> GetGameSponsorsStats(string gameId)
        {
            var gameSponsorStats = new List<GameSponsorStat>();

            if (gameId.IsEmpty())
                throw new ArgumentNullException("Invalid game id");

            var game = _store
                .WithNoTracking<Data.Game>()
                .Where(g => g.Id == gameId)
                .Select(g => new { g.Id, g.Name, g.MaxTeamSize })
                .FirstOrDefault() ?? throw new Exception("Invalid game");

            var players = _store
                .WithNoTracking<Data.Player>()
                .Where(p => p.GameId == gameId)
                .Select(p => new { p.Sponsor, p.TeamId, p.Id, p.UserId }).ToList();

            var sponsors = _store.WithNoTracking<Data.Sponsor>();
            var sponsorStats = new List<SponsorStat>();

            foreach (Data.Sponsor sponsor in sponsors)
            {
                sponsorStats.Add(new SponsorStat
                {
                    Id = sponsor.Id,
                    Name = sponsor.Name,
                    Logo = sponsor.Logo,
                    Count = players.Where(p => p.Sponsor.Id == sponsor.Id).Count(),
                    TeamCount = players.Where(p => p.Sponsor.Id == sponsor.Id && (
                        // Either every player on a team has the same sponsor, or...
                        players.Where(p2 => p.Id != p2.Id && p.TeamId == p2.TeamId).All(p2 => p.Sponsor == p2.Sponsor) ||
                        // ...the team has only one player on it, so still count them
                        players.Where(p2 => p.TeamId == p2.TeamId).Count() == 1)
                    ).Select(p => p.TeamId).Distinct().Count()
                });
            }

            sponsorStats = sponsorStats.OrderByDescending(g => game.MaxTeamSize > 0 ? g.TeamCount : g.Count).ToList();

            // Create row for multisponsor teams
            sponsorStats.Add(new SponsorStat
            {
                Id = "Multisponsor",
                Name = "Multisponsor",
                Logo = "",
                Count = 0,
                TeamCount = players.Where(p =>
                            players.Where(p2 => p.Id != p2.Id && p.TeamId == p2.TeamId)
                                .Any(p2 => p.Sponsor != p2.Sponsor)).Select(p => p.TeamId).Distinct().Count()
            });

            GameSponsorStat gameSponsorStat = new GameSponsorStat
            {
                GameId = gameId,
                GameName = game.Name,
                Stats = sponsorStats.ToArray()
            };

            gameSponsorStats.Add(gameSponsorStat);

            GameSponsorReport sponsorReport = new GameSponsorReport
            {
                Timestamp = DateTime.UtcNow,
                Stats = gameSponsorStats.ToArray()
            };

            return Task.FromResult(sponsorReport);
        }

        internal async Task<ChallengeReport> GetChallengeStats(string gameId)
        {
            var challenges = await _store
                .WithNoTracking<Data.Challenge>()
                .Where(c => c.GameId == gameId)
                .Select(c => new
                {
                    c.SpecId,
                    c.Name,
                    c.Tag,
                    c.Points,
                    c.Score,
                    c.Result,
                    c.Duration
                })
                .ToArrayAsync()
            ;

            var stats = challenges
                .GroupBy(c => c.SpecId)
                .Select(g => new ChallengeStat
                {
                    Id = g.Key,
                    Name = g.First().Name,
                    Tag = g.First().Tag,
                    Points = g.First().Points,
                    SuccessCount = g.Count(o => o.Result == ChallengeResult.Success),
                    PartialCount = g.Count(o => o.Result == ChallengeResult.Partial),
                    AverageTime = g.Any(c => c.Result == ChallengeResult.Success)
                        ? new TimeSpan(0, 0, 0, 0, (int)g
                            .Where(c => c.Result == ChallengeResult.Success)
                            .Average(o => o.Duration)
                        ).ToString(@"hh\:mm\:ss")
                        : "",
                    AttemptCount = g.Count(),
                    AverageScore = (int)g.Average(c => c.Score)
                })
                .ToArray()
            ;

            ChallengeReport challengeReport = new ChallengeReport
            {
                Timestamp = DateTime.UtcNow,
                Stats = stats.ToArray()
            };

            return challengeReport;
        }

        internal async Task<ChallengeDetailReport> GetChallengeDetails(string id)
        {
            var challenges = Mapper.Map<Challenge[]>(await _store.WithNoTracking<Data.Challenge>().Where(c => c.SpecId == id).ToArrayAsync());
            List<Part> parts = [];

            if (challenges.Length > 0)
            {
                var questions = challenges[0].State.Challenge.Questions.ToArray();

                foreach (var questionView in questions)
                {
                    parts.Add(new Part { Text = questionView.Text, SolveCount = 0, AttemptCount = 0, Weight = questionView.Weight });
                }

                foreach (var challenge in challenges)
                {
                    foreach (var questionView in challenge.State.Challenge.Questions)
                    {
                        if (questionView.IsGraded)
                        {
                            Part part = parts.Find(p => p.Text == questionView.Text);

                            if (part != null)
                            {
                                if (questionView.IsCorrect)
                                {
                                    part.SolveCount++;
                                }
                            }
                        }
                    }
                }
            }

            var challengeDetailReport = new ChallengeDetailReport
            {
                Timestamp = DateTime.UtcNow,
                Parts = parts.ToArray(),
                AttemptCount = challenges != null ? challenges.Length : 0,
                ChallengeId = id
            };

            return challengeDetailReport;
        }

        internal Task<SeriesReport> GetSeriesStats()
        {
            // Create a temporary table of all series with the number of games in that series included
            var tempTable = _store.WithNoTracking<Data.Game>().Select(
                g => new
                {
                    // Replace null, white space, or empty series with "N/A"
                    Series = string.IsNullOrWhiteSpace(g.Competition) ? blankName : g.Competition
                    // To create the table we have to group by the series, then count the rows in each group
                }).GroupBy(g => g.Series).Select(
                s => new
                {
                    Series = s.Key,
                    GameCount = s.Count()
                });

            ParticipationStat[] stats = tempTable.GroupBy(g => new { g.Series, g.GameCount }).Select(
                s => new ParticipationStat
                {
                    // Get the formatted series
                    Key = s.Key.Series,
                    // Get the number of games in the series
                    GameCount = s.Key.GameCount,
                    // Get the number of registered players in the series
                    PlayerCount = _store.WithNoTracking<Data.Player>().Where(p => (string.IsNullOrWhiteSpace(p.Game.Competition) ? blankName : p.Game.Competition) == s.Key.Series).Select(p => p.UserId).Distinct().Count(),
                    // Get the number of enrolled players in the series
                    SessionPlayerCount = _store.WithNoTracking<Data.Player>().Where(p => (string.IsNullOrWhiteSpace(p.Game.Competition) ? blankName : p.Game.Competition) == s.Key.Series && p.SessionBegin.ToString() != "-infinity" && p.SessionBegin > DateTimeOffset.MinValue).Select(p => p.UserId).Distinct().Count(),
                    // Get the number of registered teams in the series
                    TeamCount = _store.WithNoTracking<Data.Player>().Where(p => (string.IsNullOrWhiteSpace(p.Game.Competition) ? blankName : p.Game.Competition) == s.Key.Series).Select(p => p.TeamId).Distinct().Count(),
                    // Get the number of enrolled teams in the series
                    SessionTeamCount = _store.WithNoTracking<Data.Player>().Where(p => (string.IsNullOrWhiteSpace(p.Game.Competition) ? blankName : p.Game.Competition) == s.Key.Series && p.SessionBegin.ToString() != "-infinity" && p.SessionBegin > DateTimeOffset.MinValue).Select(p => p.TeamId).Distinct().Count(),
                    // Get the number of challenges deployed in this series
                    ChallengesDeployedCount = _store.WithNoTracking<Data.ArchivedChallenge>().Join(_store.WithNoTracking<Data.Game>(), ac => ac.GameId, g => g.Id, (ac, g) => new { GameId = g.Id, Series = g.Competition }).Where(g => (string.IsNullOrWhiteSpace(g.Series) ? blankName : g.Series) == s.Key.Series).Count() + _store.WithNoTracking<Data.Challenge>().Join(_store.WithNoTracking<Data.Game>(), c => c.GameId, g => g.Id, (c, g) => new { Series = g.Competition }).Where(g => (string.IsNullOrWhiteSpace(g.Series) ? blankName : g.Series) == s.Key.Series).Count()
                }
            ).OrderBy(stat => stat.Key).ToArray();

            SeriesReport seriesReport = new SeriesReport
            {
                Timestamp = DateTime.UtcNow,
                Stats = stats
            };

            return Task.FromResult(seriesReport);
        }

        internal Task<TrackReport> GetTrackStats()
        {

            // Create a temporary table of all tracks with the number of games in that track included
            var tempTable = _store.WithNoTracking<Data.Game>().Select(
                g => new
                {
                    // Replace null, white space, or empty tracks with "N/A"
                    Track = string.IsNullOrWhiteSpace(g.Track) ? blankName : g.Track
                    // To create the table we have to group by the track, then count the rows in each group
                }).GroupBy(g => g.Track).Select(
                s => new
                {
                    Track = s.Key,
                    GameCount = s.Count()
                });

            // Perform actual grouping logic using the above table; we group by both columns
            ParticipationStat[] stats = tempTable.GroupBy(g => new { g.Track, g.GameCount }).Select(
                s => new ParticipationStat
                {
                    // Get the formatted track
                    Key = s.Key.Track,
                    // Get the number of games in the track
                    GameCount = s.Key.GameCount,
                    // Get the number of registered players in the track
                    PlayerCount = _store.WithNoTracking<Data.Player>().Where(p => (string.IsNullOrWhiteSpace(p.Game.Track) ? blankName : p.Game.Track) == s.Key.Track).Select(p => p.UserId).Distinct().Count(),
                    // Get the number of enrolled players in the track
                    SessionPlayerCount = _store.WithNoTracking<Data.Player>().Where(p => (string.IsNullOrWhiteSpace(p.Game.Track) ? blankName : p.Game.Track) == s.Key.Track && p.SessionBegin.ToString() != "-infinity" && p.SessionBegin > DateTimeOffset.MinValue).Select(p => p.UserId).Distinct().Count(),
                    // Get the number of registered teams in the track
                    TeamCount = _store.WithNoTracking<Data.Player>().Where(p => (string.IsNullOrWhiteSpace(p.Game.Track) ? blankName : p.Game.Track) == s.Key.Track).Select(p => p.TeamId).Distinct().Count(),
                    // Get the number of enrolled teams in the track
                    SessionTeamCount = _store.WithNoTracking<Data.Player>().Where(p => (string.IsNullOrWhiteSpace(p.Game.Track) ? blankName : p.Game.Track) == s.Key.Track && p.SessionBegin.ToString() != "-infinity" && p.SessionBegin > DateTimeOffset.MinValue).Select(p => p.TeamId).Distinct().Count(),
                    // Get the number of challenges deployed in this track
                    ChallengesDeployedCount = _store.WithNoTracking<Data.ArchivedChallenge>().Join(_store.WithNoTracking<Data.Game>(), ac => ac.GameId, g => g.Id, (ac, g) => new { GameId = g.Id, Track = g.Track }).Where(g => (string.IsNullOrWhiteSpace(g.Track) ? blankName : g.Track) == s.Key.Track).Count() + _store.WithNoTracking<Data.Challenge>().Join(_store.WithNoTracking<Data.Game>(), c => c.GameId, g => g.Id, (c, g) => new { Track = g.Track }).Where(g => (string.IsNullOrWhiteSpace(g.Track) ? blankName : g.Track) == s.Key.Track).Count()
                }
            ).OrderBy(stat => stat.Key).ToArray();

            TrackReport trackReport = new TrackReport
            {
                Timestamp = DateTime.UtcNow,
                Stats = stats
            };

            return Task.FromResult(trackReport);
        }

        internal Task<SeasonReport> GetSeasonStats()
        {

            // Create a temporary table of all divisions with the number of games in that division included
            var tempTable = _store.WithNoTracking<Data.Game>().Select(
                g => new
                {
                    // Replace null, white space, or empty divisions with "N/A"
                    Season = string.IsNullOrWhiteSpace(g.Season) ? blankName : g.Season
                    // To create the table we have to group by the division, then count the rows in each group
                }).GroupBy(g => g.Season).Select(
                s => new
                {
                    Season = s.Key,
                    GameCount = s.Count()
                });

            // Perform actual grouping logic using the above table; we group by both columns
            ParticipationStat[] stats = tempTable.GroupBy(g => new { g.Season, g.GameCount }).Select(
                s => new ParticipationStat
                {
                    // Get the formatted division
                    Key = s.Key.Season,
                    // Get the number of games in the division
                    GameCount = s.Key.GameCount,
                    // Get the number of registered players in the division
                    PlayerCount = _store.WithNoTracking<Data.Player>().Where(p => (string.IsNullOrWhiteSpace(p.Game.Season) ? blankName : p.Game.Season) == s.Key.Season).Select(p => p.UserId).Distinct().Count(),
                    // Get the number of enrolled players in the division
                    SessionPlayerCount = _store.WithNoTracking<Data.Player>().Where(p => (string.IsNullOrWhiteSpace(p.Game.Season) ? blankName : p.Game.Season) == s.Key.Season && p.SessionBegin.ToString() != "-infinity" && p.SessionBegin > DateTimeOffset.MinValue).Select(p => p.UserId).Distinct().Count(),
                    // Get the number of registered teams in the season
                    TeamCount = _store.WithNoTracking<Data.Player>().Where(p => (string.IsNullOrWhiteSpace(p.Game.Season) ? blankName : p.Game.Season) == s.Key.Season).Select(p => p.TeamId).Distinct().Count(),
                    // Get the number of enrolled teams in the season
                    SessionTeamCount = _store.WithNoTracking<Data.Player>().Where(p => (string.IsNullOrWhiteSpace(p.Game.Season) ? blankName : p.Game.Season) == s.Key.Season && p.SessionBegin.ToString() != "-infinity" && p.SessionBegin > DateTimeOffset.MinValue).Select(p => p.TeamId).Distinct().Count(),
                    // Get the number of challenges deployed in this season
                    ChallengesDeployedCount = _store.WithNoTracking<Data.ArchivedChallenge>().Join(_store.WithNoTracking<Data.Game>(), ac => ac.GameId, g => g.Id, (ac, g) => new { GameId = g.Id, Season = g.Season }).Where(g => (string.IsNullOrWhiteSpace(g.Season) ? blankName : g.Season) == s.Key.Season).Count() + _store.WithNoTracking<Data.Challenge>().Join(_store.WithNoTracking<Data.Game>(), c => c.GameId, g => g.Id, (c, g) => new { Season = g.Season }).Where(g => (string.IsNullOrWhiteSpace(g.Season) ? blankName : g.Season) == s.Key.Season).Count()
                }
            ).OrderBy(stat => stat.Key).ToArray();

            SeasonReport seasonReport = new SeasonReport
            {
                Timestamp = DateTime.UtcNow,
                Stats = stats
            };

            return Task.FromResult(seasonReport);
        }

        internal Task<DivisionReport> GetDivisionStats()
        {

            // Create a temporary table of all divisions with the number of games in that division included
            var tempTable = _store.WithNoTracking<Data.Game>().Select(
                g => new
                {
                    // Replace null, white space, or empty divisions with "N/A"
                    Division = string.IsNullOrWhiteSpace(g.Division) ? blankName : g.Division
                    // To create the table we have to group by the division, then count the rows in each group
                }).GroupBy(g => g.Division).Select(
                s => new
                {
                    Division = s.Key,
                    GameCount = s.Count()
                });

            // Perform actual grouping logic using the above table; we group by both columns
            ParticipationStat[] stats = tempTable.GroupBy(g => new { g.Division, g.GameCount }).Select(
                s => new ParticipationStat
                {
                    // Get the formatted division
                    Key = s.Key.Division,
                    // Get the number of games in the division
                    GameCount = s.Key.GameCount,
                    // Get the number of registered players in the division
                    PlayerCount = _store.WithNoTracking<Data.Player>().Where(p => (string.IsNullOrWhiteSpace(p.Game.Division) ? blankName : p.Game.Division) == s.Key.Division).Select(p => p.UserId).Distinct().Count(),
                    // Get the number of enrolled players in the division
                    SessionPlayerCount = _store.WithNoTracking<Data.Player>().Where(p => (string.IsNullOrWhiteSpace(p.Game.Division) ? blankName : p.Game.Division) == s.Key.Division && p.SessionBegin.ToString() != "-infinity" && p.SessionBegin > DateTimeOffset.MinValue).Select(p => p.UserId).Distinct().Count(),
                    // Get the number of registered teams in the division
                    TeamCount = _store.WithNoTracking<Data.Player>().Where(p => (string.IsNullOrWhiteSpace(p.Game.Division) ? blankName : p.Game.Division) == s.Key.Division).Select(p => p.TeamId).Distinct().Count(),
                    // Get the number of enrolled teams in the division
                    SessionTeamCount = _store.WithNoTracking<Data.Player>().Where(p => (string.IsNullOrWhiteSpace(p.Game.Division) ? blankName : p.Game.Division) == s.Key.Division && p.SessionBegin.ToString() != "-infinity" && p.SessionBegin > DateTimeOffset.MinValue).Select(p => p.TeamId).Distinct().Count(),
                    // Get the number of challenges deployed in this division
                    ChallengesDeployedCount = _store.WithNoTracking<Data.ArchivedChallenge>().Join(_store.WithNoTracking<Data.Game>(), ac => ac.GameId, g => g.Id, (ac, g) => new { GameId = g.Id, Division = g.Division }).Where(g => (string.IsNullOrWhiteSpace(g.Division) ? blankName : g.Division) == s.Key.Division).Count() + _store.WithNoTracking<Data.Challenge>().Join(_store.WithNoTracking<Data.Game>(), c => c.GameId, g => g.Id, (c, g) => new { Division = g.Division }).Where(g => (string.IsNullOrWhiteSpace(g.Division) ? blankName : g.Division) == s.Key.Division).Count()
                }
            ).OrderBy(stat => stat.Key).ToArray();

            DivisionReport divisionReport = new DivisionReport
            {
                Timestamp = DateTime.UtcNow,
                Stats = stats
            };

            return Task.FromResult(divisionReport);
        }

        internal Task<ModeReport> GetModeStats()
        {

            // Create a temporary table of all modes with the number of games in that mode included
            var tempTable = _store.WithNoTracking<Data.Game>().Select(
                g => new
                {
                    // Replace null, white space, or empty modes with "N/A"
                    Mode = string.IsNullOrWhiteSpace(g.Mode) ? blankName : g.Mode
                    // To create the table we have to group by the mode, then count the rows in each group
                }).GroupBy(g => g.Mode).Select(
                s => new
                {
                    Mode = s.Key,
                    GameCount = s.Count()
                });

            // Perform actual grouping logic using the above table; we group by both columns
            ParticipationStat[] stats = tempTable.GroupBy(g => new { g.Mode, g.GameCount }).Select(
                s => new ParticipationStat
                {
                    // Get the formatted mode
                    Key = s.Key.Mode,
                    // Get the number of games in the mode
                    GameCount = s.Key.GameCount,
                    // Get the number of registered players in the mode
                    PlayerCount = _store.WithNoTracking<Data.Player>().Where(p => (string.IsNullOrWhiteSpace(p.Game.Mode) ? blankName : p.Game.Mode) == s.Key.Mode).Select(p => p.UserId).Distinct().Count(),
                    // Get the number of enrolled players in the mode
                    SessionPlayerCount = _store.WithNoTracking<Data.Player>().Where(p => (string.IsNullOrWhiteSpace(p.Game.Mode) ? blankName : p.Game.Mode) == s.Key.Mode && p.SessionBegin.ToString() != "-infinity" && p.SessionBegin > DateTimeOffset.MinValue).Select(p => p.UserId).Distinct().Count(),
                    // Get the number of registered teams in the mode
                    TeamCount = _store.WithNoTracking<Data.Player>().Where(p => (string.IsNullOrWhiteSpace(p.Game.Mode) ? blankName : p.Game.Mode) == s.Key.Mode).Select(p => p.TeamId).Distinct().Count(),
                    // Get the number of enrolled teams in the mode
                    SessionTeamCount = _store.WithNoTracking<Data.Player>().Where(p => (string.IsNullOrWhiteSpace(p.Game.Mode) ? blankName : p.Game.Mode) == s.Key.Mode && p.SessionBegin.ToString() != "-infinity" && p.SessionBegin > DateTimeOffset.MinValue).Select(p => p.TeamId).Distinct().Count(),
                    // Get the number of challenges deployed in this mode
                    ChallengesDeployedCount = _store.WithNoTracking<Data.ArchivedChallenge>().Join(_store.WithNoTracking<Data.Game>(), ac => ac.GameId, g => g.Id, (ac, g) => new { GameId = g.Id, Mode = g.Mode }).Where(g => (string.IsNullOrWhiteSpace(g.Mode) ? blankName : g.Mode) == s.Key.Mode).Count() + _store.WithNoTracking<Data.Challenge>().Join(_store.WithNoTracking<Data.Game>(), c => c.GameId, g => g.Id, (c, g) => new { Mode = g.Mode }).Where(g => (string.IsNullOrWhiteSpace(g.Mode) ? blankName : g.Mode) == s.Key.Mode).Count()
                }
            ).OrderBy(stat => stat.Key).ToArray();

            var modeReport = new ModeReport
            {
                Timestamp = DateTime.UtcNow,
                Stats = stats
            };

            return Task.FromResult(modeReport);
        }

        internal Task<CorrelationReport> GetCorrelationStats()
        {
            // Create a temporary table to first group by the user ID and count the number of games played
            var tempTable = _store.WithNoTracking<Data.Player>().GroupBy(g => g.UserId).Select(
                s => new
                {
                    UserId = s.Key,
                    GameCount = s.Count()
                }
            );

            // Re-group by the number of games played to count the number of users who enrolled in them
            CorrelationStat[] stats = tempTable.GroupBy(g => g.GameCount).Select(
                s => new CorrelationStat
                {
                    GameCount = s.Key,
                    UserCount = s.Count()
                }
            ).OrderBy(stat => stat.GameCount).ToArray();

            CorrelationReport correlationReport = new CorrelationReport
            {
                Timestamp = DateTime.UtcNow,
                Stats = stats
            };

            return Task.FromResult(correlationReport);
        }

        #region Support Stats
        internal async Task<TicketDetail[]> GetTicketDetails(TicketReportFilter model, string userId)
        {
            var q = ListFilteredTickets(model, userId);
            var tickets = await q.ToArrayAsync();

            // Todo: make sure times are eastern when grouping days and shifts
            return tickets
                .Select(t => new TicketDetail
                {
                    Key = t.Key,
                    Summary = t.Summary != null ? t.Summary : "",
                    Description = t.Description != null ? t.Description : "",
                    Challenge = t.Challenge != null && t.Challenge.Name != null ? t.Challenge.Name : "",
                    Team = t.Player != null && t.Player.ApprovedName != null ? t.Player.ApprovedName : "",
                    GameSession = t.Player != null && t.Player.Game != null && t.Player.Game.Name != null ? t.Player.Game.Name : "",
                    Assignee = t.Assignee != null && t.Assignee.Name != null ? t.Assignee.Name : "",
                    Requester = t.Requester != null && t.Requester.Name != null ? t.Requester.Name : "",
                    Creator = t.Creator != null && t.Creator.Name != null ? t.Creator.Name : "",
                    Created = t.Created,
                    LastUpdated = t.LastUpdated,
                    Label = t.Label,
                    Status = t.Status
                }
                )
                .OrderBy(detail => detail.Key).ToArray();
        }

        internal async Task<TicketDayReport> GetTicketVolume(TicketReportFilter model)
        {
            var q = ListFilteredTickets(model);
            var tickets = await q.ToArrayAsync();

            var ticketsGrouped = tickets
                .GroupBy(g => new
                {
                    Date = TimeZoneInfo.ConvertTime(g.Created, TimeZoneInfo.FindSystemTimeZoneById(Defaults.ShiftTimezone)).ToString("MM/dd/yyyy"),
                    DayOfWeek = TimeZoneInfo.ConvertTime(g.Created, TimeZoneInfo.FindSystemTimeZoneById(Defaults.ShiftTimezone)).DayOfWeek.ToString()
                });

            // Get the shifts provided in AppSettings.cs
            DateTimeOffset[][] shifts = Defaults.Shifts;

            // Counts
            int[][] shiftCountsByDay = new int[ticketsGrouped.Count()][];
            int[] outsideShiftCountsByDay = new int[ticketsGrouped.Count()];

            // Set the number of days observed so far
            int dayNum = 0;

            var result = ticketsGrouped
                .Select(g =>
                {
                    // Set the shift counts 
                    shiftCountsByDay[dayNum] = new int[shifts.Length];
                    g.ToList().ForEach(ticket =>
                    {
                        // Force convert creation to the default timezone (in AppSettings.cs this is Eastern Standard Time)
                        DateTimeOffset tz = TimeZoneInfo.ConvertTime(ticket.Created, TimeZoneInfo.FindSystemTimeZoneById(Defaults.ShiftTimezone));
                        var ticketCreatedHour = tz.Hour;
                        // Flag to check if we've found a matching shift or not
                        var found = false;
                        // Loop through all given shifts
                        for (int i = 0; i < shifts.Length; i++)
                        {
                            // See if the ticket falls within this shift; each shift hour is already converted to the default time
                            if (ticketCreatedHour >= shifts[i][0].Hour && ticketCreatedHour < shifts[i][1].Hour)
                            {
                                shiftCountsByDay[dayNum][i] += 1;
                                found = true;
                            }
                        }
                        // If we haven't found a matching shift for this ticket, it's outside shift hours for this day
                        if (!found) outsideShiftCountsByDay[dayNum] += 1;
                    });
                    // Increase the number of days observed
                    dayNum += 1;
                    // Create a new TicketDayGroup and set its attributes
                    return new TicketDayGroup
                    {
                        Date = g.Key.Date,
                        DayOfWeek = g.Key.DayOfWeek,
                        Count = shiftCountsByDay[dayNum - 1].Sum() + outsideShiftCountsByDay[dayNum - 1],
                        ShiftCounts = shiftCountsByDay[dayNum - 1],
                        OutsideShiftCount = outsideShiftCountsByDay[dayNum - 1]
                    };
                })
                .OrderByDescending(g => g.Date)
                .AsQueryable();

            // if no custom date range, only show the most recent 10
            if (!model.WantsAfterStartTime && !model.WantsBeforeEndTime)
                result = result.Take(7);

            string[] tzWords = Defaults.ShiftTimezone.Split(" ");
            string timezone = tzWords[0].First() + "" + tzWords[tzWords.Length - 1].First();

            TicketDayReport ticketDayReport = new TicketDayReport
            {
                Shifts = Defaults.ShiftStrings,
                Timezone = timezone,
                TicketDays = result.ToArray()
            };

            return ticketDayReport;
        }

        internal async Task<TicketLabelGroup[]> GetTicketLabels(TicketReportFilter model)
        {
            var q = ListFilteredTickets(model);
            q = q.Where(t => t.Label != null);

            var tickets = await q.ToArrayAsync();

            return tickets
                .SelectMany(t => t.Label.Split(" "))
                .GroupBy(a => a)
                .Select(a => new TicketLabelGroup
                {
                    Label = a.Key,
                    Count = a.Count()
                })
                .OrderByDescending(a => a.Count)
                .ToArray();
        }

        internal async Task<TicketChallengeGroup[]> GetTicketChallenges(TicketReportFilter model)
        {
            var q = ListFilteredTickets(model);

            q = q.Where(t => t.ChallengeId != null).Include(t => t.Challenge);

            var tickets = await q.ToArrayAsync();

            return tickets
                .GroupBy(t => new
                {
                    ChallengeSpecId = t.Challenge.SpecId,
                    ChallengeTag = t.Challenge.Tag,
                    ChallengeName = t.Challenge.Name
                })
                .Select(a => new TicketChallengeGroup
                {
                    ChallengeSpecId = a.Key.ChallengeSpecId,
                    ChallengeTag = a.Key.ChallengeTag,
                    ChallengeName = a.Key.ChallengeName,
                    Count = a.Count()
                })
                .OrderByDescending(a => a.Count)
                .ToArray();
        }
        #endregion

        private IQueryable<Data.Ticket> ListFilteredTickets(TicketReportFilter model, string userId = null)
        {
            var q = _store.WithNoTracking<Data.Ticket>();
            if (userId != null)
            {
                q = _ticketService.BuildTicketSearchQuery(model.Term);
            }

            if (model.WantsGame)
                q = q.Include(t => t.Player).Where(t => t.Player.GameId == model.GameId);

            if (model.WantsAfterStartTime)
                q = q.Where(t => t.Created > model.StartRange);

            if (model.WantsBeforeEndTime)
                q = q.Where(t => t.Created < model.EndRange);

            if (model.WantsOpen)
                q = q.Where(t => t.Status == "Open");
            if (model.WantsInProgress)
                q = q.Where(t => t.Status == "In Progress");
            if (model.WantsClosed)
                q = q.Where(t => t.Status == "Closed");
            if (model.WantsNotClosed)
                q = q.Where(t => t.Status != "Closed");

            if (model.WantsAssignedToMe)
                q = q.Where(t => t.AssigneeId == userId);
            if (model.WantsUnassigned)
                q = q.Where(t => t.AssigneeId == null || t.AssigneeId == "");

            return q;
        }


        // create file name for feedback reports based on type and names of games/challenges
        internal string GetFeedbackFilename(string gameName, bool wantsGame, bool wantsSpecificChallenge, string challengeTag, bool isStats)
        {
            string filename = string.Format(
                "{0}-{1}-feedback{2}-{3}",
                wantsSpecificChallenge ? challengeTag : gameName,
                wantsGame ? "game" : "challenge",
                isStats ? "-stats" : "",
                DateTime.UtcNow.ToString("yyyy-MM-dd")
            ) + ".csv";
            return filename;
        }

        // Count the maximum amount of feedback responses possible given the search params (how much feedback there could be if 100% response rate)
        public async Task<int> GetFeedbackMaxResponses(FeedbackSearchParams model)
        {
            int total = 0;
            if (model.WantsGame) // count enrollments for a specific game id, that are started
                total = await _store.WithNoTracking<Data.Player>().Where(p => p.GameId == model.GameId && p.SessionBegin > DateTimeOffset.MinValue).CountAsync();
            else if (model.WantsSpecificChallenge) // count challenges with specific challenge spec id
                total = await _store.WithNoTracking<Data.Challenge>().Where(p => p.SpecId == model.ChallengeSpecId).CountAsync();
            else if (model.WantsChallenge) // count challenges with specific game id
                total = await _store.WithNoTracking<Data.Challenge>().Where(p => p.GameId == model.GameId).CountAsync();
            return total;
        }

        // Compute aggregates for each feedback question in template based on all responses in feedback table
        public IEnumerable<QuestionStats> GetFeedbackQuestionStats(QuestionTemplate[] questionTemplate, FeedbackReportHelper[] feedbackTable)
        {
            var questionStats = new List<QuestionStats>();
            foreach (QuestionTemplate question in questionTemplate)
            {
                if (question.Type != "likert")
                    continue;

                var answers = new List<int>();
                foreach (var response in feedbackTable.Where(f => f.Submitted || true))
                {
                    var answer = response.IdToAnswer.GetValueOrDefault(question.Id, null);
                    if (answer != null)
                        answers.Add(Int32.Parse(answer));
                }
                var newStat = new QuestionStats
                {
                    Id = question.Id,
                    Prompt = question.Prompt,
                    ShortName = question.ShortName,
                    Required = question.Required,
                    ScaleMin = question.Min,
                    ScaleMax = question.Max,
                    Count = answers.Count,
                };
                if (newStat.Count > 0)
                {
                    newStat.Average = answers.Average();
                    newStat.Lowest = answers.Min();
                    newStat.Highest = answers.Max();
                }
                questionStats.Add(newStat);
            }
            return questionStats;
        }

        internal byte[] ConvertToBytes<T>(IEnumerable<T> collection)
        {
            var value = ServiceStack.StringExtensions.ToCsv(collection);

            return Encoding.UTF8.GetBytes(value.ToString());
        }
    }
}
