using MediatR;

namespace Gameboard.Api.Features.Admin;

public sealed class GetSiteOverviewStatsResponse
{

}

public record GetSiteOverviewStatsQuery() : IRequest<GetSiteOverviewStatsResponse>;
