using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Gameboard.Api.Features.Teams;

public sealed class AdminEnrollTeamResponse
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required SimpleEntity Game { get; set; }
    public required PlayerWithSponsor Players { get; set; }
}

public record AdminEnrollTeamRequest(string GameId, IEnumerable<string> UserIds, string CaptainUserId = null) : IRequest<AdminEnrollTeamResponse>;

internal class AdminEnrollTeamHandler : IRequestHandler<AdminEnrollTeamRequest, AdminEnrollTeamResponse>
{
    public Task<AdminEnrollTeamResponse> Handle(AdminEnrollTeamRequest request, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}
