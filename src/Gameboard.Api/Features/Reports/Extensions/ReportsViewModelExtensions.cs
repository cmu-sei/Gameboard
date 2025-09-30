// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Gameboard.Api.Features.Reports;

public static class ReportsViewModelExtensions
{
    public static ReportSponsorViewModel ToReportViewModel(this Data.Sponsor sponsor)
    {
        if (sponsor is null)
            return null;

        return new()
        {
            Id = sponsor.Id,
            Name = sponsor.Name,
            LogoFileName = sponsor.Logo
        };
    }
}
