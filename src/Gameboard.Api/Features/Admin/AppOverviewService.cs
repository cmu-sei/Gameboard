using System;
using System.Linq;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;

namespace Gameboard.Api.Features.Admin;

public interface IAppOverviewService
{
    IQueryable<Data.Challenge> GetActiveChallenges();
}

internal class AppOverviewService : IAppOverviewService
{
    private readonly INowService _now;
    private readonly IStore _store;

    public AppOverviewService(INowService now, IStore store)
    {
        _now = now;
        _store = store;
    }

    public IQueryable<Data.Challenge> GetActiveChallenges()
    {
        var now = _now.Get();

        return _store
            .WithNoTracking<Data.Challenge>()
            .WhereDateIsNotEmpty(c => c.StartTime)
            .Where(c => c.StartTime <= now)
            .Where(c => c.EndTime >= now || c.EndTime == DateTimeOffset.MinValue);
    }
}
