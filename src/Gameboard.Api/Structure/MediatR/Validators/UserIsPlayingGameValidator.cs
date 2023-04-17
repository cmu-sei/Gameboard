using System;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.Games;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Structure.MediatR.Validators;

internal class UserIsPlayingGameValidator<T> : IGameboardValidator<T>
{
    private readonly IPlayerStore _store;

    public required Func<T, string> GetGameId { get; set; }
    public required Func<T, User> GetUser { get; set; }

    public UserIsPlayingGameValidator(IPlayerStore store)
    {
        _store = store;
    }

    public Func<T, RequestValidationContext, Task> GetValidationTask()
    {
        return async (model, context) =>
        {
            var user = GetUser(model);
            var gameId = GetGameId(model);

            if (user.Role.HasFlag(UserRole.Admin) || user.Role.HasFlag(UserRole.Tester))
                return;

            var allplayers = await _store.List().ToListAsync();

            var playerGameCount = await _store
                .List()
                .AsNoTracking()
                .Where(p => p.UserId == user.Id && p.GameId == gameId)
                .CountAsync();

            if (playerGameCount == 0)
                context.AddValidationException(new UserIsntPlayingGame(user.Id, gameId, "User must be playing a game in order to read its sync start state."));
        };
    }
}
