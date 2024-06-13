using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.ChallengeSpecs;

public record GetChallengeSpecQuestionPerformanceQuery(string ChallengeSpecId) : IRequest<GetChallengeSpecQuestionPerformanceResult>;

internal class GetChallengeSpecQuestionPerformanceHandler : IRequestHandler<GetChallengeSpecQuestionPerformanceQuery, GetChallengeSpecQuestionPerformanceResult>
{
    private readonly ChallengeSpecService _challengeSpecService;
    private readonly EntityExistsValidator<GetChallengeSpecQuestionPerformanceQuery, Data.ChallengeSpec> _specExists;
    private readonly IStore _store;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<GetChallengeSpecQuestionPerformanceQuery> _validatorService;

    public GetChallengeSpecQuestionPerformanceHandler
    (
        ChallengeSpecService challengeSpecService,
        EntityExistsValidator<GetChallengeSpecQuestionPerformanceQuery, Data.ChallengeSpec> specExists,
        IStore store,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<GetChallengeSpecQuestionPerformanceQuery> validatorService
    )
    {
        _challengeSpecService = challengeSpecService;
        _specExists = specExists;
        _store = store;
        _userRoleAuthorizer = userRoleAuthorizer;
        _validatorService = validatorService;
    }

    public async Task<GetChallengeSpecQuestionPerformanceResult> Handle(GetChallengeSpecQuestionPerformanceQuery request, CancellationToken cancellationToken)
    {
        // auth/validate
        _userRoleAuthorizer.AllowRoles(UserRole.Support, UserRole.Director, UserRole.Admin);
        _validatorService.AddValidator(_specExists.UseProperty(r => r.ChallengeSpecId));
        await _validatorService.Validate(request, cancellationToken);

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
