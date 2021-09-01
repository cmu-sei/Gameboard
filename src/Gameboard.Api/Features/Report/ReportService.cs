using System;
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

        internal ReportService (
            ILogger<ReportService> logger,
            IMapper mapper,
            CoreOptions options,
            GameboardDbContext store
        ): base (logger, mapper, options)
        {
            Store = store;
        }

        internal Task<SponsorReport> GetSponsorStats(string gameId)
        {
            var q = gameId.HasValue()
                ? Store.Players.Where(p => p.GameId == gameId).GroupBy(p => p.Sponsor)
                : Store.Users.GroupBy(u => u.Sponsor)
            ;

            return null;
        }
    }
}
