using System;
using AutoMapper;

namespace Gameboard.Api.Features.Reports;

public class ReportsMaps : Profile
{
    private static string LIST_FIELD_DELIMITER = "|";

    public ReportsMaps()
    {
        CreateMap<Report, ReportViewModel>()
            .ForMember
            (
                r => r.ExampleFields,
                opt => opt.MapFrom(s => s.ExampleFields.Split(ReportsMaps.LIST_FIELD_DELIMITER, StringSplitOptions.RemoveEmptyEntries))
            )
            .ForMember
            (
                r => r.ExampleParameters,
                opt => opt.MapFrom(s => s.ExampleParameters.Split(ReportsMaps.LIST_FIELD_DELIMITER, StringSplitOptions.RemoveEmptyEntries))
            );
    }
}
