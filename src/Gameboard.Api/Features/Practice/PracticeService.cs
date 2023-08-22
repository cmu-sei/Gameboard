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
    Task<IEnumerable<PracticeModeCertificate>> GetCertificates(string userId);
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

    public async Task<IEnumerable<PracticeModeCertificate>> GetCertificates(string userId)
    {
        var challenges = await _store
            .List<Data.Challenge>()
                .Include(c => c.Game)
                .Include(c => c.Player)
                    .ThenInclude(p => p.User)
            .Where(c => c.Score >= c.Points)
            .Where(c => c.PlayerMode == PlayerMode.Practice)
            .Where(c => c.Player.UserId == userId)
            .WhereDateIsNotEmpty(c => c.LastScoreTime)
            .GroupBy(c => c.SpecId)
            .ToDictionaryAsync(g => g.Key, g => g.ToList().OrderBy(c => c.StartTime).FirstOrDefault());

        var specIds = challenges.Values.Select(c => c.SpecId);
        var specs = await _store
            .List<Data.ChallengeSpec>()
            .Where(s => specIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s);

        return challenges
            .Select(entry => entry.Value)
            .Select(attempt => new PracticeModeCertificate
            {
                Challenge = new()
                {
                    Id = attempt.Id,
                    Name = attempt.Name,
                    Description = specs.ContainsKey(attempt.SpecId) ? specs[attempt.SpecId].Description : string.Empty,
                    ChallengeSpecId = attempt.SpecId
                },
                PlayerName = attempt.Player.User.ApprovedName,
                Date = attempt.StartTime,
                Score = attempt.Score,
                Time = attempt.LastScoreTime - attempt.StartTime,
                Game = new()
                {
                    Id = attempt.GameId,
                    Name = attempt.Game.Name,
                    Division = attempt.Game.Competition,
                    Season = attempt.Game.Season,
                    Track = attempt.Game.Track
                }
            }).ToArray();
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
