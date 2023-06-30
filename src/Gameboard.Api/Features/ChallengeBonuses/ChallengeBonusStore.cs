using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.ChallengeBonuses;

internal class ChallengeBonusStore : IStore<ManualChallengeBonus>
{
    private readonly GameboardDbContext _db;
    private readonly IGuidService _guids;

    public ChallengeBonusStore(GameboardDbContext db, IGuidService guids)
    {
        _db = db;
        _guids = guids;
    }

    public GameboardDbContext DbContext => _db;
    public IQueryable<ManualChallengeBonus> DbSet => _db.ManualChallengeBonuses;

    public Task<bool> AnyAsync()
        => _db.ManualChallengeBonuses.AnyAsync();

    public Task<bool> AnyAsync(Expression<Func<ManualChallengeBonus, bool>> predicate = null)
        => _db.ManualChallengeBonuses.AnyAsync(predicate);

    public Task<int> CountAsync(Func<IQueryable<ManualChallengeBonus>, IQueryable<ManualChallengeBonus>> queryBuilder = null)
    {
        var query = _db.ManualChallengeBonuses.AsNoTracking();

        if (queryBuilder != null)
            query = queryBuilder(query);

        return query.CountAsync();
    }

    public async Task<ManualChallengeBonus> Create(ManualChallengeBonus entity)
    {
        var finalEntity = new ManualChallengeBonus
        {
            Id = _guids.GetGuid(),
            Description = entity.Description,
            PointValue = entity.PointValue,
            EnteredByUserId = entity.EnteredByUserId,
            ChallengeId = entity.ChallengeId
        };

        _db.ManualChallengeBonuses.Add(finalEntity);

        await _db.SaveChangesAsync();
        return finalEntity;
    }

    public Task<IEnumerable<ManualChallengeBonus>> Create(IEnumerable<ManualChallengeBonus> range)
    {
        throw new NotImplementedException();
    }

    public Task Delete(string id)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> Exists(string id)
    {
        return
        (
            await _db
            .ManualChallengeBonuses
            .FirstOrDefaultAsync(b => b.Id == id)
        ) != null;
    }

    public IQueryable<ManualChallengeBonus> List(string term = null)
        => _db
            .ManualChallengeBonuses
            .AsNoTracking()
            .Include(c => c.EnteredByUser);

    public IQueryable<ManualChallengeBonus> ListWithNoTracking()
        => List(null);

    public Task<ManualChallengeBonus> Retrieve(string id)
        => _db
            .ManualChallengeBonuses
            .SingleOrDefaultAsync(b => b.Id == id);

    public Task<ManualChallengeBonus> Retrieve(string id, Func<IQueryable<ManualChallengeBonus>, IQueryable<ManualChallengeBonus>> includes)
    {
        var query = _db.ManualChallengeBonuses.AsQueryable();

        if (includes != null)
        {
            query = includes(query);
        }

        return query.SingleOrDefaultAsync(b => b.Id == id);
    }
    public Task Update(ManualChallengeBonus entity)
    {
        throw new NotImplementedException();
    }

    public Task Update(IEnumerable<ManualChallengeBonus> range)
    {
        throw new NotImplementedException();
    }
}
