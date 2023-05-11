using System;
using AutoMapper;
using Gameboard.Api.Structure;

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

        CreateMap<ChallengesReportRecord, ChallengesReportCsvRecord>()
            .ForMember(d => d.ChallengeSpecId, opt => opt.MapFrom(s => s.ChallengeSpec.Id))
            .ForMember(d => d.ChallengeSpecName, opt => opt.MapFrom(s => s.ChallengeSpec.Name))
            .ForMember(d => d.GameId, opt => opt.MapFrom(s => s.Game.Id))
            .ForMember(d => d.GameName, opt => opt.MapFrom(s => s.Game.Name))
            .ForMember(d => d.FastestSolvePlayerId, opt => opt.MapFrom(s => s.FastestSolve.Player.Id))
            .ForMember(d => d.FastestSolvePlayerName, opt => opt.MapFrom(s => s.FastestSolve.Player.Name))
            .ForMember(d => d.FastestSolveTimeMs, opt => opt.MapFrom(s => s.FastestSolve.SolveTimeMs));

        CreateMap<SupportReportRecord, SupportReportExportRecord>()
            .ForMember(d => d.Attachments, opt => opt.MapFrom(s => string.Join("\n", s.AttachmentUris)))
            .ForMember(d => d.Labels, opt => opt.MapFrom(s => string.Join(", ", s.Labels)));
    }
}
