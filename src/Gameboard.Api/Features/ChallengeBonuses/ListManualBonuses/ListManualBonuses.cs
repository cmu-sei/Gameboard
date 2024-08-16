using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.ChallengeBonuses;

public record ListManualChallengeBonusesQuery(string ChallengeId) : IRequest<IEnumerable<ManualChallengeBonusViewModel>>;

internal class ListManualChallengeBonusesHandler(
    EntityExistsValidator<ListManualChallengeBonusesQuery, Data.Challenge> challengeExists,
    IMapper mapper,
    IStore store,
    IValidatorService<ListManualChallengeBonusesQuery> validatorService) : IRequestHandler<ListManualChallengeBonusesQuery, IEnumerable<ManualChallengeBonusViewModel>>
{
    private readonly EntityExistsValidator<ListManualChallengeBonusesQuery, Data.Challenge> _challengeExists = challengeExists;
    private readonly IMapper _mapper = mapper;
    private readonly IStore _store = store;
    private readonly IValidatorService<ListManualChallengeBonusesQuery> _validatorService = validatorService;

    public async Task<IEnumerable<ManualChallengeBonusViewModel>> Handle(ListManualChallengeBonusesQuery request, CancellationToken cancellationToken)
    {
        await _validatorService
            .ConfigureAuthorization(a => a.RequirePermissions(UserRolePermissionKey.Scores_AwardManualBonuses))
            .AddValidator(_challengeExists.UseProperty(r => r.ChallengeId))
            .Validate(request, cancellationToken);

        return await _mapper
            .ProjectTo<ManualChallengeBonusViewModel>
            (
                _store
                    .WithNoTracking<ManualChallengeBonus>()
                    .Where(b => b.ChallengeId == request.ChallengeId)
            ).ToListAsync(cancellationToken);
    }
}
