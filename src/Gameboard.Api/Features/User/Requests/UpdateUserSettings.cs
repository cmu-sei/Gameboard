using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Users;

public sealed class UpdateUserSettingsRequest
{
    public bool? PlayAudioOnBrowserNotification { get; set; }
}

public record UpdateUserSettingsCommand(UpdateUserSettingsRequest Request) : IRequest<UserSettings>;

internal class UpdateUserSettingsHandler : IRequestHandler<UpdateUserSettingsCommand, UserSettings>
{
    private readonly ActingUserExistsValidator _actingUserExists;
    private readonly IActingUserService _actingUserService;
    private readonly IStore _store;
    private readonly IValidatorService _validatorService;

    public UpdateUserSettingsHandler
    (
        ActingUserExistsValidator actingUserExists,
        IActingUserService actingUserService,
        IStore store,
        IValidatorService validatorService
    )
    {
        _actingUserExists = actingUserExists;
        _actingUserService = actingUserService;
        _store = store;
        _validatorService = validatorService;
    }

    public async Task<UserSettings> Handle(UpdateUserSettingsCommand request, CancellationToken cancellationToken)
    {
        // validate
        await _validatorService
            .AddValidator(_actingUserExists)
            .Validate(cancellationToken);

        var userEntity = await _store
            .WithTracking<Data.User>()
            .SingleAsync(u => u.Id == _actingUserService.Get().Id);

        if (request.Request.PlayAudioOnBrowserNotification is not null)
        {
            userEntity.PlayAudioOnBrowserNotification = request.Request.PlayAudioOnBrowserNotification.Value;
        }

        await _store.SaveUpdate(userEntity, cancellationToken);

        return new UserSettings
        {
            PlayAudioOnBrowserNotification = userEntity.PlayAudioOnBrowserNotification
        };
    }
}
