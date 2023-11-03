using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Certificates;

public interface ICertificatesService
{
    Task<IEnumerable<PracticeModeCertificate>> GetPracticeCertificates(string userId);
}

internal class CertificatesService : ICertificatesService
{
    private readonly INowService _now;
    private readonly IStore _store;

    public CertificatesService(INowService now, IStore store)
    {
        _now = now;
        _store = store;
    }

    public async Task<IEnumerable<PracticeModeCertificate>> GetPracticeCertificates(string userId)
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

        // have to hit specs separately for now
        var specIds = challenges.Values.Select(c => c.SpecId);
        var specs = await _store
            .List<Data.ChallengeSpec>()
                .Include(s => s.PublishedPracticeCertificates.Where(c => c.OwnerUserId == userId))
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
                },
                PublishedOn = specs.ContainsKey(attempt.SpecId) ? specs[attempt.SpecId].PublishedPracticeCertificates.FirstOrDefault()?.PublishedOn : null
            }).ToArray();
    }
}
