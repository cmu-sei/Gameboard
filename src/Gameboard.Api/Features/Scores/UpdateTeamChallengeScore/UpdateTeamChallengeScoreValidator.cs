using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Scores;

internal class UpdateTeamChallengeBaseScoreValidator : IGameboardRequestValidator<UpdateTeamChallengeBaseScoreCommand>
{
    private readonly EntityExistsValidator<UpdateTeamChallengeBaseScoreCommand, Data.Challenge> _challengeExists;
    private readonly IStore _store;
    private readonly IValidatorService<UpdateTeamChallengeBaseScoreCommand> _validatorService;

    public UpdateTeamChallengeBaseScoreValidator
    (
        EntityExistsValidator<UpdateTeamChallengeBaseScoreCommand, Data.Challenge> challengeExists,
        IStore store,
        IValidatorService<UpdateTeamChallengeBaseScoreCommand> validatorService
    )
    {
        _challengeExists = challengeExists;
        _store = store;
        _validatorService = validatorService;
    }

    public async Task Validate(UpdateTeamChallengeBaseScoreCommand request, CancellationToken cancellationToken)
    {
        // validate
        var challenge = await _store
            .WithNoTracking<Data.Challenge>()
            .Include(c => c.Game)
            .Include(c => c.AwardedBonuses)
                .ThenInclude(b => b.ChallengeBonus)
            .FirstOrDefaultAsync(c => c.Id == request.ChallengeId, cancellationToken);

        // TODO: validator may need to be able to one-off validate, because all of this will fail without a challenge id
        _validatorService.AddValidator(_challengeExists.UseProperty(c => c.ChallengeId));
        _validatorService.AddValidator
        (
            (req, context) =>
            {
                if (request.Score < 0)
                    context.AddValidationException(new CantAwardNegativePointValue(challenge.Id, challenge.TeamId, request.Score));
            }
        );

        _validatorService.AddValidator
        (
            async (req, context) =>
            {
                if (!await _store.WithNoTracking<Data.ChallengeSpec>().AnyAsync(s => s.Id == challenge.SpecId))
                    context.AddValidationException(new ResourceNotFound<Data.ChallengeSpec>(challenge.SpecId));
            }
        );

        // can't change the team's score if they've already received a bonus
        if (challenge.Score > 0)
        {
            _validatorService.AddValidator
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

        await _validatorService.Validate(request, cancellationToken);
    }
}
