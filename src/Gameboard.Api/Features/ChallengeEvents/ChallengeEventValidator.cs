using System;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Features.ChallengeEvents;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Validators;

public class ChallengeEventValidator : IModelValidator
{
    private readonly IChallengeEventStore _store;

    public ChallengeEventValidator(IChallengeEventStore store)
    {
        _store = store;
    }

    public async Task Validate(object model)
    {
        if (model is NewChallengeEvent)
            await ValidateNewChallengeEvent(model as NewChallengeEvent);

        throw new NotImplementedException();
    }

    private async Task<bool> ValidateNewChallengeEvent(NewChallengeEvent model)
    {
        return
            await ChallengeExists(model.ChallengeId) &&
            await UserExists(model.UserId) &&
            await TeamExists(model);
    }

    private async Task<bool> ChallengeExists(string id)
        => id.NotEmpty() && (await _store.DbContext.Challenges.FindAsync(id)) is Data.Challenge;

    private async Task<bool> TeamExists(NewChallengeEvent model)
        => model.TeamId.NotEmpty() && (
            await _store
                .DbContext.Players
                .Where(p => p.UserId == model.UserId && p.TeamId == model.TeamId)
                .FirstOrDefaultAsync()
            ) != null;

    private async Task<bool> UserExists(string id)
        => id.NotEmpty() && (await _store.DbContext.Users.FindAsync(id)) is Data.User;
}