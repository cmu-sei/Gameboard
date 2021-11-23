using System;
using System.Collections.Generic;
using System.Globalization;
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
            GameService gameService
        ): base(logger, cache)
        {
            Service = service;
            GameService = gameService;
        }

        ReportService Service { get; }
        GameService GameService { get; }

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
                    string.Format("game-sponsor-report-{0}", DateTime.UtcNow.ToString("yyyy-MM-dd")) + ".csv");

            }
            else
            {
                List<Tuple<string, string>> gameSponsorStats = new List<Tuple<string, string>>();
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
                    string.Format("game-sponsor-report-{0}", DateTime.UtcNow.ToString("yyyy-MM-dd")) + ".csv");
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

            List<ChallengeStatsExport> challengeStats = new List<ChallengeStatsExport>();
            challengeStats.Add(new ChallengeStatsExport { ChallengeName = "Challenge", Tag = "Tag", Points = "Points", Attempts = "Attempts #", 
                Complete = "Complete(#/%)", Partial = "Partial(#/%)", AvgTime = "Avg Time", AvgScore = "Avg Score" });

            foreach (ChallengeStat challengeStat in result.Stats)
            {
                challengeStats.Add(new ChallengeStatsExport {
                    ChallengeName = challengeStat.Name, Tag = challengeStat.Tag, Points = challengeStat.Points.ToString(), Attempts = challengeStat.AttemptCount.ToString(), 
                    Complete = challengeStat.SuccessCount.ToString() + " / " + (challengeStat.SuccessCount / challengeStat.AttemptCount).ToString("P", CultureInfo.InvariantCulture),
                    Partial = challengeStat.PartialCount.ToString() + " / " + (challengeStat.PartialCount / challengeStat.AttemptCount).ToString("P", CultureInfo.InvariantCulture), 
                    AvgTime = challengeStat.AverageTime, AvgScore = challengeStat.AverageScore.ToString()});
            }

            return File(
                Service.ConvertToBytes(challengeStats),
                "application/octet-stream",
                string.Format("challenge-stats-report-{0}", DateTime.UtcNow.ToString("yyyy-MM-dd")) + ".csv");
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

            List<ChallengeDetailsExport> challengeDetails = new List<ChallengeDetailsExport>();
            challengeDetails.Add(new ChallengeDetailsExport { ChallengeName = "Challenge Name", Tag = "Tag", Question = "Question", Points = "Points / % of Total", Solves = 
                "Solves / % of Attempts Correct" });

            foreach (ChallengeStat stat in result.Stats)
            {
                var challengeDetail = await Service.GetChallengeDetails(stat.Id);

                foreach (Part part in challengeDetail.Parts)
                {
                    challengeDetails.Add(new ChallengeDetailsExport { ChallengeName = stat.Name, Tag = stat.Tag, Question = part.Text, 
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
    }
}
