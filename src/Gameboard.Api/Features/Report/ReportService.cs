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
    }
}
