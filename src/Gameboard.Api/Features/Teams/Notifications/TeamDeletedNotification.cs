using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Games.External;
using MediatR;

namespace Gameboard.Api.Features.Teams;

public sealed class TeamDeletedNotification : INotification
{
    public string TeamId { get; private set; }

    public TeamDeletedNotification(string teamId)
        => TeamId = teamId;
}

internal sealed class TeamDeletedHandler : INotificationHandler<TeamDeletedNotification>
{
    private readonly IExternalGameService _externalGameService;

    public TeamDeletedHandler(IExternalGameService externalGameService)
        => _externalGameService = externalGameService;

    public Task Handle(TeamDeletedNotification notification, CancellationToken cancellationToken)
        => _externalGameService.DeleteTeamExternalData(cancellationToken, notification.TeamId);
}
