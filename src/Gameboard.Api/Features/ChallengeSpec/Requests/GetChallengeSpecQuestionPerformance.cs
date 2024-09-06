using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.ChallengeSpecs;

public record GetChallengeSpecQuestionPerformanceQuery(string ChallengeSpecId) : IRequest<GetChallengeSpecQuestionPerformanceResult>;

internal class GetChallengeSpecQuestionPerformanceHandler(
    ChallengeSpecService challengeSpecService,
    EntityExistsValidator<GetChallengeSpecQuestionPerformanceQuery, Data.ChallengeSpec> specExists,
    IStore store,
    IValidatorService<GetChallengeSpecQuestionPerformanceQuery> validatorService
    ) : IRequestHandler<GetChallengeSpecQuestionPerformanceQuery, GetChallengeSpecQuestionPerformanceResult>
{
    private readonly ChallengeSpecService _challengeSpecService = challengeSpecService;
    private readonly EntityExistsValidator<GetChallengeSpecQuestionPerformanceQuery, Data.ChallengeSpec> _specExists = specExists;
    private readonly IStore _store = store;
    private readonly IValidatorService<GetChallengeSpecQuestionPerformanceQuery> _validatorService = validatorService;

    public async Task<GetChallengeSpecQuestionPerformanceResult> Handle(GetChallengeSpecQuestionPerformanceQuery request, CancellationToken cancellationToken)
    {
        // auth/validate
        await _validatorService
            .Auth(a => a.RequirePermissions(PermissionKey.Reports_View))
            .AddValidator(_specExists.UseProperty(r => r.ChallengeSpecId))
            .Validate(request, cancellationToken);

        // pull raw data
        var specData = await _store
            .WithNoTracking<Data.ChallengeSpec>()
                .Include(cs => cs.Game)
            .Select(cs => new { cs.Id, cs.Name, cs.GameId, GameName = cs.Game.Name, cs.Points })
            .SingleAsync(cs => cs.Id == request.ChallengeSpecId, cancellationToken);
        var performance = await _challengeSpecService.GetQuestionPerformance(request.ChallengeSpecId, cancellationToken);

        return new GetChallengeSpecQuestionPerformanceResult
        {
            ChallengeSpec = new SimpleEntity { Id = specData.Id, Name = specData.Name },
            MaxPossibleScore = specData.Points,
            Game = new SimpleEntity { Id = specData.GameId, Name = specData.GameName },
            Questions = performance
        };
    }
}
