using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Practice;

public interface IPracticeService
{
    Task<PracticeChallengeGroupDto[]> ChallengeGroupsList(ChallengeGroupsListArgs args, CancellationToken cancellationToken);
    Task<PracticeChallengeGroupDto> ChallengeGroupGet(string id, CancellationToken cancellationToken);
    string EscapeSuggestedSearches(IEnumerable<string> input);
    // we're currently not using this, but I'd like to add an endpoint for it so that we can clarify why practice challenges
    // are unavailable when requested
    Task<CanPlayPracticeChallengeResult> GetCanDeployChallenge(string userId, string challengeSpecId, CancellationToken cancellationToken);
    Task<DateTimeOffset> GetExtendedSessionEnd(DateTimeOffset currentSessionBegin, DateTimeOffset currentSessionEnd, CancellationToken cancellationToken);
    Task<IQueryable<Data.ChallengeSpec>> GetPracticeChallengesQueryBase(string filterTerm = null, bool includeHiddenChallengesIfHasPermission = true);
    Task<PracticeModeSettingsApiModel> GetSettings(CancellationToken cancellationToken);
    Task<Data.Player> GetUserActivePracticeSession(string userId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns a summary of all of the requested user's practice activity (with one challengespec object per challenge they've ever attempted
    /// at least once)
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<UserPracticeHistoryChallenge[]> GetUserPracticeHistory(string userId, CancellationToken cancellationToken);
    Task<string[]> GetVisibleChallengeTags(CancellationToken cancellationToken);
    Task<string[]> GetVisibleChallengeTags(IEnumerable<string> requestedTags, CancellationToken cancellationToken);
    IEnumerable<string> UnescapeSuggestedSearches(string input);
    Task<PracticeModeSettings> UpdateSettings(PracticeModeSettingsApiModel settings, string actingUserId, CancellationToken cancellationToken);
}

public enum CanPlayPracticeChallengeResult
{
    AlreadyPlayingThisChallenge,
    TooManyActivePracticeSessions,
    Yes
}

internal class PracticeService
(
    CoreOptions coreOptions,
    IGuidService guids,
    ILockService lockService,
    IMapper mapper,
    INowService now,
    IUserRolePermissionsService permissionsService,
    ISlugService slugService,
    IStore store
) : IPracticeService
{
    private readonly CoreOptions _coreOptions = coreOptions;
    private readonly IGuidService _guids = guids;
    private readonly ILockService _lockService = lockService;
    private readonly IMapper _mapper = mapper;
    private readonly INowService _now = now;
    private readonly IUserRolePermissionsService _permissions = permissionsService;
    private readonly ISlugService _slugService = slugService;

    public async Task<PracticeChallengeGroupDto> ChallengeGroupGet(string id, CancellationToken cancellationToken)
    {
        var result = await ChallengeGroupsList(new ChallengeGroupsListArgs { GroupId = id }, cancellationToken);
        if (result.Length == 1)
        {
            return result[0];
        }

        throw new ResourceNotFound<PracticeChallengeGroup>(id);
    }

    public async Task<PracticeChallengeGroupDto[]> ChallengeGroupsList(ChallengeGroupsListArgs args, CancellationToken cancellationToken)
    {
        var requestedGroupId = args.GroupId.IsEmpty() ? null : args.GroupId;
        var requestedParentGroupId = args.ParentGroupId.IsEmpty() ? null : args.ParentGroupId;
        var requestedSearchTerm = args.SearchTerm.IsEmpty() ? null : args.SearchTerm;
        var practiceSettings = await GetSettings(cancellationToken);
        var hasGlobalCertificate = practiceSettings.CertificateTemplateId.IsNotEmpty();

        // now pull the challenge groups and their challenges (specs)
        var challengeGroups = await store
            .WithNoTracking<PracticeChallengeGroup>()
            .Where(g => requestedGroupId == null || g.Id == requestedGroupId)
            .Where
            (
                g =>
                    (!args.GetRootOnly && requestedParentGroupId == null) ||
                    (args.GetRootOnly && g.ParentGroupId == null) ||
                    (g.ParentGroupId == args.ParentGroupId)
            )
            .Where
            (
                g =>
                    requestedSearchTerm == null ||
                    g.TextSearchVector.Matches(EF.Functions.PlainToTsQuery(GameboardDbContext.DEFAULT_TS_VECTOR_CONFIG, requestedSearchTerm)) ||
                    g.ChallengeSpecs.Any(s => s.TextSearchVector.Matches(EF.Functions.PlainToTsQuery(GameboardDbContext.DEFAULT_TS_VECTOR_CONFIG, requestedSearchTerm))) ||
                    g.ChildGroups.SelectMany(cg => cg.ChallengeSpecs).Any(s => s.TextSearchVector.Matches(EF.Functions.PlainToTsQuery(GameboardDbContext.DEFAULT_TS_VECTOR_CONFIG, requestedSearchTerm)))
            )
            // we materialize an anonymous type because we have to do a lot of funky aggregation that we don't need to return
            // all the data from (e.g. tags)
            .Select(g => new
            {
                g.Id,
                g.Name,
                g.Description,
                g.ImageUrl,
                g.IsFeatured,
                g.TextSearchVector,
                ParentGroup = g.ParentGroupId != null ? new SimpleEntity { Id = g.ParentGroupId, Name = g.ParentGroup.Name } : null,
                ChildGroups = g.ChildGroups.Select(c => new
                {
                    c.Id,
                    c.Name,
                    ChallengeSpecs = c.ChallengeSpecs.Select(s => new { s.Id, Tags = s.Tags.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) }).ToArray(),
                }).ToArray(),
                ChallengeCount = g.ChallengeSpecs.Count + g.ChildGroups.SelectMany(c => c.ChallengeSpecs).Count(),
                ChallengeMaxScoreTotal = g.ChallengeSpecs.Select(s => s.Points).Sum() + g.ChildGroups.SelectMany(c => c.ChallengeSpecs).Select(s => s.Points).Sum(),
                Challenges = g.ChallengeSpecs
                    .Where(s => !s.IsHidden && !s.Disabled)
                    .Select(s => new PracticeChallengeGroupDtoChallenge
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Game = new SimpleEntity { Id = s.GameId, Name = s.Game.Name },
                        Description = s.Description,
                        MaxPossibleScore = s.Points,
                        // we have to parse the tags out and filter them by practice area settings later, but
                        // can't do that in the EF query context. .split works here, but will happen on retrieval
                        Tags = s.Tags.Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
                        // default launch data, overwritten later in the function if exists
                        LaunchData = new PracticeChallengeGroupDtoChallengeLaunchData
                        {
                            CountCompletions = 0,
                            CountLaunches = 0,
                            LastLaunch = null
                        }
                    })
                    .ToArray(),
            })
            .OrderBy(c => c.IsFeatured ? 0 : 1)
                .ThenByDescending(g => requestedSearchTerm == null ? 1 : g.TextSearchVector.Rank(EF.Functions.PlainToTsQuery(GameboardDbContext.DEFAULT_TS_VECTOR_CONFIG, requestedSearchTerm)))
                .ThenBy(c => c.Name)
            .ToArrayAsync(cancellationToken);

