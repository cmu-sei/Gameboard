using AutoMapper;

namespace Gameboard.Api.Features.Reports;

public class ReportsMaps : Profile
{
    public ReportsMaps()
    {
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
