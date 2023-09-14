using System;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.Games;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Structure.MediatR.Validators;

internal class UserIsPlayingGameValidator<T> : IGameboardValidator<T> where T : class
{
    private Func<T, string> _gameIdExpression;
    private Func<T, User> _userExpression;
    private readonly IPlayerStore _store;

    public UserIsPlayingGameValidator(IPlayerStore store)
    {
        _store = store;
    }

    public UserIsPlayingGameValidator<T> UseGameIdProperty(Func<T, string> gameIdPropertyExpression)
    {
        _gameIdExpression = gameIdPropertyExpression;
        return this;
    }

    public UserIsPlayingGameValidator<T> UseUserIdProperty(Func<T, User> userPropertyExpression)
    {
        _userExpression = userPropertyExpression;
        return this;
    }

    public Func<T, RequestValidationContext, Task> GetValidationTask()
    {
        return async (model, context) =>
        {
            var user = _userExpression(model);
            var gameId = _gameIdExpression(model);

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
