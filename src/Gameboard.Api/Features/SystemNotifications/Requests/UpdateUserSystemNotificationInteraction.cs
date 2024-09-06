using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.SystemNotifications;

public record UpdateUserSystemNotificationInteractionCommand(IEnumerable<string> SystemNotificationIds, InteractionType Type) : IRequest;

internal class UpdateUserSystemNotificationInteractionHandler(
    IActingUserService actingUserService,
    INowService nowService,
    IStore store,
    IValidatorService<UpdateUserSystemNotificationInteractionCommand> validatorService
    ) : IRequestHandler<UpdateUserSystemNotificationInteractionCommand>
{
    private readonly IActingUserService _actingUserService = actingUserService;
    private readonly INowService _nowService = nowService;
    private readonly IStore _store = store;
    private readonly IValidatorService<UpdateUserSystemNotificationInteractionCommand> _validatorService = validatorService;

    public async Task Handle(UpdateUserSystemNotificationInteractionCommand request, CancellationToken cancellationToken)
    {
        var actingUser = _actingUserService.Get();
        var nowish = _nowService.Get();

        _validatorService.AddValidator(async (req, ctx) =>
        {
            var existingIds = await _store
                .WithNoTracking<SystemNotification>()
                .Select(n => n.Id)
                .Where(nId => request.SystemNotificationIds.Contains(nId))
                .ToArrayAsync(cancellationToken);

            foreach (var id in request.SystemNotificationIds)
                if (!existingIds.Contains(id))
                    ctx.AddValidationException(new ResourceNotFound<SystemNotification>(id));
        });

        // can't dismiss undismissibles
        if (request.Type == InteractionType.Dismissed)
        {
            _validatorService.AddValidator(async (req, ctx) =>
            {
                var undismissibleIds = await _store
                    .WithNoTracking<SystemNotification>()
                    .Where(n => request.SystemNotificationIds.Contains(n.Id))
                    .Where(n => !n.IsDismissible)
                    .Select(n => n.Id)
                    .ToArrayAsync(cancellationToken);

                foreach (var undismissibleId in undismissibleIds)
                    ctx.AddValidationException(
                        new InvalidParameterValue<UpdateUserSystemNotificationInteractionCommand>
                        (
                            nameof(request.Type),
                            "Can't dismiss undismissible system notifications.",
                            request
                        )
                    );
            });
        }

        await _validatorService.Validate(request, cancellationToken);

        // if this is the user's first interaction with this notification, they may not have an interaction record yet.
        // if they don't, create it.
        foreach (var notificationId in request.SystemNotificationIds)
        {
            var interactionRecord = await _store
                .WithNoTracking<SystemNotificationInteraction>()
                .Where(i => i.UserId == actingUser.Id && i.SystemNotificationId == notificationId)
                .SingleOrDefaultAsync(cancellationToken);

            interactionRecord ??= await _store
                .Create(new SystemNotificationInteraction
                {
                    SystemNotificationId = notificationId,
                    UserId = actingUser.Id
                });
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
