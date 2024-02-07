using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Scores;

public record UpdateTeamChallengeBaseScoreCommand(string ChallengeId, double Score) : IRequest<TeamChallengeScore>;

internal class UpdateTeamChallengeBaseScoreHandler : IRequestHandler<UpdateTeamChallengeBaseScoreCommand, TeamChallengeScore>
{
    private readonly IGuidService _guidService;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly IScoringService _scoringService;
    private readonly IStore _store;
    private readonly IGameboardRequestValidator<UpdateTeamChallengeBaseScoreCommand> _validator;
    private readonly GameboardDbContext _dbContext;

    public UpdateTeamChallengeBaseScoreHandler
    (
        IGuidService guidService,
        IMapper mapper,
        IMediator mediator,
        IScoringService scoringService,
        IStore store,
        IGameboardRequestValidator<UpdateTeamChallengeBaseScoreCommand> validator,
        GameboardDbContext dbContext
    )
    {
        _guidService = guidService;
        _mapper = mapper;
        _mediator = mediator;
        _scoringService = scoringService;
        _store = store;
        _validator = validator;
        _dbContext = dbContext;
    }

    public async Task<TeamChallengeScore> Handle(UpdateTeamChallengeBaseScoreCommand request, CancellationToken cancellationToken)
    {
        // validate
        await _validator.Validate(request, cancellationToken);

        // load additional data (and track - we use the dbContext to finalize below)
        var updateChallenge = await _store
            .WithTracking<Data.Challenge>()
            .Include(c => c.Game)
            .SingleAsync(c => c.Id == request.ChallengeId, cancellationToken);

        // award points
        updateChallenge.Score = request.Score;

        // AWARD AUTOMATIC BONUSES (but only if the result is a success and we're in competitive mode)
        // also note: right now, there's only one type of automatic bonus (which is based on solve rank) 
        if (updateChallenge.Result == ChallengeResult.Success && updateChallenge.PlayerMode == PlayerMode.Competition)
        {
            var spec = await _store
                .WithNoTracking<Data.ChallengeSpec>()
                .Include
                (
                    spec => spec
                        .Bonuses
                        .Where(b => b.ChallengeBonusType == ChallengeBonusType.CompleteSolveRank)
                        .OrderBy(b => (b as ChallengeBonusCompleteSolveRank).SolveRank)
                ).SingleAsync(spec => spec.Id == updateChallenge.SpecId, cancellationToken);

            // other copies of this challenge for other teams who have a solve
            var otherTeamChallenges = await _store
                .WithNoTracking<Data.Challenge>()
                .Include(c => c.AwardedBonuses)
                .Include(c => c.Game)
                .Where(c => c.SpecId == spec.Id)
                .Where(c => c.GameId == updateChallenge.GameId)
                .Where(c => c.TeamId != updateChallenge.TeamId)
                .WhereIsFullySolved()
                // end time of the challenge against game start to get ranks for ordinal bonuses
                .OrderBy(c => c.EndTime - updateChallenge.Game.GameStart)
                .ToArrayAsync(cancellationToken);

            // if they have a full solve, compute their ordinal rank by time and award them the appropriate challenge bonus
            var availableBonuses = spec
                    .Bonuses
                    .Where(bonus => !otherTeamChallenges.SelectMany(c => c.AwardedBonuses).Any(otherTeamBonus => otherTeamBonus.ChallengeBonusId == bonus.Id));

            if (availableBonuses.Any() && (availableBonuses.First() as ChallengeBonusCompleteSolveRank).SolveRank == otherTeamChallenges.Length + 1)
                updateChallenge.AwardedBonuses.Add(new AwardedChallengeBonus
                {
                    Id = _guidService.GetGuid(),
                    ChallengeBonusId = availableBonuses.First().Id
                });
        }

        // commit it
        await _dbContext.SaveChangesAsync(cancellationToken);

        // notify score-interested listeners that a team has scored
        await _mediator.Publish(new ScoreChangedNotification(updateChallenge.TeamId));

        // have to query the scoring service to compose a complete score (which includes manual bonuses)
        return _mapper.Map<TeamChallengeScore>(await _scoringService.GetTeamChallengeScore(updateChallenge.Id));
    }
}
