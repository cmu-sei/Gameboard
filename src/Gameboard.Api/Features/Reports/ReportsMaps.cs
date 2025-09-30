// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using AutoMapper;

namespace Gameboard.Api.Features.Reports;

public class ReportsMaps : Profile
{
    public ReportsMaps()
    {
        CreateMap<SupportReportRecord, SupportReportExportRecord>()
            .ForMember(d => d.Attachments, opt => opt.MapFrom(s => string.Join("\n", s.AttachmentUris)))
            .ForMember(d => d.Labels, opt => opt.MapFrom(s => string.Join(", ", s.Labels)));
    }
}
