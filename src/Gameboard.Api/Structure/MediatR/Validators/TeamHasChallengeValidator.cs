using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.GameEngine;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Structure.MediatR.Validators;

internal class TeamHasChallengeValidator : IGameboardValidator<Player, TeamDoesntHaveChallenge>
{
    private readonly IChallengeStore _store;

    public TeamHasChallengeValidator(IChallengeStore store)
    {
        _store = store;
    }

    public async Task<TeamDoesntHaveChallenge> Validate(Player model)
    {
        var result = await _store
            .List()
            .Where(c => c.TeamId == model.Id)
            .FirstOrDefaultAsync();

        if (result == default)
        {
            return new TeamDoesntHaveChallenge(model.Id);
        }

        return null;
    }
}
