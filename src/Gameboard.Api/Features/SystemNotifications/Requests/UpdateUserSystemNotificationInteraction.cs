using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR.Authorizers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.SystemNotifications;

public record UpdateUserSystemNotificationInteractionCommand(IEnumerable<string> SystemNotificationIds, InteractionType Type) : IRequest;

internal class UpdateUserSystemNotificationInteractionHandler : IRequestHandler<UpdateUserSystemNotificationInteractionCommand>
{
    private readonly IActingUserService _actingUserService;
    private readonly INowService _nowService;
    private readonly IStore _store;

    public UpdateUserSystemNotificationInteractionHandler
    (
        IActingUserService actingUserService,
        INowService nowService,
        IStore store
    )
    {
        _actingUserService = actingUserService;
        _nowService = nowService;
        _store = store;
    }

    public async Task Handle(UpdateUserSystemNotificationInteractionCommand request, CancellationToken cancellationToken)
    {
        var actingUser = _actingUserService.Get();
        var nowish = _nowService.Get();

        // if this is the user's first interaction with this notification, they may not have an interaction record yet.
        // if they don't, create it.

        foreach (var notificationId in request.SystemNotificationIds)
        {
            var interactionRecord = await _store
                .WithNoTracking<SystemNotificationInteraction>()
                .Where(i => i.UserId == actingUser.Id && i.SystemNotificationId == notificationId)
                .SingleOrDefaultAsync(cancellationToken);

            if (interactionRecord is null)
            {
                interactionRecord = await _store
                    .Create(new SystemNotificationInteraction
                    {
                        SystemNotificationId = notificationId,
                        UserId = actingUser.Id
                    });
            }
        }

        await _store
                .WithNoTracking<SystemNotificationInteraction>()
                .Where(n => request.SystemNotificationIds.Contains(n.SystemNotificationId))
                .Where(n => n.UserId == actingUser.Id)
                .ExecuteUpdateAsync
                (
                    up => up
                        .SetProperty(i => i.DismissedOn, i => request.Type == InteractionType.Dismissed ? nowish : i.DismissedOn)
                        .SetProperty(i => i.SawCalloutOn, i => request.Type == InteractionType.SawCallout ? nowish : i.SawCalloutOn)
                        .SetProperty(i => i.SawFullNotificationOn, i => request.Type == InteractionType.SawFull ? nowish : i.SawFullNotificationOn),

                    cancellationToken
                );
    }
}
