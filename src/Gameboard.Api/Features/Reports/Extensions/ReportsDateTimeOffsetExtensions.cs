using System;

namespace Gameboard.Api.Features.Reports;

public static class ReportsDateTimeOffsetExtensions
{
    // an end date of, say, 12/25/23 should really be 12/25/23 23:59:59.999 or whatever
    public static DateTimeOffset ToEndDate(this DateTimeOffset date)
        => date.AddDays(1).Date.AddMilliseconds(-1);
}
