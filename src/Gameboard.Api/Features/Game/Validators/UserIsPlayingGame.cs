using System;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Games.Validators;

public class UserIsPlayingGameValidator : IGameboardValidator
{
    private string _gameId;
    private string _userId;
    private readonly GameService _gameService;

    public UserIsPlayingGameValidator(GameService gameService)
    {
        _gameService = gameService;
    }

    public UserIsPlayingGameValidator UseValues(string gameId, string userId)
    {
        _gameId = gameId;
        _userId = userId;

        return this;
    }

    public Func<RequestValidationContext, Task> GetValidationTask()
    {
        return async (ctx) =>
        {
            var game = await _gameService
                .BuildQuery()
                .AsNoTracking()
                .Include(g => g.Players)
                .FirstOrDefaultAsync(g => g.Id == _gameId);

            if (game == null)
                ctx.AddValidationException(new ResourceNotFound<Data.Game>(_gameId));

            if (!game.Players.Any(p => p.UserId == _userId))
                ctx.AddValidationException(new UserIsntPlayingGame(_userId, _gameId));
        };
    }
}
