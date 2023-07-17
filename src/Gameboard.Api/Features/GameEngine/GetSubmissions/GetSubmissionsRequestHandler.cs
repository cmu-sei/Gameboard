using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using MediatR;

namespace Gameboard.Api.Features.GameEngine.Requests;

internal class GetSubmissionsRequestHandler : IRequestHandler<GetSubmissionsQuery, IEnumerable<GameEngineSectionSubmission>>
{
    private readonly IChallengeStore _challengeStore;
    private readonly IGameEngineService _gameEngine;

    // validators
    private readonly IGameboardRequestValidator<GetSubmissionsQuery> _validator;

    // authorizers 
    private readonly UserRoleAuthorizer _roleAuthorizer;

    public GetSubmissionsRequestHandler
    (
        IChallengeStore challengeStore,
        IGameEngineService gameEngine,
        UserRoleAuthorizer roleAuthorizer,
        IGameboardRequestValidator<GetSubmissionsQuery> validator
    )
    {
        _challengeStore = challengeStore;
        _gameEngine = gameEngine;
        _roleAuthorizer = roleAuthorizer;
        _validator = validator;
    }

    public async Task<IEnumerable<GameEngineSectionSubmission>> Handle(GetSubmissionsQuery request, CancellationToken cancellationToken)
    {
        _roleAuthorizer
            .AllowRoles(UserRole.Admin, UserRole.Support, UserRole.Designer)
            .Authorize();

        await _validator.Validate(request, cancellationToken);

        var challenge = await _challengeStore.Retrieve(request.ChallengeId);
        return await _gameEngine.AuditChallenge(challenge);
    }
}
