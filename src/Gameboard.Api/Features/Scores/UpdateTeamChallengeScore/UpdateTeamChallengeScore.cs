using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.ChallengeSpecs;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Scores;

public record UpdateTeamChallengeBaseScoreCommand(string ChallengeId, double Score) : IRequest<TeamChallengeScore>;

internal class UpdateTeamChallengeBaseScoreHandler : IRequestHandler<UpdateTeamChallengeBaseScoreCommand, TeamChallengeScore>
{
    private readonly IChallengeStore _challengeStore;
    private readonly IChallengeSpecStore _challengeSpecStore;
    private readonly EntityExistsValidator<UpdateTeamChallengeBaseScoreCommand, Data.Challenge> _challengeExists;
    private readonly IGuidService _guidService;
    private readonly IMapper _mapper;
    private readonly IScoringService _scoringService;
    private readonly EntityExistsValidator<Data.ChallengeSpec> _specExists;
    private readonly IValidatorService<UpdateTeamChallengeBaseScoreCommand> _validator;
    private readonly GameboardDbContext _dbContext;

    public UpdateTeamChallengeBaseScoreHandler
    (
        IChallengeStore challengeStore,
        IChallengeSpecStore challengeSpecStore,
        EntityExistsValidator<UpdateTeamChallengeBaseScoreCommand, Data.Challenge> challengeExists,
        IGuidService guidService,
        IMapper mapper,
        IScoringService scoringService,
        EntityExistsValidator<Data.ChallengeSpec> specExists,
        IValidatorService<UpdateTeamChallengeBaseScoreCommand> validator,
        GameboardDbContext dbContext
    )
    {
        _challengeStore = challengeStore;
        _challengeSpecStore = challengeSpecStore;
        _challengeExists = challengeExists;
        _guidService = guidService;
        _mapper = mapper;
        _scoringService = scoringService;
        _specExists = specExists;
        _validator = validator;
        _dbContext = dbContext;
    }

    public async Task<TeamChallengeScore> Handle(UpdateTeamChallengeBaseScoreCommand request, CancellationToken cancellationToken)
    {
        // validate
        var challenge = await _challengeStore
            .ListAsNoTracking()
            .Include(c => c.Game)
            .Include(c => c.AwardedBonuses)
                .ThenInclude(b => b.ChallengeBonus)
            .FirstAsync(c => c.Id == request.ChallengeId);

        // TODO: validator may need to be able to one-off validate, because all of this will fail without a challenge id
        // _validator.AddValidator(_challengeExists.UseProperty(c => c.ChallengeId));
        _validator.AddValidator
        (
            (req, context) =>
            {
                if (request.Score <= 0)
                    context.AddValidationException(new CantAwardNonPositivePointValue(challenge.Id, challenge.TeamId, request.Score));
            }
        );

        _validator.AddValidator(_specExists.UseValue(challenge.SpecId));

        // can't change the team's score if they've already received a bonus
        if (challenge.Score > 0)
        {
            _validator.AddValidator
            (
                (req, context) =>
                {
                    var awardedBonus = challenge.AwardedBonuses.FirstOrDefault(b => b.ChallengeBonus.PointValue > 0);

                    if (challenge.AwardedBonuses.Any(b => b.ChallengeBonus.PointValue > 0))
                        context.AddValidationException(new CantRescoreChallengeWithANonZeroBonus
                        (
                            request.ChallengeId,
                            challenge.TeamId,
                            awardedBonus.Id,
                            awardedBonus.ChallengeBonus.PointValue
                        ));

                    return Task.CompletedTask;
                }
            );
        }

        await _validator.Validate(request);

        // load additional data
        // note: right now, we're only awarding solve rank bonuses right now
        var spec = await _challengeSpecStore
            .ListAsNoTracking()
            .Include
            (
                spec => spec
                .Bonuses
                .Where(b => b.ChallengeBonusType == ChallengeBonusType.CompleteSolveRank).OrderBy(b => (b as ChallengeBonusCompleteSolveRank).SolveRank)
            )
            .FirstOrDefaultAsync(spec => spec.Id == challenge.SpecId);

        // other copies of this challenge for other teams who have a solve
        var otherTeamChallenges = await _challengeStore
            .ListAsNoTracking()
            .Include(c => c.AwardedBonuses)
            .Where(c => c.SpecId == spec.Id)
            .Where(c => c.TeamId != challenge.TeamId)
            .WhereIsFullySolved()
            // end time of the challenge against game start to get ranks
            .OrderBy(c => c.EndTime - challenge.Game.GameStart)
            .ToArrayAsync();

        // award points
        // treating this as a dbcontext savechanges to preserve atomicity
        var updateChallenge = await _dbContext.Challenges.FirstAsync(c => c.Id == request.ChallengeId);
        updateChallenge.Score = request.Score;

        // if they have a full solve, compute their ordinal rank by time and award them the appropriate challenge bonus
        if (challenge.Result == ChallengeResult.Success)
        {
            var availableBonuses = spec
                .Bonuses
                .Where(bonus => !otherTeamChallenges.SelectMany(c => c.AwardedBonuses).Any(otherTeamBonus => otherTeamBonus.Id == bonus.Id));

            if (availableBonuses.Any() && (availableBonuses.First() as ChallengeBonusCompleteSolveRank).SolveRank == otherTeamChallenges.Count() + 1)
                updateChallenge.AwardedBonuses.Add(new AwardedChallengeBonus
                {
                    Id = _guidService.GetGuid(),
                    ChallengeBonusId = availableBonuses.First().Id
                });
        }

        // commit it
        await _dbContext.SaveChangesAsync();

        // query manual bonuses to compose a complete score
        return _mapper.Map<TeamChallengeScore>(await _scoringService.GetTeamChallengeScore(challenge.Id));
    }
}
