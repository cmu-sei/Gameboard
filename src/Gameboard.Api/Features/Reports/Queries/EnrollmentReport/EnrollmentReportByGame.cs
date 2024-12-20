using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public record EnrollmentReportByGameQuery(EnrollmentReportParameters Parameters, PagingArgs PagingArgs) : IRequest<ReportResults<EnrollmentReportByGameRecord>>, IReportQuery;

internal class EnrollmentReportByGameHandler
(
    IEnrollmentReportService enrollmentReportService,
    ReportsQueryValidator queryValidator,
    IReportsService reportsService
) : IRequestHandler<EnrollmentReportByGameQuery, ReportResults<EnrollmentReportByGameRecord>>
{
    private readonly IEnrollmentReportService _enrollmentReportService = enrollmentReportService;
    private readonly ReportsQueryValidator _queryValidator = queryValidator;
    private readonly IReportsService _reportsService = reportsService;

    public async Task<ReportResults<EnrollmentReportByGameRecord>> Handle(EnrollmentReportByGameQuery request, CancellationToken cancellationToken)
    {
        // validate
        await _queryValidator.Validate(request, cancellationToken);

        // get the dataset, slim it down and group it
        var rawResults = await _enrollmentReportService
            .GetBaseQuery(request.Parameters)
            .Select(p => new
            {
                Game = new EnrollmentReportByGameGame
                {
                    Id = p.GameId,
                    Name = p.Game.Name,
                    Season = p.Game.Season,
                    Series = p.Game.Division,
                    Track = p.Game.Track,
                    IsTeamGame = p.Game.MaxTeamSize > 1,
                    ExecutionClosed = p.Game.GameEnd,
                    ExecutionOpen = p.Game.GameStart,
                    RegistrationOpen = p.Game.RegistrationOpen,
                    RegistrationClosed = p.Game.RegistrationClose
                },
                Sponsor = new ReportSponsorViewModel
                {
                    Id = p.SponsorId,
                    Name = p.Sponsor.Name,
                    LogoFileName = p.Sponsor.Logo
                },
                PlayerId = p.Id,
                p.UserId,
            })
            .ToArrayAsync(cancellationToken);

        // now do the stuff we can't easily translate to the db level
        // this is just to make it easier to look up id/name/logo for hydration
        // below
        var allSponsors = rawResults
            .Select(p => p.Sponsor)
            .GroupBy(s => s.Id)
            .ToDictionary(s => s.Key, s =>
            {
                var first = s.First();

                return new ReportSponsorViewModel
                {
                    Id = s.Key,
                    Name = first.Name,
                    LogoFileName = first.LogoFileName
                };
            });

        // how many total humans played the game?
        var gamePlayerCount = rawResults
            .GroupBy(g => g.Game.Id)
            .ToDictionary(gr => gr.Key, gr => gr.DistinctBy(p => p.UserId).Count());

        // how many humans per sponsor per game?
        var gameSponsorPlayerCount = rawResults
            .GroupBy(g => new { GameId = g.Game.Id, SponsorId = g.Sponsor.Id })
            .Select(gr => new
            {
                gr.Key.GameId,
                gr.Key.SponsorId,
                PlayerCount = gr.DistinctBy(result => result.UserId).Count()
            })
            .OrderBy(entry => entry.GameId)
                .ThenByDescending(entry => entry.PlayerCount);

        var groupedResults = rawResults
            .GroupBy(p => p.Game.Id)
            .Select(gr =>
            {
                // all the game info should be the same, we just need
                // a single instance to return the gameinfo
                var gameInfo = gr.First();
                var thisGameSponsors = gameSponsorPlayerCount
                    .Where(c => c.GameId == gr.Key)
                    .OrderByDescending(s => s.PlayerCount);
                var topSponsor = thisGameSponsors.First();

                return new EnrollmentReportByGameRecord
                {
                    Game = gameInfo.Game,
                    PlayerCount = gamePlayerCount[gr.Key],
                    SponsorCount = thisGameSponsors.Count(),
                    Sponsors = thisGameSponsors.Select(s => new EnrollmentReportByGameSponsor
                    {
                        Id = s.SponsorId,
                        Name = allSponsors[s.SponsorId].Name,
                        LogoFileName = allSponsors[s.SponsorId].LogoFileName,
                        PlayerCount = s.PlayerCount
                    }),
                    TopSponsor = new EnrollmentReportByGameSponsor
                    {
                        Id = allSponsors[topSponsor.SponsorId].Id,
                        Name = allSponsors[topSponsor.SponsorId].Name,
                        LogoFileName = allSponsors[topSponsor.SponsorId].LogoFileName,
                        PlayerCount = topSponsor.PlayerCount
                    }
                };
            })
            .OrderByDescending(record => record.PlayerCount);

        if (request.Parameters.Sort.IsNotEmpty())
        {
            var sortDirection = request.Parameters.SortDirection;

            switch (request.Parameters.Sort)
            {
                case "count-players":
                    groupedResults = groupedResults.Sort(r => r.PlayerCount, sortDirection);
                    break;
                case "count-sponsors":
                    groupedResults = groupedResults.Sort(r => r.SponsorCount, sortDirection);
                    break;
                case "execution-end":
                    groupedResults = groupedResults.Sort(r => r.Game.ExecutionClosed, sortDirection);
                    break;
                case "execution-start":
                    groupedResults = groupedResults.Sort(r => r.Game.ExecutionOpen);
                    break;
                case "game":
                    groupedResults = groupedResults.Sort(r => r.Game.Name, sortDirection);
                    break;
                case "registration-close":
                    groupedResults = groupedResults.Sort(r => r.Game.RegistrationClosed);
                    break;
                case "registration-open":
                    groupedResults = groupedResults.Sort(r => r.Game.RegistrationOpen);
                    break;
                case "top-sponsor":
                    groupedResults = groupedResults.Sort(r => r.TopSponsor.Name, sortDirection);
                    break;
            }

            groupedResults = groupedResults
                .ThenBy(r => r.Game.Name);
        }

        return _reportsService.BuildResults(new ReportRawResults<EnrollmentReportByGameRecord>
        {
            ParameterSummary = string.Empty,
            PagingArgs = request.PagingArgs,
            Key = ReportKey.Enrollment,
            Description = await _reportsService.GetDescription(ReportKey.Enrollment),
            Records = groupedResults,
            Title = "Enrollment Report (Grouped By Game)"
        });
    }
}
