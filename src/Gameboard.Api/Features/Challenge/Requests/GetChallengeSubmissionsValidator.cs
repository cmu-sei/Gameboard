using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Challenges;

internal class GetChallengeSubmissionsValidator : IGameboardRequestValidator<GetChallengeSubmissionsQuery>
{
    private readonly IActingUserService _actingUserService;
    private readonly EntityExistsValidator<GetChallengeSubmissionsQuery, Data.Challenge> _challengeExists;
    private readonly IStore _store;
    private readonly IValidatorService<GetChallengeSubmissionsQuery> _validatorService;

    public GetChallengeSubmissionsValidator
    (
        IActingUserService actingUserService,
        EntityExistsValidator<GetChallengeSubmissionsQuery, Data.Challenge> challengeExists,
        IStore store,
        IValidatorService<GetChallengeSubmissionsQuery> validatorService
    )
    {
        _actingUserService = actingUserService;
        _challengeExists = challengeExists;
        _store = store;
        _validatorService = validatorService;
    }

    public async Task Validate(GetChallengeSubmissionsQuery request, CancellationToken cancellationToken)
    {
        _validatorService.AddValidator(async (req, ctx) =>
        {
            // check if challenge exists
            var challenge = await _store
                .WithNoTracking<Data.Challenge>()
                .SingleOrDefaultAsync(c => c.Id == req.ChallengeId, cancellationToken);

            if (challenge is null)
            {
                ctx.AddValidationException(new ResourceNotFound<Data.Challenge>(req.ChallengeId));
                return;
            }

            // check that the acting user is a player on the challenge team or an admin
            var actingUser = _actingUserService.Get();
            if (!actingUser.IsAdmin)
            {
                var isUserOnTeam = await _store
                    .WithNoTracking<Data.Player>()
                    .Where(p => p.UserId == actingUser.Id)
                    .Where(p => p.TeamId == challenge.TeamId)
                    .AnyAsync(cancellationToken);

                if (!isUserOnTeam)
                {
                    ctx.AddValidationException(new UserIsntOnTeam(actingUser.Id, challenge.TeamId));
                    return;
                }
            }
        });

        await _validatorService.Validate(request, cancellationToken);
    }
}
