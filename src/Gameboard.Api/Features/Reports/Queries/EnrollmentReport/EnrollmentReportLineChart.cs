using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public record EnrollmentReportLineChartQuery(EnrollmentReportParameters Parameters) : IRequest<EnrollmentReportLineChartResponse>, IReportQuery;

internal class EnrollmentReportLineChartHandler(
    IEnrollmentReportService reportService,
    ReportsQueryValidator reportsQueryValidator,
    IGameboardRequestValidator<EnrollmentReportParameters> validator
    ) : IRequestHandler<EnrollmentReportLineChartQuery, EnrollmentReportLineChartResponse>
{
    private readonly IEnrollmentReportService _reportService = reportService;
    private readonly ReportsQueryValidator _reportsQueryValidator = reportsQueryValidator;
    private readonly IGameboardRequestValidator<EnrollmentReportParameters> _validator = validator;

    public async Task<EnrollmentReportLineChartResponse> Handle(EnrollmentReportLineChartQuery request, CancellationToken cancellationToken)
    {
        // authorize/validate
        await _reportsQueryValidator.Validate(request, cancellationToken);
        await _validator.Validate(request.Parameters, cancellationToken);

        if (request.Parameters.TrendPeriod is null)
        {
            request.Parameters.TrendPeriod = EnrollmentReportLineChartPeriod.All;
        }

        // pull base query but select only what we need
        var results = await _reportService
            .GetBaseQuery(request.Parameters)
            .Select(p => new EnrollmentReportLineChartPlayer
            {
                Id = p.Id,
                Name = p.ApprovedName,
                EnrollDate = p.WhenCreated,
                Game = new SimpleEntity { Id = p.GameId, Name = p.Game.Name },
            })
            .WhereDateIsNotEmpty(p => p.EnrollDate)
            .OrderBy(p => p.EnrollDate)
            .ToArrayAsync(cancellationToken);

        // we send down data by game and just by date, so let's store the player and game data in one place so we don't duplicate it
        var gameMap = results.Select(p => p.Game).DistinctBy(g => g.Id).OrderBy(g => g.Name).ToDictionary(g => g.Id, g => g.Name);
        var byDate = new Dictionary<DateTimeOffset, int>();
        var byGameByDate = new Dictionary<string, Dictionary<DateTimeOffset, int>>();
        var distinctRegistrationDates = results
            .Select(p => new DateTimeOffset(p.EnrollDate.Year, p.EnrollDate.Month, p.EnrollDate.Day, 0, 0, 0, p.EnrollDate.Offset))
            .Distinct()
            .ToArray();

        foreach (var distinctRegistrationDate in distinctRegistrationDates)
        {
            var registeredPlayers = results
            .Where(p => new DateTimeOffset(p.EnrollDate.Year, p.EnrollDate.Month, p.EnrollDate.Day, 0, 0, 0, p.EnrollDate.Offset) <= distinctRegistrationDate)
            .Select(p => new EnrollmentReportLineChartPlayerGame { Id = p.Id, GameId = p.Game.Id });

            byDate.Add(distinctRegistrationDate, registeredPlayers.Count());

            foreach (var gameId in gameMap.Keys)
            {
                byGameByDate.EnsureKey(gameId, []);
                byGameByDate[gameId].Add
                (
                    distinctRegistrationDate,
                    registeredPlayers.Where(p => p.GameId == gameId).Count()
                );
            }
        }

        return new EnrollmentReportLineChartResponse
        {
            PeriodEnd = results.Select(p => p.EnrollDate).Max(),
            PeriodStart = results.Select(p => p.EnrollDate).Min(),
            PeriodType = request.Parameters.TrendPeriod.Value,
            Games = gameMap,
            // Players = results.DistinctBy(p => p.Id).ToDictionary(p => p.Id, p => p),
            ByDate = byDate,
            ByGameByDate = byGameByDate
        };
    }
}