        // shortcut out if there are no groups
        if (challengeGroups.Length == 0)
        {
            return [];
        }

        // otherwise, we have to do some work
        // first, we need to screen out tags that are on these challenges/groups but shouldn't show to end users
        // (filtered by practice area settings)
        var groupTagsDict = new Dictionary<string, string[]>();
        var challenges = challengeGroups.SelectMany(g => g.Challenges).ToArray();
        var visibleTags = new HashSet<string>(await GetVisibleChallengeTags(cancellationToken));

        foreach (var group in challengeGroups)
        {
            var groupTags = new List<string>(group.Challenges.SelectMany(c => c.Tags));
            groupTags.AddRange(group.ChildGroups.SelectMany(g => g.ChallengeSpecs.SelectMany(s => s.Tags)));
            groupTagsDict.Add(group.Id, [.. visibleTags.Intersect(groupTags.Distinct().OrderBy(t => t))]);

            foreach (var challenge in challenges)
            {
                challenge.Tags = [.. challenge.Tags.Intersect(visibleTags).OrderBy(t => t)];
            }
        }

        // calculate launch data (improved dramatically by #317, when we get there)
        var challengeSpecIds = challengeGroups.SelectMany(g => g.Challenges.Select(c => c.Id)).Distinct().ToArray();
        var challengeData = await store
            .WithNoTracking<Data.Challenge>()
            .Where(c => challengeSpecIds.Contains(c.SpecId))
            .GroupBy(c => c.SpecId)
            .Select(gr => new
            {
                ChallengeSpecId = gr.Key,
                LaunchCount = gr.Count(),
                SolveCount = gr.Where(c => c.Score >= c.Points).Count(),
                LastLaunch = gr.OrderByDescending(c => c.StartTime).Select(c => c.StartTime).FirstOrDefault()
            })
            .ToDictionaryAsync(s => s.ChallengeSpecId, s => s, cancellationToken);

