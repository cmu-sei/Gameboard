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
