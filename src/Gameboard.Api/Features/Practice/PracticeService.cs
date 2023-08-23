using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Certificates;
using Gameboard.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Practice;

public interface IPracticeService
{
    Task<CanPlayPracticeChallengeResult> GetCanDeployChallenge(string userId, string challengeSpecId, CancellationToken cancellationToken);
    Task<PracticeModeSettings> GetSettings(CancellationToken cancellationToken);
}

public enum CanPlayPracticeChallengeResult
{
    AlreadyPlayingThisChallenge,
    TooManyActivePracticeSessions,
    Yes
}

internal class PracticeService : IPracticeService
{
    private readonly INowService _now;
    private readonly IStore _store;

    public PracticeService(INowService now, IStore store)
    {
        _now = now;
        _store = store;
    }

    public async Task<CanPlayPracticeChallengeResult> GetCanDeployChallenge(string userId, string challengeSpecId, CancellationToken cancellationToken)
    {
        // get settings
        var settings = await GetSettings(cancellationToken);

        // ensure the global practice session count isn't maxed
        if (settings.MaxConcurrentPracticeSessions.HasValue)
        {
            var activeSessionUsers = await GetActiveSessionUsers();
            if (activeSessionUsers.Count() >= settings.MaxConcurrentPracticeSessions.Value && !activeSessionUsers.Contains(userId))
                return CanPlayPracticeChallengeResult.TooManyActivePracticeSessions;
        }

        return CanPlayPracticeChallengeResult.Yes;
    }

    public Task<PracticeModeSettings> GetSettings(CancellationToken cancellationToken)
        => _store.SingleOrDefaultAsync<PracticeModeSettings>(cancellationToken);

    private async Task<IEnumerable<string>> GetActiveSessionUsers()
        => await GetActivePracticeSessionsQueryBase()
            .Select(p => p.UserId)
            .ToArrayAsync();

    private IQueryable<Data.Player> GetActivePracticeSessionsQueryBase()
        => _store
            .List<Data.Player>()
            .Where(p => p.SessionEnd > _now.Get())
            .Where(p => p.Mode == PlayerMode.Practice);

}
