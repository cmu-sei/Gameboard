using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Scores;

public record UpdateTeamChallengeBaseScoreCommand(string ChallengeId, double Score) : IRequest<TeamChallengeScore>;

internal class UpdateTeamChallengeBaseScoreHandler(
    IGuidService guidService,
    IMapper mapper,
    IMediator mediator,
    IScoringService scoringService,
    IStore store,
    ITeamService teamService,
    IGameboardRequestValidator<UpdateTeamChallengeBaseScoreCommand> validator
    ) : IRequestHandler<UpdateTeamChallengeBaseScoreCommand, TeamChallengeScore>
{
    private readonly IGuidService _guidService = guidService;
    private readonly IMapper _mapper = mapper;
    private readonly IMediator _mediator = mediator;
    private readonly IScoringService _scoringService = scoringService;
    private readonly IStore _store = store;
    private readonly ITeamService _teamService = teamService;
    private readonly IGameboardRequestValidator<UpdateTeamChallengeBaseScoreCommand> _validator = validator;

    public async Task<TeamChallengeScore> Handle(UpdateTeamChallengeBaseScoreCommand request, CancellationToken cancellationToken)
    {
        // validate
        await _validator.Validate(request, cancellationToken);

        var updateChallenge = await _store
            .WithNoTracking<Data.Challenge>()
            .Include(c => c.Game)
            .SingleAsync(c => c.Id == request.ChallengeId, cancellationToken);

        // award points
        updateChallenge.Score = request.Score;

        // AWARD AUTOMATIC BONUSES (but only if the result is a success and we're in competitive mode)
        // also note: right now, there's only one type of automatic bonus (which is based on solve rank) 
        if (_scoringService.GetChallengeResult(updateChallenge.Score, updateChallenge.Points) == ChallengeResult.Success && updateChallenge.PlayerMode == PlayerMode.Competition)
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
                    .Where
                    (
                        bonus => !otherTeamChallenges
                            .SelectMany(c => c.AwardedBonuses)
                            .Any(otherTeamBonus => otherTeamBonus.ChallengeBonusId == bonus.Id)
                    );

            if (availableBonuses.Any() && (availableBonuses.First() as ChallengeBonusCompleteSolveRank).SolveRank == otherTeamChallenges.Length + 1)
            {
                await _store.Create(new AwardedChallengeBonus
                {
                    Id = _guidService.Generate(),
                    ChallengeBonusId = availableBonuses.First().Id,
                    ChallengeId = updateChallenge.Id
                });
            }
        }

        // update cumulative times
        var teamCumulativeTimeMs = await _teamService.GetCumulativeTimeMs(updateChallenge.TeamId, cancellationToken);
        await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.TeamId == updateChallenge.TeamId)
            .ExecuteUpdateAsync(up => up.SetProperty(p => p.Time, teamCumulativeTimeMs), cancellationToken);

        // notify score-interested listeners that a team has scored
        await _mediator.Publish(new ScoreChangedNotification(updateChallenge.TeamId), cancellationToken);

        // have to query the scoring service to compose a complete score (which includes manual bonuses)
        return _mapper.Map<TeamChallengeScore>(await _scoringService.GetTeamChallengeScore(updateChallenge.Id));
    }
}