        // append launch data to challenges where we can
        foreach (var challengeGroup in challengeGroups)
        {
            foreach (var challenge in challengeGroup.Challenges)
            {
                if (challengeData.TryGetValue(challenge.Id, out var launchData))
                {
                    challenge.LaunchData = new PracticeChallengeGroupDtoChallengeLaunchData
                    {
                        CountCompletions = launchData.SolveCount,
                        CountLaunches = launchData.LaunchCount,
                        LastLaunch = launchData.LastLaunch
                    };
                }
            }
        }

        return challengeGroups.Select(g => new PracticeChallengeGroupDto
        {
            Id = g.Id,
            Name = g.Name,
            Description = g.Description,
            ImageUrl = g.ImageUrl,
            IsFeatured = g.IsFeatured,
            ChallengeCount = g.ChallengeCount,
            ChallengeMaxScoreTotal = g.ChallengeMaxScoreTotal,
            Challenges = g.Challenges,
            ChildGroups = [.. g.ChildGroups.Select(c => new SimpleEntity { Id = c.Id, Name = c.Name })],
            ParentGroup = g.ParentGroup,
            Tags = groupTagsDict.GetValueOrDefault(g.Id) ?? [],
        }).ToArray();
    }

    // To avoid needing a table that literally just displays a list of strings, we store the list of suggested searches as a 
    // newline-delimited string in the PracticeModeSettings table (which has only one record). 
    public string EscapeSuggestedSearches(IEnumerable<string> input)
        => string.Join(Environment.NewLine, input.Select(search => _slugService.Get(search.Trim())));

    // same deal here - split on newline
    public IEnumerable<string> UnescapeSuggestedSearches(string input)
    {
        if (input.IsEmpty())
            return [];

        return [..
            CommonRegexes
                .WhitespaceGreedy
                .Split(input)
                .Select(m => m.Trim().ToLower())
                .Where(m => m.IsNotEmpty())
        ];
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
            {
                return CanPlayPracticeChallengeResult.TooManyActivePracticeSessions;
            }
        }

        return CanPlayPracticeChallengeResult.Yes;
    }

    /// <summary>
    /// Load the transformed query results from the database.
    /// </summary>
    /// <param name="filterTerm">A term by which to filter challenge results. Uses text vectors on various properties of the challenge and game for matching.</param>
    /// <param name="includeHiddenChallengesIfHasPermission">Include challenges which are </param>
    /// <returns></returns>
    public async Task<IQueryable<Data.ChallengeSpec>> GetPracticeChallengesQueryBase(string filterTerm = null, bool includeHiddenChallengesIfHasPermission = true)
    {
        var canViewHidden = await _permissions.Can(PermissionKey.Games_ViewUnpublished);

        var q = store
            .WithNoTracking<Data.ChallengeSpec>()
            .Where(s => s.Game.PlayerMode == PlayerMode.Practice)
            .Where(s => !s.Disabled);

        if (!canViewHidden || !includeHiddenChallengesIfHasPermission)
        {
            // without the permission, neither spec nor the game can be hidden
            q = q
                .Where(s => !s.IsHidden)
                .Where(s => s.Game.IsPublished);
        }

        if (filterTerm.IsNotEmpty())
        {
            q = q.Where(s => s.TextSearchVector.Matches(EF.Functions.PlainToTsQuery("english", filterTerm)) || s.Game.TextSearchVector.Matches(EF.Functions.PlainToTsQuery("english", filterTerm)));
            q = q.OrderByDescending(s => s.TextSearchVector.Rank(EF.Functions.PlainToTsQuery("english", filterTerm)))
                .ThenByDescending(s => s.Game.TextSearchVector.Rank(EF.Functions.PlainToTsQuery("english", filterTerm)))
                .ThenBy(s => s.Name);
        }
        else
        {
            q = q.OrderBy(s => s.Name);
        }

        return q;
    }

    public Task<Data.Player> GetUserActivePracticeSession(string userId, CancellationToken cancellationToken)
        => GetActivePracticeSessionsQueryBase()
            .Where(p => p.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<UserPracticeHistoryChallenge[]> GetUserPracticeHistory(string userId, CancellationToken cancellationToken)
    {
        // restrict to living specs #317
        var specs = await store
            .WithNoTracking<Data.ChallengeSpec>()
            .Where(s => s.Game.PlayerMode == PlayerMode.Practice || s.Game.Challenges.Any(c => c.PlayerMode == PlayerMode.Practice))
            .Select(s => s.Id)
            .ToArrayAsync(cancellationToken);

        return await store
            .WithNoTracking<Data.Challenge>()
            .Where(c => c.Player.UserId == userId)
            .Where(c => c.PlayerMode == PlayerMode.Practice)
            .Where(c => specs.Contains(c.SpecId))
            .Where(c => c.Score > 0)
            .GroupBy(c => new { c.SpecId })
            .Select(gr => new UserPracticeHistoryChallenge
            {
                ChallengeName = gr.OrderByDescending(c => c.Score).First().Name,
                ChallengeSpecId = gr.Key.SpecId,
                AttemptCount = gr.Count(),
                BestAttemptDate = gr.OrderByDescending(c => c.Score).First().StartTime,
                BestAttemptScore = gr.OrderByDescending(c => c.Score).First().Score,
                ChallengeId = gr.OrderByDescending(c => c.Score).First().Id,
                IsComplete = gr.OrderByDescending(c => c.Score).First().Score >= gr.OrderByDescending(c => c.Score).First().Points
            })
            .ToArrayAsync(cancellationToken);
    }

    public async Task<PracticeModeSettingsApiModel> GetSettings(CancellationToken cancellationToken)
    {
        using var settingsLock = await _lockService.GetArbitraryLock($"{nameof(PracticeService)}.{nameof(GetSettings)}").LockAsync(cancellationToken);
        var settings = await store.FirstOrDefaultAsync<PracticeModeSettings>(cancellationToken);

        // if we don't have any settings, make up some defaults (and save them)
        if (settings is null)
        {
            settings = GetDefaultSettings();
            await store.SaveAddRange(settings);
        }

        var apiModel = _mapper.Map<PracticeModeSettingsApiModel>(settings);
        apiModel.SuggestedSearches = UnescapeSuggestedSearches(settings.SuggestedSearches);

        return apiModel;
    }

    public async Task<string[]> GetVisibleChallengeTags(CancellationToken cancellationToken)
    {
        var settings = await GetSettings(cancellationToken);
        return [.. settings.SuggestedSearches.OrderBy(s => s)];
    }

    public async Task<string[]> GetVisibleChallengeTags(IEnumerable<string> requestedTags, CancellationToken cancellationToken)
    {
        var settings = await GetSettings(cancellationToken);
        return [.. requestedTags.Select(t => t.ToLower()).Intersect(settings.SuggestedSearches).OrderBy(s => s)];
    }

    public async Task<PracticeModeSettings> UpdateSettings(PracticeModeSettingsApiModel update, string actingUserId, CancellationToken cancellationToken)
    {
        var settings = await store.FirstOrDefaultAsync<PracticeModeSettings>(cancellationToken);
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
            settings.DefaultPracticeSessionLengthMinutes = _coreOptions.PracticeDefaultSessionLength;
        }

        await store.SaveUpdate(settings, cancellationToken);
        return settings;
    }

    private async Task<IEnumerable<string>> GetActiveSessionUsers()
        => await GetActivePracticeSessionsQueryBase()
            .Select(p => p.UserId)
            .ToArrayAsync();

    private IQueryable<Data.Player> GetActivePracticeSessionsQueryBase()
        => store
            .WithNoTracking<Data.Player>()
            .Where(p => p.SessionEnd > _now.Get())
            .Where(p => p.Mode == PlayerMode.Practice);

    private PracticeModeSettings GetDefaultSettings()
        => new()
        {
            DefaultPracticeSessionLengthMinutes = _coreOptions.PracticeDefaultSessionLength,
            MaxPracticeSessionLengthMinutes = _coreOptions.PracticeMaxSessionLength,
        };
}
