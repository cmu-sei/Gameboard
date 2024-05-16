using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Users;

public sealed class UpdateUserLoginEventsResult
{
    public DateTimeOffset CurrentLoginDate { get; set; }
    public DateTimeOffset? LastLoginDate { get; set; }
}

public record UpdateUserLoginEventsCommand(string UserId) : IRequest<UpdateUserLoginEventsResult>;

internal class UpdateUserLoginEventsHandler : IRequestHandler<UpdateUserLoginEventsCommand, UpdateUserLoginEventsResult>
{
    private readonly INowService _now;
    private readonly IStore _store;
    private readonly EntityExistsValidator<UpdateUserLoginEventsCommand, Data.User> _userExists;
    private readonly IValidatorService<UpdateUserLoginEventsCommand> _validator;

    public UpdateUserLoginEventsHandler
    (
        INowService now,
        IStore store,
        EntityExistsValidator<UpdateUserLoginEventsCommand, Data.User> userExists,
        IValidatorService<UpdateUserLoginEventsCommand> validator
    )
    {
        _now = now;
        _store = store;
        _userExists = userExists;
        _validator = validator;
    }

    public async Task<UpdateUserLoginEventsResult> Handle(UpdateUserLoginEventsCommand request, CancellationToken cancellationToken)
    {
        // validate
        _validator.AddValidator(_userExists.UseProperty(r => r.UserId));
        await _validator.Validate(request, cancellationToken);

        var user = await _store.Retrieve<Data.User>(request.UserId);
        var lastLoginDate = user.LastLoginDate;
        var currentLoginDate = _now.Get();

        await _store
            .WithNoTracking<Data.User>()
            .Where(u => u.Id == user.Id)
            .ExecuteUpdateAsync
            (
                u => u
                    .SetProperty(u => u.LoginCount, user.LoginCount + 1)
                    .SetProperty(u => u.LastLoginDate, currentLoginDate)
            );

        return new UpdateUserLoginEventsResult
        {
            LastLoginDate = lastLoginDate,
            CurrentLoginDate = currentLoginDate
        };
    }
}
