using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.ChallengeBonuses;

public record ListManualChallengeBonusesQuery(string ChallengeId) : IRequest<IEnumerable<ManualChallengeBonusViewModel>>;

internal class ListManualChallengeBonusesHandler : IRequestHandler<ListManualChallengeBonusesQuery, IEnumerable<ManualChallengeBonusViewModel>>
{
    private readonly UserRoleAuthorizer _authorizer;
    private readonly EntityExistsValidator<ListManualChallengeBonusesQuery, Data.Challenge> _challengeExists;
    private readonly IMapper _mapper;
    private readonly IStore _store;
    private readonly IValidatorService<ListManualChallengeBonusesQuery> _validatorService;

    public ListManualChallengeBonusesHandler(
        UserRoleAuthorizer authorizer,
        EntityExistsValidator<ListManualChallengeBonusesQuery, Data.Challenge> challengeExists,
        IMapper mapper,
        IStore store,
        IValidatorService<ListManualChallengeBonusesQuery> validatorService)
    {
        _authorizer = authorizer;
        _challengeExists = challengeExists;
        _mapper = mapper;
        _store = store;
        _validatorService = validatorService;
    }

    public async Task<IEnumerable<ManualChallengeBonusViewModel>> Handle(ListManualChallengeBonusesQuery request, CancellationToken cancellationToken)
    {
        _authorizer
            .AllowRoles(UserRole.Admin, UserRole.Designer, UserRole.Support)
            .Authorize();

        _validatorService.AddValidator(_challengeExists.UseProperty(r => r.ChallengeId));
        await _validatorService.Validate(request, cancellationToken);

        return await _mapper
            .ProjectTo<ManualChallengeBonusViewModel>
            (
                _store
                    .WithNoTracking<ManualChallengeBonus>()
                    .Where(b => b.ChallengeId == request.ChallengeId)
            ).ToListAsync(cancellationToken);
    }
}
