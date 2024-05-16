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

public record EnrollmentReportLineChartQuery(EnrollmentReportParameters Parameters) : IRequest<IDictionary<DateTimeOffset, EnrollmentReportLineChartGroup>>, IReportQuery;

internal class EnrollmentReportLineChartHandler : IRequestHandler<EnrollmentReportLineChartQuery, IDictionary<DateTimeOffset, EnrollmentReportLineChartGroup>>
{
    private readonly IEnrollmentReportService _reportService;
    private readonly ReportsQueryValidator _reportsQueryValidator;
    private readonly IGameboardRequestValidator<EnrollmentReportParameters> _validator;

    public EnrollmentReportLineChartHandler
    (
        IEnrollmentReportService reportService,
        ReportsQueryValidator reportsQueryValidator,
        IGameboardRequestValidator<EnrollmentReportParameters> validator
    )
    {
        _reportService = reportService;
        _reportsQueryValidator = reportsQueryValidator;
        _validator = validator;
    }

    public async Task<IDictionary<DateTimeOffset, EnrollmentReportLineChartGroup>> Handle(EnrollmentReportLineChartQuery request, CancellationToken cancellationToken)
    {
        // authorize/validate
        await _reportsQueryValidator.Validate(request, cancellationToken);
        await _validator.Validate(request.Parameters, cancellationToken);

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
        var retVal = new Dictionary<DateTimeOffset, EnrollmentReportLineChartGroup>();

        foreach (var grouping in results.GroupBy(p => new DateTimeOffset(p.EnrollDate.Year, p.EnrollDate.Month, p.EnrollDate.Day, 0, 0, 0, p.EnrollDate.Offset)))
        {
            totalEnrolledPlayerCount += grouping.Count();
            retVal[grouping.Key] = new EnrollmentReportLineChartGroup
            {
                Players = grouping,
                TotalCount = totalEnrolledPlayerCount
            };
        }

        return retVal;
    }
}
