using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.ChallengeBonuses;

public interface IChallengeBonusStore
{
    Task AddManualBonus(CreateManualChallengeBonus model, User actor);
    Task DeleteManualBonus(string id);
    Task UpdateManualBonus(UpdateManualChallengeBonus model, User actor);
    Task<IEnumerable<ManualChallengeBonus>> ListManualBonuses(string challengeId);
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

    public async Task AddManualBonus(CreateManualChallengeBonus model, User actor)
    {
        _db.ManualChallengeBonuses.Add(new ManualChallengeBonus
        {
            Id = _guids.GetGuid(),
            Description = model.Description,
            PointValue = model.PointValue,
            EnteredByUserId = actor.Id,
            ChallengeId = model.ChallengeId
        });

        await _db.SaveChangesAsync();
    }

    public async Task DeleteManualBonus(string id)
        => await _db
            .ManualChallengeBonuses
            .Where(b => b.Id == id)
            .ExecuteDeleteAsync();

    public async Task<IEnumerable<ManualChallengeBonus>> ListManualBonuses(string challengeId)
        => await _db
            .ManualChallengeBonuses
            .AsNoTracking()
            .Where(b => b.ChallengeId == challengeId)
            .ToListAsync();

    public Task UpdateManualBonus(UpdateManualChallengeBonus model, User actor)
    {
        throw new System.NotImplementedException();
    }
}
