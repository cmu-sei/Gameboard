using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Services
{
    public class ReportService : _Service
    {
        GameboardDbContext Store { get; }

        public ReportService (
            ILogger<ReportService> logger,
            IMapper mapper,
            CoreOptions options,
            GameboardDbContext store
        ): base (logger, mapper, options)
        {
            Store = store;
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
                     select new PlayerStat { GameId = games.Id, GameName = games.Name, PlayerCount = games.Players.Count };

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
                      .Select(g => new SponsorStat { Id = g.Key.Id, Name = g.Key.Name, Logo = g.Key.Logo, Count = g.Count() });

            SponsorReport sponsorReport = new SponsorReport
            {
                Timestamp = DateTime.UtcNow,
                Stats = sp.ToArray()
            };

            return Task.FromResult(sponsorReport);
        }

        internal Task<GameSponsorReport> GetGameSponsorsStats(string[] gameIds)
        {
            List<GameSponsorStat> gameSponsorStats = new List<GameSponsorStat>();

            if (gameIds == null || gameIds.Length == 0)
            {
                gameIds = Store.Games.Select(g => g.Id).ToArray();
            }

            var games = Store.Games.Where(g => gameIds.Contains(g.Id)).Select(g => new
            {
                g.Id,
                g.Name
            }).ToDictionary(pair => pair.Id, pair => pair.Name);

            foreach (var gameId in gameIds)
            {
                var sp = (from sponsors in Store.Sponsors
                          join p in Store.Players on
                          sponsors.Logo equals p.Sponsor
                          join game in Store.Games on
                          p.GameId equals game.Id
                          where p.GameId == gameId
                          select new { sponsors.Id, sponsors.Name, sponsors.Logo }).GroupBy(s => new { s.Id, s.Name, s.Logo })
                          .Select(g => new SponsorStat { Id = g.Key.Id, Name = g.Key.Name, Logo = g.Key.Logo, Count = g.Count() });

                GameSponsorStat gameSponsorStat = new GameSponsorStat
                {
                    GameId = gameId,
                    GameName = games[gameId],
                    Stats = (SponsorStat[])sp.ToArray()
                };

                gameSponsorStats.Add(gameSponsorStat);
            }

            GameSponsorReport sponsorReport = new GameSponsorReport
            {
                Timestamp = DateTime.UtcNow,
                Stats = gameSponsorStats.ToArray()
            };

            return Task.FromResult(sponsorReport);
        }

        internal Task<TeamReport> GetTeamStats()
        {
            List<SponsorStat> sponsorStats = new List<SponsorStat>();
            var sponsors = Store.Sponsors.ToList();

            foreach (Data.Sponsor sponsor in sponsors)
            {
                var teamCount = Store.Players.Where(p => p.Sponsor == sponsor.Logo).Select(p => p.TeamId).Distinct().Count();

                sponsorStats.Add(new SponsorStat
                {
                     Id = sponsor.Id,
                     Logo = sponsor.Logo,
                     Name = sponsor.Name,
                     Count = teamCount
                });
            }

            TeamReport teamReport = new TeamReport();
            teamReport.Timestamp = DateTime.UtcNow;
            // we get the distinct team count separately because it is possible that some teams are made
            // up of members from multiple sponsors
            teamReport.TotalTeamCount = Store.Players.Select(p => p.TeamId).Distinct().Count();
            teamReport.Stats = sponsorStats.ToArray();

            return Task.FromResult(teamReport);
        }
    }
}
