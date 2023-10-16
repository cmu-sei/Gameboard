using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Practice;

public interface IPracticeService
{
    string EscapeSuggestedSearches(IEnumerable<string> input);
    // we're currently not using this, but I'd like to add an endpoint for it so that we can clarify why practice challenges
    // are unavailable when requested
    Task<CanPlayPracticeChallengeResult> GetCanDeployChallenge(string userId, string challengeSpecId, CancellationToken cancellationToken);
    Task<DateTimeOffset> GetExtendedSessionEnd(DateTimeOffset currentSessionBegin, CancellationToken cancellationToken);
    Task<PracticeModeSettings> GetSettings(CancellationToken cancellationToken);
    IEnumerable<string> UnescapeSuggestedSearches(string input);
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

    // To avoid needing a table that literally just displays a list of strings, we store the list of suggested searches as a 
    // newline-delimited string in the PracticeModeSettings table (which has only one record). 
    public string EscapeSuggestedSearches(IEnumerable<string> input)
    {
        return string.Join(Environment.NewLine, input.Select(search => search.Trim()));
    }

    // same deal here - split on newline
    public IEnumerable<string> UnescapeSuggestedSearches(string input)
    {
        if (input.IsEmpty())
            return Array.Empty<string>();

        return input
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(search => search.Trim())
            .ToArray();
    }

    public async Task<DateTimeOffset> GetExtendedSessionEnd(DateTimeOffset currentSessionBegin, CancellationToken cancellationToken)
    {
        var now = _now.Get();
        var settings = await GetSettings(cancellationToken);

        // extend by one hour (hard value for now, added to practice settings later)
        var newSessionEnd = now.AddMinutes(60);

        if (settings.MaxPracticeSessionLengthMinutes.HasValue)
        {
            var maxTime = currentSessionBegin.AddMinutes(settings.MaxPracticeSessionLengthMinutes.Value);
            if (newSessionEnd > maxTime)
                newSessionEnd = maxTime;
        }

        return newSessionEnd;
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
