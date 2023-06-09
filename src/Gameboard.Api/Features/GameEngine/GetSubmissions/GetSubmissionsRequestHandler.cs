using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Structure.MediatR.Authorizers;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Gameboard.Api.Features.GameEngine.Requests;

internal class GetSubmissionsRequestHandler : IRequestHandler<GetSubmissionsQuery, IEnumerable<GameEngineSectionSubmission>>
{
    private readonly IChallengeStore _challengeStore;
    private readonly IGameEngineService _gameEngine;
    private readonly User _actor;

    // validators
    private readonly GetSubmissionsRequestValidator _validator;

    // authorizers 
    private readonly UserRoleAuthorizer _roleAuthorizer;

    public GetSubmissionsRequestHandler(
        IChallengeStore challengeStore,
        IGameEngineService gameEngine,
        UserRoleAuthorizer roleAuthorizer,
        GetSubmissionsRequestValidator validator,
        IHttpContextAccessor httpContextAccessor)
    {
        _actor = httpContextAccessor.HttpContext.User.ToActor();
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
        await _validator.Validate(request);

        var challenge = await _challengeStore.Retrieve(request.ChallengeId);
        return await _gameEngine.AuditChallenge(challenge);
    }
}
