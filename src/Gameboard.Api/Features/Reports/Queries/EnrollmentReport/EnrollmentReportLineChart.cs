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
            request.Parameters.TrendPeriod = EnrollmentReportLineChartPeriod.All;

        // pull base query but select only what we need
        var results = await _reportService
            .GetBaseQuery(request.Parameters)
            .Select(p => new EnrollmentReportLineChartPlayer
            {
                Id = p.Id,
                Name = p.ApprovedName,
                EnrollDate = p.WhenCreated,
                Game = new SimpleEntity { Id = p.GameId, Name = p.Game.Id },
            })
            .WhereDateIsNotEmpty(p => p.EnrollDate)
            .OrderBy(p => p.EnrollDate)
            .ToListAsync(cancellationToken);

        // grouping stuff
        var totalEnrolledPlayerCount = 0;
        var playerGroups = new Dictionary<DateTimeOffset, EnrollmentReportLineChartGroup>();

        foreach (var grouping in results.GroupBy(p => new DateTimeOffset(p.EnrollDate.Year, p.EnrollDate.Month, p.EnrollDate.Day, 0, 0, 0, p.EnrollDate.Offset)))
        {
            totalEnrolledPlayerCount += grouping.Count();
            playerGroups[grouping.Key] = new EnrollmentReportLineChartGroup
            {
                Players = grouping,
                TotalCount = totalEnrolledPlayerCount
            };
        }

        return new EnrollmentReportLineChartResponse
        {
            PeriodEnd = results.Select(p => p.EnrollDate).Max(),
            PeriodStart = results.Select(p => p.EnrollDate).Min(),
            PeriodType = request.Parameters.TrendPeriod.Value,
            PlayerGroups = playerGroups
        };
    }
}
