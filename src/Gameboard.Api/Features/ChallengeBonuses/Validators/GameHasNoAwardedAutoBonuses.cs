using System;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.ChallengeBonuses;

internal class GameHasNoAwardedAutoBonuses<TModel> : IGameboardValidator<TModel>
{
    private readonly IChallengeBonusStore _store;
    private Func<TModel, string> _propertyExpression;

    public GameHasNoAwardedAutoBonuses(IChallengeBonusStore store)
    {
        _store = store;
    }

    public GameHasNoAwardedAutoBonuses<TModel> UseProperty(Func<TModel, string> propertyExpression)
    {
        _propertyExpression = propertyExpression;
        return this;
    }

    public Func<TModel, RequestValidationContext, Task> GetValidationTask()
    {
        return async (model, ctx) =>
        {
            var gameId = _propertyExpression.Invoke(model);

            var awardedBonusIds = await _store
                .DbContext
                .AwardedChallengeBonuses
                .AsNoTracking()
                .Include(ab => ab.ChallengeBonus)
                    .ThenInclude(b => b.ChallengeSpec)
                .Where(b => b.ChallengeBonus.ChallengeSpec.GameId == gameId)
                .Select(ab => ab.ChallengeBonusId)
                .ToArrayAsync();

            var awarded = await _store.DbContext
                .AwardedChallengeBonuses
                .AsNoTracking()
                .Include(ab => ab.ChallengeBonus)
                    .ThenInclude(b => b.ChallengeSpec)
                .ToArrayAsync();

            if (awardedBonusIds.Count() > 0)
                ctx.AddValidationException(new CantDeleteAutoBonusIfAwarded(gameId, awardedBonusIds));
        };
    }
}
