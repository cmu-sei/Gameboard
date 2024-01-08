using System;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Structure.MediatR.Validators;

public class ActingUserExistsValidator : IGameboardValidator
{
    private readonly IActingUserService _actingUserService;
    private readonly IStore _store;

    public ActingUserExistsValidator
    (
        IActingUserService actingUserService,
        IStore store
    )
    {
        _actingUserService = actingUserService;
        _store = store;
    }

    public Func<RequestValidationContext, Task> GetValidationTask()
    {
        var actingUserId = _actingUserService.Get().Id;

        return async ctx =>
        {
            var exists = await _store
                    .WithNoTracking<Data.User>()
                    .AnyAsync(u => u.Id == actingUserId);

            if (!exists)
                ctx.AddValidationException(new ResourceNotFound<Data.User>(actingUserId));
        };
    }
}
