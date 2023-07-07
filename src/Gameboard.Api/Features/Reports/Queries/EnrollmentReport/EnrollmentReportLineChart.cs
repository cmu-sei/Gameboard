using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR.Authorizers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public record EnrollmentReportLineChartQuery(EnrollmentReportParameters Parameters) : IRequest<IDictionary<DateTimeOffset, EnrollmentReportLineChartGroup>>;

internal class EnrollmentReportLineChartHandler : IRequestHandler<EnrollmentReportLineChartQuery, IDictionary<DateTimeOffset, EnrollmentReportLineChartGroup>>
{
    private readonly UserRoleAuthorizer _authorizer;
    private readonly IEnrollmentReportService _reportService;
    private readonly EnrollmentReportValidator _validator;

    public EnrollmentReportLineChartHandler
    (
        UserRoleAuthorizer authorizer,
        IEnrollmentReportService reportService,
        EnrollmentReportValidator validator
    )
    {
        _authorizer = authorizer;
        _reportService = reportService;
        _validator = validator;
    }

    public async Task<IDictionary<DateTimeOffset, EnrollmentReportLineChartGroup>> Handle(EnrollmentReportLineChartQuery request, CancellationToken cancellationToken)
    {
        // authorize/validate
        _authorizer.AllowedRoles = new UserRole[] { UserRole.Admin, UserRole.Director, UserRole.Support };
        _authorizer.Authorize();
        await _validator.Validate(request.Parameters);

        // pull base query but select only what we need
        var query = await _reportService.GetBaseQuery(request.Parameters, cancellationToken);
        var results = await query.Select(p => new EnrollmentReportLineChartPlayer
        {
            Id = p.Id,
            Name = p.ApprovedName,
            EnrollDate = p.WhenCreated,
            Game = new SimpleEntity { Id = p.GameId, Name = p.Game.Id },
        })
        .WhereDateHasValue(p => p.EnrollDate)
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
