using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Users;

public record GetUserSettingsQuery : IRequest<UserSettings>;

internal class GetUserSettingsHandler : IRequestHandler<GetUserSettingsQuery, UserSettings>
{
    private readonly ActingUserExistsValidator _actingUserExists;
    private readonly IActingUserService _actingUserService;
    private readonly IStore _store;
    private readonly IValidatorService _validatorService;

    public GetUserSettingsHandler
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

    public async Task<UserSettings> Handle(GetUserSettingsQuery request, CancellationToken cancellationToken)
    {
        // validate
        await _validatorService
            .AddValidator(_actingUserExists)
            .Validate(cancellationToken);

        var userEntity = await _store
            .WithNoTracking<Data.User>()
            .SingleAsync(u => u.Id == _actingUserService.Get().Id, cancellationToken);

        return new UserSettings
        {
            PlayAudioOnBrowserNotification = userEntity.PlayAudioOnBrowserNotification
        };
    }
}
