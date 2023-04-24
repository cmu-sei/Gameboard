using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.ChallengeBonuses;

internal class ListManualBonusesHandler : IRequestHandler<ListManualBonusesQuery, IEnumerable<ManualChallengeBonusViewModel>>
{
    private readonly IStore<ManualChallengeBonus> _challengeBonusStore;
    private readonly EntityExistsValidator<ListManualBonusesQuery, Data.Challenge> _challengeExists;
    private readonly IMapper _mapper;
    private readonly IValidatorService<ListManualBonusesQuery> _validatorService;

    public ListManualBonusesHandler(
        IStore<ManualChallengeBonus> challengeBonusStore,
        EntityExistsValidator<ListManualBonusesQuery, Data.Challenge> challengeExists,
        IMapper mapper,
        IValidatorService<ListManualBonusesQuery> validatorService)
    {
        _challengeBonusStore = challengeBonusStore;
        _challengeExists = challengeExists;
        _mapper = mapper;
        _validatorService = validatorService;
    }

    public async Task<IEnumerable<ManualChallengeBonusViewModel>> Handle(ListManualBonusesQuery request, CancellationToken cancellationToken)
    {
        _validatorService.AddValidator(_challengeExists.UseProperty(r => r.ChallengeId));
        await _validatorService.Validate(request);

        return await _mapper
            .ProjectTo<ManualChallengeBonusViewModel>
            (
                _challengeBonusStore
                .List()
                .Where(b => b.ChallengeId == request.ChallengeId)
            ).ToListAsync();
    }
}
