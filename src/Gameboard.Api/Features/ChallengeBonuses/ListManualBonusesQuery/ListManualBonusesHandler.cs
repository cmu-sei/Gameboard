using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.ChallengeBonuses;

internal class ListManualBonusesHandler : IRequestHandler<ListManualBonusesQuery, IEnumerable<ManualChallengeBonusViewModel>>
{
    private readonly IChallengeBonusStore _challengeBonusStore;
    private readonly EntityExistsValidator<Data.Challenge> _challengeExists;
    private readonly IMapper _mapper;
    private readonly IValidatorService _validatorService;

    public ListManualBonusesHandler(
        IChallengeBonusStore challengeBonusStore,
        EntityExistsValidator<Data.Challenge> challengeExists,
        IMapper mapper,
        IValidatorService validatorService)
    {
        _challengeBonusStore = challengeBonusStore;
        _challengeExists = challengeExists;
        _mapper = mapper;
        _validatorService = validatorService;
    }

    public async Task<IEnumerable<ManualChallengeBonusViewModel>> Handle(ListManualBonusesQuery request, CancellationToken cancellationToken)
    {
        await _validatorService.Validate(request.challengeId, _challengeExists);

        return await _mapper
            .ProjectTo<ManualChallengeBonusViewModel>(_challengeBonusStore.ListManualBonuses(request.challengeId))
            .ToListAsync();
    }
}
