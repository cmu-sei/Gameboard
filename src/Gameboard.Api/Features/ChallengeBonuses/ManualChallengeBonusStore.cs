using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.ChallengeBonuses;

internal class ManualChallengeBonusStore : IStore<ManualChallengeBonus>
{
    private readonly GameboardDbContext _db;
    private readonly IGuidService _guids;

    public ManualChallengeBonusStore(GameboardDbContext db, IGuidService guids)
    {
        _db = db;
        _guids = guids;
    }

    public GameboardDbContext DbContext => _db;
    public IQueryable<ManualChallengeBonus> DbSet => _db.ManualChallengeBonuses;

    public async Task<int> CountAsync(Func<IQueryable<ManualChallengeBonus>, IQueryable<ManualChallengeBonus>> queryBuilder = null)
    {
        var query = _db.ManualChallengeBonuses.AsNoTracking();

        if (queryBuilder != null)
            query = queryBuilder(query);

        return await query.CountAsync();
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

    public async Task Delete(string id)
        => await _db
            .ManualChallengeBonuses
            .Where(b => b.Id == id)
            .ExecuteDeleteAsync();


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

    public IQueryable<ManualChallengeBonus> ListAsNoTracking()
        => List();

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
