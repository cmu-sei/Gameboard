using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.ChallengeBonuses;
using Gameboard.Api.Structure;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Scores;

public interface IScoresService
{
    Task<TeamChallengeScoreSummary> GetTeamChallengeScore(string challengeId);
    Task<ChallengeScoreSummary> GetChallengeScores(string challengeId);
}

internal class ScoresService : IScoresService
{
    private readonly IChallengeBonusStore _challengeBonusStore;
    private readonly IChallengeStore _challengeStore;
    private readonly IMapper _mapper;

    public ScoresService(
        IChallengeBonusStore challengeBonusStore,
        IChallengeStore challengeStore,
        IMapper mapper)
    {
        _challengeBonusStore = challengeBonusStore;
        _challengeStore = challengeStore;
        _mapper = mapper;
    }

    public Task<ChallengeScoreSummary> GetChallengeScores(string challengeId)
    {
        throw new System.NotImplementedException();
    }

    public async Task<TeamChallengeScoreSummary> GetTeamChallengeScore(string challengeId)
    {
        var challenge = await _challengeStore
            .List()
            .Include(c => c.Player)
            .FirstAsync(c => c.Id == challengeId);

        var bonuses = await _challengeBonusStore
            .ListManualBonuses(challengeId)
            .ToListAsync();

        var bonusScore = bonuses.Select(b => b.PointValue).Sum();
        var totalScore = challenge.Points + bonusScore;

        return new TeamChallengeScoreSummary
        {
            Team = new SimpleEntity { Id = challenge.TeamId, Name = challenge.Player.ApprovedName },
            TotalScore = totalScore,
            BaseScore = challenge.Points,
            BonusScore = bonusScore,
            ManualBonuses = _mapper.Map<IEnumerable<ManualChallengeBonusViewModel>>(bonuses)
        };
    }
}
