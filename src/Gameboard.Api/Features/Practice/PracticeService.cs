using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
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
    Task<DateTimeOffset> GetExtendedSessionEnd(DateTimeOffset currentSessionBegin, DateTimeOffset currentSessionEnd, CancellationToken cancellationToken);
    Task<PracticeModeSettingsApiModel> GetSettings(CancellationToken cancellationToken);
    Task<Data.Player> GetUserActivePracticeSession(string userId, CancellationToken cancellationToken);
    Task<IEnumerable<string>> GetVisibleChallengeTags(CancellationToken cancellationToken);
    Task<IEnumerable<string>> GetVisibleChallengeTags(IEnumerable<string> requestedTags, CancellationToken cancellationToken);
    IEnumerable<string> UnescapeSuggestedSearches(string input);
    Task<PracticeModeSettings> UpdateSettings(PracticeModeSettingsApiModel settings, string actingUserId, CancellationToken cancellationToken);
}

public enum CanPlayPracticeChallengeResult
{
    AlreadyPlayingThisChallenge,
    TooManyActivePracticeSessions,
    Yes
}

internal partial class PracticeService
(
    IGuidService guids,
    IMapper mapper,
    INowService now,
    ISlugService slugService,
    IStore store
) : IPracticeService
{
    private readonly IGuidService _guids = guids;
    private readonly IMapper _mapper = mapper;
    private readonly INowService _now = now;
    private readonly ISlugService _slugService = slugService;
    private readonly IStore _store = store;

    // To avoid needing a table that literally just displays a list of strings, we store the list of suggested searches as a 
    // newline-delimited string in the PracticeModeSettings table (which has only one record). 
    public string EscapeSuggestedSearches(IEnumerable<string> input)
        => string.Join(Environment.NewLine, input.Select(search => _slugService.Get(search.Trim())));

    // same deal here - split on newline
    public IEnumerable<string> UnescapeSuggestedSearches(string input)
    {
        if (input.IsEmpty())
            return [];

        return CommonRegexes
            .WhitespaceGreedy
            .Split(input)
            .Select(m => m.Trim().ToLower())
            .Where(m => m.IsNotEmpty())
            .ToArray();
    }

    public async Task<DateTimeOffset> GetExtendedSessionEnd(DateTimeOffset currentSessionBegin, DateTimeOffset currentSessionEnd, CancellationToken cancellationToken)
    {
        var now = _now.Get();
        var extendSessionBy = TimeSpan.FromMinutes(60);
        var settings = await GetSettings(cancellationToken);

        // if there's more time between now and the end of the session than the maximum allowable extension
        // just return what we already have
        if (currentSessionEnd - now >= extendSessionBy)
            return currentSessionEnd;

        // extend by one hour (hard value for now, added to practice settings later)
        var newSessionEnd = now.Add(extendSessionBy);

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

    public Task<Data.Player> GetUserActivePracticeSession(string userId, CancellationToken cancellationToken)
        => GetActivePracticeSessionsQueryBase()
            .Where(p => p.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PracticeModeSettingsApiModel> GetSettings(CancellationToken cancellationToken)
    {
        var settings = await _store.FirstOrDefaultAsync<PracticeModeSettings>(cancellationToken);

        // if we don't have any settings, make up some defaults
        if (settings is null)
        {
            return _mapper.Map<PracticeModeSettingsApiModel>(GetDefaultSettings());
        }

        var apiModel = _mapper.Map<PracticeModeSettingsApiModel>(settings);
        apiModel.SuggestedSearches = UnescapeSuggestedSearches(settings.SuggestedSearches);

        return apiModel;
    }

    public async Task<IEnumerable<string>> GetVisibleChallengeTags(CancellationToken cancellationToken)
    {
        var settings = await GetSettings(cancellationToken);
        return settings.SuggestedSearches;
    }

    public async Task<IEnumerable<string>> GetVisibleChallengeTags(IEnumerable<string> requestedTags, CancellationToken cancellationToken)
    {
        var settings = await GetSettings(cancellationToken);
        return [.. requestedTags.Select(t => t.ToLower()).Intersect(settings.SuggestedSearches)];
    }

    public async Task<PracticeModeSettings> UpdateSettings(PracticeModeSettingsApiModel update, string actingUserId, CancellationToken cancellationToken)
    {
        var settings = await _store.FirstOrDefaultAsync<PracticeModeSettings>(cancellationToken);
        if (settings is null)
        {
            settings.Id = settings.Id.IsEmpty() ? _guids.Generate() : settings.Id;
            settings.AttemptLimit = update.AttemptLimit;
            settings.CertificateTemplateId = update.CertificateTemplateId;
            settings.DefaultPracticeSessionLengthMinutes = update.DefaultPracticeSessionLengthMinutes;
            settings.IntroTextMarkdown = update.IntroTextMarkdown;
            settings.MaxConcurrentPracticeSessions = update.MaxConcurrentPracticeSessions;
            settings.MaxPracticeSessionLengthMinutes = update.MaxPracticeSessionLengthMinutes;
            settings.SuggestedSearches = EscapeSuggestedSearches(update.SuggestedSearches);
            settings.UpdatedByUserId = actingUserId;
            settings.UpdatedOn = _now.Get();
        }

        // force a value for default session length, becaues it's required
        if (settings.DefaultPracticeSessionLengthMinutes <= 0)
        {
            settings.DefaultPracticeSessionLengthMinutes = 60;
        }

        await _store.SaveUpdate(settings, cancellationToken);
        return settings;
    }

    private async Task<IEnumerable<string>> GetActiveSessionUsers()
        => await GetActivePracticeSessionsQueryBase()
            .Select(p => p.UserId)
            .ToArrayAsync();

    private IQueryable<Data.Player> GetActivePracticeSessionsQueryBase()
        => _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.SessionEnd > _now.Get())
            .Where(p => p.Mode == PlayerMode.Practice);

    private PracticeModeSettings GetDefaultSettings()
        => new()
        {
            DefaultPracticeSessionLengthMinutes = 60,
            MaxPracticeSessionLengthMinutes = 240,
        };
}
