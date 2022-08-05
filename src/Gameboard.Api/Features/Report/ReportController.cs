using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gameboard.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Controllers
{

    [Authorize]
    public class ReportController: _Controller
    {
        public ReportController(
            ILogger<ReportController> logger,
            IDistributedCache cache,
            ReportService service,
            GameService gameService,
            ChallengeSpecService challengeSpecService,
            FeedbackService feedbackService,
            TicketService ticketService
        ): base(logger, cache)
        {
            Service = service;
            GameService = gameService;
            FeedbackService = feedbackService;
            ChallengeSpecService = challengeSpecService;
            TicketService = ticketService;
        }

        ReportService Service { get; }
        GameService GameService { get; }
        FeedbackService FeedbackService { get; }
        ChallengeSpecService ChallengeSpecService { get; }
        TicketService TicketService { get; }

        [HttpGet("/api/report/userstats")]
        [Authorize]
        public async Task<ActionResult<UserReport>> GetUserStats()
        {
            AuthorizeAny(
                () => Actor.IsObserver
            );

            return Ok(await Service.GetUserStats());
        }

        /// <summary>
        /// Export user stats to CSV
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("api/report/exportuserstats")]
        [Authorize]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        public async Task<IActionResult> ExportUserStats()
        {
            AuthorizeAny(
                () => Actor.IsObserver
            );

            var result = await Service.GetUserStats();

            List<Tuple<string, string>> userStats = new List<Tuple<string, string>>();
            userStats.Add(new Tuple<string, string>("Category", "Total"));
            userStats.Add(new Tuple<string, string>("Users Enrolled", result.EnrolledUserCount.ToString()));
            userStats.Add(new Tuple<string, string>("Users Not Enrolled", result.UnenrolledUserCount.ToString()));
            userStats.Add(new Tuple<string, string>("Total User Count", (result.EnrolledUserCount + result.UnenrolledUserCount).ToString()));

            return File(
                Service.ConvertToBytes(userStats),
                "application/octet-stream",
                string.Format("user-stats-{0}", DateTime.UtcNow.ToString("yyyy-MM-dd")) + ".csv");
        }

        [HttpGet("/api/report/playerstats")]
        [Authorize]
        public async Task<ActionResult<PlayerReport>> GetPlayerStats()
        {
            AuthorizeAny(
                () => Actor.IsObserver
            );

            return Ok(await Service.GetPlayerStats());
        }

        /// <summary>
        /// Export player stats to CSV
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("api/report/exportplayerstats")]
        [Authorize]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        public async Task<IActionResult> ExportPlayerStats()
        {
            AuthorizeAny(
                () => Actor.IsObserver
            );

            var result = await Service.GetPlayerStats();

            List<Tuple<string, string>> playerStats = new List<Tuple<string, string>>();
            playerStats.Add(new Tuple<string, string>("Game", "Player Count"));

            foreach(PlayerStat playerStat in result.Stats)
            {
                playerStats.Add(new Tuple<string, string>(playerStat.GameName, playerStat.PlayerCount.ToString()));
            }

            return File(
                Service.ConvertToBytes(playerStats),
                "application/octet-stream",
                string.Format("player-stats-{0}", DateTime.UtcNow.ToString("yyyy-MM-dd")) + ".csv");
        }

        [HttpGet("/api/report/sponsorstats")]
        [Authorize]
        public async Task<ActionResult<SponsorReport>> GetSponsorStats()
        {
            AuthorizeAny(
                () => Actor.IsObserver
            );

            return Ok(await Service.GetSponsorStats());
        }

        [HttpGet("/api/report/exportsponsorstats")]
        [Authorize]
        public async Task<ActionResult<SponsorReport>> ExportSponsorStats()
        {
            AuthorizeAny(
                () => Actor.IsObserver
            );

            var result = await Service.GetSponsorStats();

            List<Tuple<string, string>> sponsorStats = new List<Tuple<string, string>>();
            sponsorStats.Add(new Tuple<string, string>("Name", "User Count"));

            foreach(SponsorStat sponsorStat in result.Stats)
            {
                sponsorStats.Add(new Tuple<string, string>(sponsorStat.Name, sponsorStat.Count.ToString()));
            }

            return File(
                Service.ConvertToBytes(sponsorStats),
                "application/octet-stream",
                string.Format("sponsor-report-{0}", DateTime.UtcNow.ToString("yyyy-MM-dd")) + ".csv");
        }

        [HttpGet("/api/report/gamesponsorstats/{id}")]
        [Authorize]
        public async Task<ActionResult<GameSponsorReport>> GetGameSponsorsStats([FromRoute] string id)
        {
            AuthorizeAny(
                () => Actor.IsObserver
            );

            return Ok(await Service.GetGameSponsorsStats(id));
        }

        [HttpGet("/api/report/exportgamesponsorstats/{id}")]
        [Authorize]
        public async Task<ActionResult<GameSponsorReport>> ExportGameSponsorsStats([FromRoute] string id)
        {
            AuthorizeAny(
                () => Actor.IsObserver
            );

            var result = await Service.GetGameSponsorsStats(id);
            var game = await GameService.Retrieve(id);

            if (game == null)
            {
                return NotFound();
            }
            
            if (game.MaxTeamSize > 1)
            {
                List<Tuple<string, string, string>> gameSponsorStats = new List<Tuple<string, string, string>>();
                gameSponsorStats.Add(new Tuple<string, string, string>("Board:", game.Name, ""));
                gameSponsorStats.Add(new Tuple<string, string, string>("", "", ""));
                gameSponsorStats.Add(new Tuple<string, string, string>("Name", "Player Count", "Team Count"));

                foreach (GameSponsorStat gameSponsorStat in result.Stats)
                {
                    foreach (SponsorStat sponsorStat in gameSponsorStat.Stats)
                    {
                        gameSponsorStats.Add(new Tuple<string, string, string>(sponsorStat.Name, sponsorStat.Count.ToString(), sponsorStat.TeamCount.ToString()));
                    }
                }

                return File(
                    Service.ConvertToBytes(gameSponsorStats),
                    "application/octet-stream",
                    string.Format("board-report-{0}", DateTime.UtcNow.ToString("yyyy-MM-dd")) + ".csv");

            }
            else
            {
                List<Tuple<string, string>> gameSponsorStats = new List<Tuple<string, string>>();
                gameSponsorStats.Add(new Tuple<string, string>("Board:", game.Name));
                gameSponsorStats.Add(new Tuple<string, string>("", ""));
                gameSponsorStats.Add(new Tuple<string, string>("Name", "Player Count"));

                foreach (GameSponsorStat gameSponsorStat in result.Stats)
                {
                    foreach (SponsorStat sponsorStat in gameSponsorStat.Stats)
                    {
                        gameSponsorStats.Add(new Tuple<string, string>(sponsorStat.Name, sponsorStat.Count.ToString()));
                    }
                }

                return File(
                    Service.ConvertToBytes(gameSponsorStats),
                    "application/octet-stream",
                    string.Format("board-report-{0}", DateTime.UtcNow.ToString("yyyy-MM-dd")) + ".csv");
            }
        }

        [HttpGet("/api/report/challengestats/{id}")]
        [Authorize]
        public async Task<ActionResult<ChallengeReport>> GetChallengeStats([FromRoute] string id)
        {
            AuthorizeAny(
                () => Actor.IsObserver
            );

            return Ok(await Service.GetChallengeStats(id));
        }

        /// <summary>
        /// Retrieve challenge details by Spec Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("api/report/challengedetails/{id}")]
        [Authorize]
        public async Task<ChallengeDetailReport> GetChallengeDetails([FromRoute] string id)
        {
            AuthorizeAny(
                () => Actor.IsObserver
            );

            return await Service.GetChallengeDetails(id);
        }

        /// <summary>
        /// Export challenge stats to CSV
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("api/report/exportchallengestats/{id}")]
        [Authorize]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        public async Task<IActionResult> ExportChallengeStats([FromRoute] string id)
        {
            AuthorizeAny(
                () => Actor.IsObserver
            );

            var result = await Service.GetChallengeStats(id);
            var game = await GameService.Retrieve(id);

            if (game == null)
            {
                return NotFound();
            }

            List<ChallengeStatsExport> challengeStats = new List<ChallengeStatsExport>();
            challengeStats.Add(new ChallengeStatsExport { GameName = "Game", ChallengeName = "Challenge", Tag = "Tag", Points = "Points", Attempts = "Attempts #", 
                Complete = "Complete(#/%)", Partial = "Partial(#/%)", AvgTime = "Avg Time", AvgScore = "Avg Score" });

            foreach (ChallengeStat challengeStat in result.Stats)
            {
                challengeStats.Add(new ChallengeStatsExport {
                    GameName = game.Name, ChallengeName = challengeStat.Name, Tag = challengeStat.Tag, Points = challengeStat.Points.ToString(), Attempts = challengeStat.AttemptCount.ToString(), 
                    Complete = challengeStat.SuccessCount.ToString() + " / " + (challengeStat.SuccessCount / challengeStat.AttemptCount).ToString("P", CultureInfo.InvariantCulture),
                    Partial = challengeStat.PartialCount.ToString() + " / " + (challengeStat.PartialCount / challengeStat.AttemptCount).ToString("P", CultureInfo.InvariantCulture), 
                    AvgTime = challengeStat.AverageTime, AvgScore = challengeStat.AverageScore.ToString()});
            }

            return File(
                Service.ConvertToBytes(challengeStats),
                "application/octet-stream",
                string.Format("challenge-statistics-report-{0}", DateTime.UtcNow.ToString("yyyy-MM-dd")) + ".csv");
        }

        /// <summary>
        /// Export challenge details to CSV
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("api/report/exportchallengedetails/{id}")]
        [Authorize]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        public async Task<IActionResult> ExportChallengeDetails([FromRoute] string id)
        {
            AuthorizeAny(
                () => Actor.IsObserver
            );

            var result = await Service.GetChallengeStats(id);
            var game = await GameService.Retrieve(id);

            if (game == null)
            {
                return NotFound();
            }

            List<ChallengeDetailsExport> challengeDetails = new List<ChallengeDetailsExport>();
            challengeDetails.Add(new ChallengeDetailsExport { GameName = "Game", ChallengeName = "Challenge", Tag = "Tag", Question = "Question", Points = "Points / % of Total", Solves = 
                "Solves / % of Attempts Correct" });

            foreach (ChallengeStat stat in result.Stats)
            {
                var challengeDetail = await Service.GetChallengeDetails(stat.Id);

                foreach (Part part in challengeDetail.Parts)
                {
                    challengeDetails.Add(new ChallengeDetailsExport { GameName = game.Name, ChallengeName = stat.Name, Tag = stat.Tag, Question = part.Text, 
                        Points = part.Weight.ToString() + " / " + (part.Weight / stat.Points).ToString("P", CultureInfo.InvariantCulture),
                        Solves = part.SolveCount.ToString() + " / " + ((decimal)part.SolveCount / (decimal)challengeDetail.AttemptCount).ToString("P", CultureInfo.InvariantCulture)
                    });
                }
            }

            return File(
                Service.ConvertToBytes(challengeDetails),
                "application/octet-stream",
                string.Format("challenge-details-report-{0}", DateTime.UtcNow.ToString("yyyy-MM-dd")) + ".csv");
        }

        /// <summary>
        /// Export feedback response details to CSV
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpGet("api/report/exportfeedbackdetails")]
        [Authorize]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        public async Task<IActionResult> ExportFeedbackDetails([FromQuery] FeedbackSearchParams model)
        {
            AuthorizeAny(
                () => Actor.IsObserver
            );

            // gameId must be specified, even for challenge feedback, since template is stored in Game
            var game = await GameService.Retrieve(model.GameId);
            if (game == null || game.FeedbackTemplate == null)
                return NotFound();

            model.SubmitStatus = "submitted";
            var feedback = await FeedbackService.ListFull(model);

            var questionTemplate = FeedbackService.GetTemplate(model.WantsGame, game);
            if (questionTemplate == null)
                return NotFound();

            var expandedTable = FeedbackService.MakeHelperList(feedback);

            // Create list to hold objects with dynamic attributes, key of dictionary is columnm name
            var results = new List<IDictionary<string, object>>();
            foreach (var response in expandedTable)
            {
                IDictionary<string, object> feedbackRow = new ExpandoObject() as IDictionary<string, Object>;
                // Add all meta data from feedback response, columnms based on order of FeedbackReportExport definition
                foreach (var p in typeof(FeedbackReportExport).GetProperties())
                {
                    feedbackRow.Add(p.Name, (p.GetValue(response, null)?.ToString() ?? ""));
                }
                // Add each individual response as a new cell
                foreach (var q in questionTemplate) {
                    feedbackRow.Add($"{q.Id} - {q.Prompt}", response.IdToAnswer.GetValueOrDefault(q.Id, ""));
                }
                results.Add(feedbackRow);
            }

            string challengeTag = "";
            if (model.WantsSpecificChallenge)
                challengeTag = (await ChallengeSpecService.Retrieve(model.ChallengeSpecId))?.Tag ?? "";
            
            string filename = Service.GetFeedbackFilename(game.Name, model.WantsGame, model.WantsSpecificChallenge, challengeTag, false);
        
            return File(
                Service.ConvertToBytes(results),
                "application/octet-stream",
                filename);
        }

        /// <summary>
        /// Export feedback stats to CSV
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpGet("api/report/exportfeedbackstats")]
        [Authorize]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        public async Task<IActionResult> ExportFeedbackStats([FromQuery] FeedbackSearchParams model)
        {
            AuthorizeAny(
                () => Actor.IsObserver
            );

            // gameId must be specified, even for challenge feedback, since template is stored in Game
            var game = await GameService.Retrieve(model.GameId);
            if (game == null || game.FeedbackTemplate == null)
                return NotFound();

            model.SubmitStatus = "submitted";
            model.Sort = "";
            var feedback = await FeedbackService.ListFull(model);

            var questionTemplate = FeedbackService.GetTemplate(model.WantsGame, game);
            if (questionTemplate == null)
                return NotFound();

            var expandedTable = FeedbackService.MakeHelperList(feedback);

            var result = Service.GetFeedbackQuestionStats(questionTemplate, expandedTable);

            string challengeTag = "";
            if (model.WantsSpecificChallenge)
                challengeTag = (await ChallengeSpecService.Retrieve(model.ChallengeSpecId))?.Tag ?? "";
            
            string filename = Service.GetFeedbackFilename(game.Name, model.WantsGame, model.WantsSpecificChallenge, challengeTag, true);

            return File(
                Service.ConvertToBytes(result),
                "application/octet-stream",
                filename);
        }

        [HttpGet("/api/report/feedbackstats")]
        [Authorize]
        public async Task<ActionResult<FeedbackStats>> GetFeedbackStats([FromQuery] FeedbackSearchParams model)
        {
            AuthorizeAny(
                () => Actor.IsObserver
            );

            // gameId must be specified, even for challenge feedback, since template is stored in Game
            var game = await GameService.Retrieve(model.GameId);
            if (game == null || game.FeedbackTemplate == null)
                return NotFound();

            model.SubmitStatus = ""; // at first get unsubmitted too, to count in progress vs submitted
            model.Sort = "";
            var feedback = await FeedbackService.ListFull(model);

            var questionTemplate = FeedbackService.GetTemplate(model.WantsGame, game);
            if (questionTemplate == null)
                return NotFound();

            var submittedFeedback = feedback.Where(f => f.Submitted).ToArray();
            var expandedTable = FeedbackService.MakeHelperList(submittedFeedback);
            var maxResponses = await Service.GetFeedbackMaxResponses(model);
            var questionStats = Service.GetFeedbackQuestionStats(questionTemplate, expandedTable);

            var fullStats = new FeedbackStats 
            {
                GameId = game.Id,
                ChallengeSpecId = model.ChallengeSpecId,
                ConfiguredCount = questionTemplate.Length,
                LikertCount = questionTemplate.Where(q => q.Type == "likert").Count(),
                TextCount = questionTemplate.Where(q => q.Type == "text").Count(),
                SelectOneCount = questionTemplate.Where(q => q.Type == "selectOne").Count(),
                SelectManyCount = questionTemplate.Where(q => q.Type == "selectMany").Count(),
                RequiredCount = questionTemplate.Where(q => q.Required).Count(),
                ResponsesCount = feedback.Length,
                MaxResponseCount = maxResponses,
                InProgressCount = feedback.Length - submittedFeedback.Length,
                SubmittedCount = submittedFeedback.Length,
                QuestionStats = questionStats
            };

            return Ok(fullStats);
        }

        [HttpGet("/api/report/supportdaystats")]
        [Authorize]
        public async Task<ActionResult<TicketDayGroup[]>> GetTicketVolumeStats([FromQuery] TicketReportFilter model)
        {
            AuthorizeAny(
                () => Actor.IsObserver
            );

            var tickets = await Service.GetTicketVolume(model);

            return Ok(tickets);
        }

        [HttpGet("/api/report/supportlabelstats")]
        [Authorize]
        public async Task<ActionResult<TicketLabelGroup[]>> GetTicketLabelStats([FromQuery] TicketReportFilter model)
        {
            AuthorizeAny(
                () => Actor.IsObserver
            );

            var tickets = await Service.GetTicketLabels(model);

            return Ok(tickets);
        }

        [HttpGet("/api/report/supportchallengestats")]
        [Authorize]
        public async Task<ActionResult<TicketChallengeGroup[]>> GetTicketChallengeStats([FromQuery] TicketReportFilter model)
        {
            AuthorizeAny(
                () => Actor.IsObserver
            );

            var tickets = await Service.GetTicketChallenges(model);

            return Ok(tickets);
        }

    }
}
