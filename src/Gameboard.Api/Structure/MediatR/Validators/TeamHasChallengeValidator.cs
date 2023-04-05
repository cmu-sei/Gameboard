using System;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.GameEngine;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Structure.MediatR.Validators;

internal class TeamHasChallengeValidator : IGameboardValidator<Player>
{
    private readonly IChallengeStore _store;

    public TeamHasChallengeValidator(IChallengeStore store)
    {
        _store = store;
    }

    public Func<Player, RequestValidationContext, Task> GetValidationTask()
    {
        return async (player, context) =>
        {
            var result = await _store
           .List()
           .Where(c => c.TeamId == player.TeamId)
           .FirstOrDefaultAsync();

            if (result == default)
            {
                context.AddValidationException(new TeamDoesntHaveChallenge(player.TeamId));
            }
        };
    }
}
