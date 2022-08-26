using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TopoMojo.Api.Client;

namespace Gameboard.Api.Services
{
    public class ReportService : _Service
    {
        GameboardDbContext Store { get; }
        ChallengeService _challengeService { get; }

        public ReportService (
            ILogger<ReportService> logger,
            IMapper mapper,
            CoreOptions options,
            GameboardDbContext store,
            ChallengeService challengeService,
            GameService gameService
        ): base (logger, mapper, options)
        {
            Store = store;
            _challengeService = challengeService;
        }

        internal Task<UserReport> GetUserStats()
        {
            UserReport userReport = new UserReport
            {
                Timestamp = DateTime.UtcNow,
                EnrolledUserCount = Store.Users.Where(u => u.Enrollments.Count() > 0).Count(),
                UnenrolledUserCount = Store.Users.Where(u => u.Enrollments.Count == 0).Count(),
            };

            return Task.FromResult(userReport);
        }

        internal Task<PlayerReport> GetPlayerStats()
        {
            var ps = from games in Store.Games
                     select new PlayerStat { 
                        GameId = games.Id, 
                        GameName = games.Name, 
                        // games.Players - people who have enrolled in the game (not necessarily those who played it)
                        PlayerCount = games.Players.Count, 
                        // If a player has a session year later than 0001, they've started a session
                        SessionPlayerCount = games.Players.Where(p => p.SessionBegin.ToString() != "-infinity" && p.SessionBegin > DateTimeOffset.MinValue).Count()
                    };

            PlayerReport playerReport = new PlayerReport
            {
                Timestamp = DateTime.UtcNow,
                Stats = ps.ToArray()
            };

            return Task.FromResult(playerReport);
        }

        internal Task<SponsorReport> GetSponsorStats()
        {
            var sp = (from sponsors in Store.Sponsors
                      join u in Store.Users on
                      sponsors.Logo equals u.Sponsor
                      select new { sponsors.Id, sponsors.Name, sponsors.Logo }).GroupBy(s => new { s.Id, s.Name, s.Logo })
                      .Select(g => new SponsorStat { Id = g.Key.Id, Name = g.Key.Name, Logo = g.Key.Logo, Count = g.Count() }).OrderByDescending(g => g.Count).ThenBy(g => g.Name);

            SponsorReport sponsorReport = new SponsorReport
            {
                Timestamp = DateTime.UtcNow,
                Stats = sp.ToArray()
            };

            return Task.FromResult(sponsorReport);
        }

