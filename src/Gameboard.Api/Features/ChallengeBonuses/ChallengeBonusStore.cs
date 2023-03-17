using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.ChallengeBonuses;

public interface IChallengeBonusStore
{
    Task AddManualBonus(string challengeId, CreateManualChallengeBonus model, User actor);
    Task DeleteManualBonus(string id);
    Task UpdateManualBonus(UpdateManualChallengeBonus model, User actor);
    IQueryable<ManualChallengeBonus> List();
}

internal class ChallengeBonusStore : IChallengeBonusStore
{
    private readonly GameboardDbContext _db;
    private readonly IGuidService _guids;

    public ChallengeBonusStore(GameboardDbContext db, IGuidService guids)
    {
        _db = db;
        _guids = guids;
    }

    public async Task AddManualBonus(string challengeId, CreateManualChallengeBonus model, User actor)
    {
        _db.ManualChallengeBonuses.Add(new ManualChallengeBonus
        {
            Id = _guids.GetGuid(),
            Description = model.Description,
            PointValue = model.PointValue,
            EnteredByUserId = actor.Id,
            ChallengeId = challengeId
        });

        await _db.SaveChangesAsync();
    }

    public async Task DeleteManualBonus(string id)
        => await _db
            .ManualChallengeBonuses
            .Where(b => b.Id == id)
            .ExecuteDeleteAsync();

    public IQueryable<ManualChallengeBonus> List()
        => _db
            .ManualChallengeBonuses
            .AsNoTracking()
            .Include(c => c.EnteredBy);

    public Task UpdateManualBonus(UpdateManualChallengeBonus model, User actor)
    {
        throw new System.NotImplementedException();
    }
}
