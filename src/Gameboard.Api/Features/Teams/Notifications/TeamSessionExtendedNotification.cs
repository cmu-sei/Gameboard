using System;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games.External;
using MediatR;

namespace Gameboard.Api.Features.Teams;

public record TeamSessionExtendedNotification(string TeamId, DateTimeOffset NewSessionEnd) : INotification;

internal class TeamSessionExtendedHandler : INotificationHandler<TeamSessionExtendedNotification>
{
    private readonly IExternalGameService _externalGameService;
    private readonly IGamebrainService _gamebrainService;
    private readonly IStore _store;

    public TeamSessionExtendedHandler
    (
        IExternalGameService externalGameService,
        IGamebrainService gamebrainService,


    )
    {
        _externalGameService = externalGameService;
        _gamebrainService = game
    }

    public Task Handle(TeamSessionExtendedNotification notification, CancellationToken cancellationToken)
    {

    }
}