        internal Task<GameSponsorReport> GetGameSponsorsStats(string gameId)
        {
            List<GameSponsorStat> gameSponsorStats = new List<GameSponsorStat>();

            if (string.IsNullOrWhiteSpace(gameId))
            {
                throw new ArgumentNullException("Invalid game id");
            }

            var game = Store.Games.Where(g => g.Id == gameId).Select(g => new { g.Id, g.Name }).FirstOrDefault();

            if (game == null)
            {
                throw new Exception("Invalid game");
            }

            var players = Store.Players.Where(p => p.GameId == gameId)
                .Select(p => new { p.Sponsor, p.TeamId }).ToList();

            var sponsors = Store.Sponsors;

            List<SponsorStat> sponsorStats = new List<SponsorStat>();

            foreach (Data.Sponsor sponsor in sponsors)
            {
                sponsorStats.Add(new SponsorStat
                {
                    Id = sponsor.Id,
                    Name = sponsor.Name,
                    Logo = sponsor.Logo,
                    Count = players.Where(p => p.Sponsor == sponsor.Logo).Count(),
                    TeamCount = players.Where(p => p.Sponsor == sponsor.Logo).Select(p => p.TeamId).Distinct().Count()
                });
            }

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
            var challenges = await Store.Challenges
                .Where(c => c.GameId == gameId)
                .Select(c => new {
                    SpecId = c.SpecId,
                    Name = c.Name,
                    Tag = c.Tag,
                    Points = c.Points,
                    Score = c.Score,
                    Result = c.Result,
                    Duration = c.Duration
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
                        ? new TimeSpan(0, 0, 0, 0, (int) g
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
            var challenges = Mapper.Map<Challenge[]>(await Store.Challenges.Where(c => c.SpecId == id).ToArrayAsync());
            List<Part> parts = new List<Part>();

            if (challenges.Length > 0)
            {
                QuestionView[] questions = challenges[0].State.Challenge.Questions.ToArray();

                foreach (QuestionView questionView in questions)
                {
                    parts.Add(new Part{ Text = questionView.Text, SolveCount = 0, AttemptCount = 0, Weight = questionView.Weight });
                }

                foreach (Challenge challenge in challenges)
                {
                    foreach (QuestionView questionView in challenge.State.Challenge.Questions)
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

            ChallengeDetailReport challengeDetailReport = new ChallengeDetailReport();
            challengeDetailReport.Timestamp = DateTime.UtcNow;
            challengeDetailReport.Parts = parts.ToArray();
            challengeDetailReport.AttemptCount = challenges != null ? challenges.Length : 0;
            challengeDetailReport.ChallengeId = id;

            return challengeDetailReport;
        }

        #region Ticket Reports
        internal Task<TicketDetailReport> GetTicketDetails() {
            TicketDetail[] details = Store.Tickets.Select(
                t => new TicketDetail {
                    Key = t.Key,
                    Summary = t.Summary,
                    Description = t.Description,
                    Challenge = t.Challenge.Name,
                    Team = t.Player.ApprovedName,
                    GameSession = t.Player.Game.Name,
                    Assignee = t.Assignee.Name,
                    Requester = t.Requester.Name,
                    Creator = t.Creator.Name,
                    Created = t.Created,
                    LastUpdated = t.LastUpdated,
                    Label = t.Label,
                    Status = t.Status
                }
            ).OrderBy(detail => detail.Key).ToArray();

            TicketDetailReport ticketReport = new TicketDetailReport
            {
                Timestamp = DateTime.UtcNow,
                Details = details
            };

            return Task.FromResult(ticketReport);
        }
        #endregion

        internal Task<SeriesReport> GetSeriesStats() {

            // Create a temporary table of all series with the number of games in that series included
            var tempTable = Store.Games.Select(
                g => new {
                    // Replace null, white space, or empty series with "N/A"
                    Series = string.IsNullOrWhiteSpace(g.Competition) ? "N/A" : g.Competition
                // To create the table we have to group by the series, then count the rows in each group
                }).GroupBy(g => g.Series).Select(
                s => new {
                    Series = s.Key,
                    GameCount = s.Count()
                });

            // Perform actual grouping logic using the above table; we group by both columns
            ParticipationStat[] stats = tempTable.GroupBy(g => new { g.Series, g.GameCount } ).Select(
                s => new ParticipationStat {
                    // Get the formatted series
                    Key = s.Key.Series,
                    // Get the number of games in the series
                    GameCount = s.Key.GameCount,
                    // Get the number of registered players in the series
                    PlayerCount = Store.Players.Where(p => p.Game.Competition == s.Key.Series).Select(p => p.UserId).Distinct().Count(),
                    // Get the number of enrolled players in the series
                    SessionPlayerCount = Store.Players.Where(p => p.Game.Competition == s.Key.Series && p.SessionBegin.ToString() != "-infinity" && p.SessionBegin > DateTimeOffset.MinValue).Select(p => p.UserId).Distinct().Count()
                }
            ).OrderBy(stat => stat.Key).ToArray();

            SeriesReport seriesReport = new SeriesReport
            {
                Timestamp = DateTime.UtcNow,
                Stats = stats
            };

            return Task.FromResult(seriesReport);
        }

        internal Task<TrackReport> GetTrackStats() {

            // Create a temporary table of all tracks with the number of games in that track included
            var tempTable = Store.Games.Select(
                g => new {
                    // Replace null, white space, or empty tracks with "N/A"
                    Track = string.IsNullOrWhiteSpace(g.Track) ? "N/A" : g.Track
                // To create the table we have to group by the track, then count the rows in each group
                }).GroupBy(g => g.Track).Select(
                s => new {
                    Track = s.Key,
                    GameCount = s.Count()
                });

            // Perform actual grouping logic using the above table; we group by both columns
            ParticipationStat[] stats = tempTable.GroupBy(g => new { g.Track, g.GameCount } ).Select(
                s => new ParticipationStat {
                    // Get the formatted track
                    Key = s.Key.Track,
                    // Get the number of games in the track
                    GameCount = s.Key.GameCount,
                    // Get the number of registered players in the track
                    PlayerCount = Store.Players.Where(p => p.Game.Track == s.Key.Track).Select(p => p.UserId).Distinct().Count(),
                    // Get the number of enrolled players in the track
                    SessionPlayerCount = Store.Players.Where(p => p.Game.Track == s.Key.Track && p.SessionBegin.ToString() != "-infinity" && p.SessionBegin > DateTimeOffset.MinValue).Select(p => p.UserId).Distinct().Count()
                }
            ).OrderBy(stat => stat.Key).ToArray();

            TrackReport trackReport = new TrackReport
            {
                Timestamp = DateTime.UtcNow,
                Stats = stats
            };

            return Task.FromResult(trackReport);
        }

        internal Task<SeasonReport> GetSeasonStats() {

            // Create a temporary table of all divisions with the number of games in that division included
            var tempTable = Store.Games.Select(
                g => new {
                    // Replace null, white space, or empty divisions with "N/A"
                    Season = string.IsNullOrWhiteSpace(g.Season) ? "N/A" : g.Season
                // To create the table we have to group by the division, then count the rows in each group
                }).GroupBy(g => g.Season).Select(
                s => new {
                    Season = s.Key,
                    GameCount = s.Count()
                });

            // Perform actual grouping logic using the above table; we group by both columns
            ParticipationStat[] stats = tempTable.GroupBy(g => new { g.Season, g.GameCount } ).Select(
                s => new ParticipationStat {
                    // Get the formatted division
                    Key = s.Key.Season,
                    // Get the number of games in the division
                    GameCount = s.Key.GameCount,
                    // Get the number of registered players in the division
                    PlayerCount = Store.Players.Where(p => p.Game.Season == s.Key.Season).Select(p => p.UserId).Distinct().Count(),
                    // Get the number of enrolled players in the division
                    SessionPlayerCount = Store.Players.Where(p => p.Game.Season == s.Key.Season && p.SessionBegin.ToString() != "-infinity" && p.SessionBegin > DateTimeOffset.MinValue).Select(p => p.UserId).Distinct().Count()
                }
            ).OrderBy(stat => stat.Key).ToArray();

            SeasonReport divisionReport = new SeasonReport
            {
                Timestamp = DateTime.UtcNow,
                Stats = stats
            };

            return Task.FromResult(divisionReport);
        }

        internal Task<DivisionReport> GetDivisionStats() {

            // Create a temporary table of all divisions with the number of games in that division included
            var tempTable = Store.Games.Select(
                g => new {
                    // Replace null, white space, or empty divisions with "N/A"
                    Division = string.IsNullOrWhiteSpace(g.Division) ? "N/A" : g.Division
                // To create the table we have to group by the division, then count the rows in each group
                }).GroupBy(g => g.Division).Select(
                s => new {
                    Division = s.Key,
                    GameCount = s.Count()
                });

            // Perform actual grouping logic using the above table; we group by both columns
            ParticipationStat[] stats = tempTable.GroupBy(g => new { g.Division, g.GameCount } ).Select(
                s => new ParticipationStat {
                    // Get the formatted division
                    Key = s.Key.Division,
                    // Get the number of games in the division
                    GameCount = s.Key.GameCount,
                    // Get the number of registered players in the division
                    PlayerCount = Store.Players.Where(p => p.Game.Division == s.Key.Division).Select(p => p.UserId).Distinct().Count(),
                    // Get the number of enrolled players in the division
                    SessionPlayerCount = Store.Players.Where(p => p.Game.Division == s.Key.Division && p.SessionBegin.ToString() != "-infinity" && p.SessionBegin > DateTimeOffset.MinValue).Select(p => p.UserId).Distinct().Count()
                }
            ).OrderBy(stat => stat.Key).ToArray();

            DivisionReport divisionReport = new DivisionReport
            {
                Timestamp = DateTime.UtcNow,
                Stats = stats
            };

            return Task.FromResult(divisionReport);
        }

        internal Task<ModeReport> GetModeStats() {

            // Create a temporary table of all modes with the number of games in that mode included
            var tempTable = Store.Games.Select(
                g => new {
                    // Replace null, white space, or empty modes with "N/A"
                    Mode = string.IsNullOrWhiteSpace(g.Mode) ? "N/A" : g.Mode
                // To create the table we have to group by the mode, then count the rows in each group
                }).GroupBy(g => g.Mode).Select(
                s => new {
                    Mode = s.Key,
                    GameCount = s.Count()
                });

            // Perform actual grouping logic using the above table; we group by both columns
            ParticipationStat[] stats = tempTable.GroupBy(g => new { g.Mode, g.GameCount } ).Select(
                s => new ParticipationStat {
                    // Get the formatted mode
                    Key = s.Key.Mode,
                    // Get the number of games in the mode
                    GameCount = s.Key.GameCount,
                    // Get the number of registered players in the mode
                    PlayerCount = Store.Players.Where(p => p.Game.Mode == s.Key.Mode).Select(p => p.UserId).Distinct().Count(),
                    // Get the number of enrolled players in the mode
                    SessionPlayerCount = Store.Players.Where(p => p.Game.Mode == s.Key.Mode && p.SessionBegin.ToString() != "-infinity" && p.SessionBegin > DateTimeOffset.MinValue).Select(p => p.UserId).Distinct().Count()
                }
            ).OrderBy(stat => stat.Key).ToArray();

            ModeReport modeReport = new ModeReport
            {
                Timestamp = DateTime.UtcNow,
                Stats = stats
            };

            return Task.FromResult(modeReport);
        }

        internal Task<CorrelationReport> GetCorrelationStats() {

            // Create a temporary table to first group by the user ID and count the number of games played
            var tempTable = Store.Players.GroupBy(g => g.UserId).Select(
                s => new {
                    UserId = s.Key,
                    GameCount = s.Count()
                }
            );

            // Re-group by the number of games played to count the number of users who enrolled in them
            CorrelationStat[] stats = tempTable.GroupBy(g => g.GameCount ).Select(
                s => new CorrelationStat {  
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

        private static string GetCommonGroupString(string original) {
            return string.IsNullOrWhiteSpace(original) ? "N/A" : original;
        }

        // Compute aggregates for each feedback question in template based on all responses in feedback table
        internal List<QuestionStats> GetFeedbackQuestionStats(QuestionTemplate[] questionTemplate, FeedbackReportHelper[] feedbackTable)
        {
            List<QuestionStats> questionStats = new List<QuestionStats>();
            foreach (QuestionTemplate question in questionTemplate)
            {
                if (question.Type != "likert")
                    continue;

                List<int> answers = new List<int>();
                foreach (var response in feedbackTable.Where(f => f.Submitted || true))
                {
                    var answer = response.IdToAnswer.GetValueOrDefault(question.Id, null);
                    if (answer != null)
                        answers.Add(Int32.Parse(answer));
                }
                var newStat = new QuestionStats {
                    Id = question.Id,
                    Prompt = question.Prompt,
                    ShortName = question.ShortName,
                    Required = question.Required,
                    ScaleMin = question.Min,
                    ScaleMax = question.Max,
                    Count = answers.Count(),
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

        internal async Task<TicketDayGroup[]> GetTicketVolume(TicketReportFilter model)
        {
            var q = ListFilteredTickets(model);
            var tickets = await q.ToArrayAsync();

            // Todo: make sure times are eastern when grouping days and shifts
            var result = tickets
                .GroupBy(g => new {
                    Date = g.Created.ToString("MM/dd/yyyy"),
                    DayOfWeek = g.Created.DayOfWeek.ToString()
                })
                .Select(g => {
                        var shift1Count = 0;
                        var shift2Count = 0;
                        var outsideShiftCount = 0;
                        g.ToList().ForEach(ticket => {
                            // Convert creation to local time
                            var ticketCreatedHour = ticket.Created.ToLocalTime().Hour;
                            if (ticketCreatedHour >= 8 && ticketCreatedHour < 16)
                                shift1Count += 1;
                            else if (ticketCreatedHour >= 16 && ticketCreatedHour < 23)
                                shift2Count += 1;
                            else 
                                outsideShiftCount += 1;
                        });
                        return new TicketDayGroup {
                        Date = g.Key.Date,
                        DayOfWeek = g.Key.DayOfWeek,
                        Count = shift1Count + shift2Count + outsideShiftCount,
                        Shift1Count = shift1Count,
                        Shift2Count = shift2Count,
                        OutsideShiftCount = outsideShiftCount
                    };
                })
                .OrderByDescending(g => g.Date)
                .AsQueryable();

            // if no custom date range, only show the most recent 10
            if (!model.WantsAfterStartTime && !model.WantsBeforeEndTime) 
                result = result.Take(7);

            return result.ToArray();

        }

        internal async Task<TicketLabelGroup[]> GetTicketLabels(TicketReportFilter model)
        {
            var q = ListFilteredTickets(model);
            q = q.Where(t => t.Label != null);

            var tickets = await q.ToArrayAsync();

            return tickets
                .SelectMany(t => t.Label.Split(" "))
                .GroupBy(a => a)
                .Select(a => new TicketLabelGroup {
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
                .GroupBy(t => new {
                    ChallengeSpecId = t.Challenge.SpecId,
                    ChallengeTag = t.Challenge.Tag,
                    ChallengeName = t.Challenge.Name
                })
                .Select(a => new TicketChallengeGroup {
                    ChallengeSpecId = a.Key.ChallengeSpecId,
                    ChallengeTag = a.Key.ChallengeTag,
                    ChallengeName = a.Key.ChallengeName,
                    Count = a.Count()
                })
                .OrderByDescending(a => a.Count)
                .ToArray();
        }

        private IQueryable<Data.Ticket> ListFilteredTickets(TicketReportFilter model)
        {
            var q = Store.Tickets.AsNoTracking();

            if (model.WantsGame)
                q = q.Include(t => t.Player).Where(t => t.Player.GameId == model.GameId);

            if (model.WantsAfterStartTime)
                q = q.Where(t => t.Created > model.StartRange);

            if (model.WantsBeforeEndTime) 
                q = q.Where(t => t.Created < model.EndRange);

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
                total = await Store.Players.Where(p => p.GameId == model.GameId && p.SessionBegin > DateTimeOffset.MinValue).CountAsync();
            else if (model.WantsSpecificChallenge) // count challenges with specific challenge spec id
                total = await Store.Challenges.Where(p => p.SpecId == model.ChallengeSpecId).CountAsync();
            else if (model.WantsChallenge) // count challenges with specific game id
                total = await Store.Challenges.Where(p => p.GameId == model.GameId).CountAsync();
            return total; 
        }

        internal byte[] ConvertToBytes<T>(IEnumerable<T> collection)
        {
            var value = ServiceStack.StringExtensions.ToCsv(collection);

            return Encoding.UTF8.GetBytes(value.ToString());
        }
    }
}
