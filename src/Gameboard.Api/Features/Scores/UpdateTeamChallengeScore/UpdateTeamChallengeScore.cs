using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Players;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Scores;

public record UpdateTeamChallengeBaseScoreCommand(string ChallengeId, double Score) : IRequest<TeamChallengeScore>;

internal class UpdateTeamChallengeBaseScoreHandler : IRequestHandler<UpdateTeamChallengeBaseScoreCommand, TeamChallengeScore>
{
    private readonly IGuidService _guidService;
    private readonly IMapper _mapper;
    private readonly IPlayersTableDenormalizationService _playersTableDenormalizationService;
    private readonly IScoringService _scoringService;
    private readonly IStore _store;
    private readonly IGameboardRequestValidator<UpdateTeamChallengeBaseScoreCommand> _validator;
    private readonly GameboardDbContext _dbContext;

    public UpdateTeamChallengeBaseScoreHandler
    (
        IGuidService guidService,
        IMapper mapper,
        IPlayersTableDenormalizationService playersTableDenormalizationService,
        IScoringService scoringService,
        IStore store,
        IGameboardRequestValidator<UpdateTeamChallengeBaseScoreCommand> validator,
        GameboardDbContext dbContext
    )
    {
        _guidService = guidService;
        _mapper = mapper;
        _playersTableDenormalizationService = playersTableDenormalizationService;
        _scoringService = scoringService;
        _store = store;
        _validator = validator;
        _dbContext = dbContext;
    }

    public async Task<TeamChallengeScore> Handle(UpdateTeamChallengeBaseScoreCommand request, CancellationToken cancellationToken)
    {
        // validate
        await _validator.Validate(request, cancellationToken);

        // load additional data
        var challenge = await _store
            .WithNoTracking<Data.Challenge>()
            .Include(c => c.Game)
            .FirstOrDefaultAsync(c => c.Id == request.ChallengeId, cancellationToken);

        // note: right now, there's only one type of automatic bonus (which is based on solve rank) 
        var spec = await _store
            .WithNoTracking<Data.ChallengeSpec>()
            .Include
            (
                spec => spec
                    .Bonuses
                    .Where(b => b.ChallengeBonusType == ChallengeBonusType.CompleteSolveRank)
                    .OrderBy(b => (b as ChallengeBonusCompleteSolveRank).SolveRank)
            ).FirstAsync(spec => spec.Id == challenge.SpecId, cancellationToken);

        // other copies of this challenge for other teams who have a solve
        var otherTeamChallenges = await _store
            .WithNoTracking<Data.Challenge>()
            .Include(c => c.AwardedBonuses)
            .Where(c => c.SpecId == spec.Id)
            .Where(c => c.GameId == challenge.GameId)
            .Where(c => c.TeamId != challenge.TeamId)
            .WhereIsFullySolved()
            // end time of the challenge against game start to get ranks
            .OrderBy(c => c.EndTime - challenge.Game.GameStart)
            .ToArrayAsync(cancellationToken);

        // award points
        // treating this as a dbcontext savechanges to preserve atomicity
        var updateChallenge = await _dbContext.Challenges.SingleAsync(c => c.Id == request.ChallengeId, cancellationToken);
        updateChallenge.Score = request.Score;

        // if they have a full solve, compute their ordinal rank by time and award them the appropriate challenge bonus
        if (challenge.Result == ChallengeResult.Success)
        {
            var availableBonuses = spec
                .Bonuses
                .Where(bonus => !otherTeamChallenges.SelectMany(c => c.AwardedBonuses).Any(otherTeamBonus => otherTeamBonus.Id == bonus.Id));

            if (availableBonuses.Any() && (availableBonuses.First() as ChallengeBonusCompleteSolveRank).SolveRank == otherTeamChallenges.Length + 1)
                updateChallenge.AwardedBonuses.Add(new AwardedChallengeBonus
                {
                    Id = _guidService.GetGuid(),
                    ChallengeBonusId = availableBonuses.First().Id
                });
        }

        // commit it
        await _dbContext.SaveChangesAsync(cancellationToken);

        // also update the players table
        // (this is a denormalization of the data in the Challenges table)
        await _playersTableDenormalizationService.UpdateTeamData(challenge.TeamId, cancellationToken);

        // have to query the scoring service to compose a complete score (which includes manual bonuses)
        return _mapper.Map<TeamChallengeScore>(await _scoringService.GetTeamChallengeScore(challenge.Id));
    }
}
