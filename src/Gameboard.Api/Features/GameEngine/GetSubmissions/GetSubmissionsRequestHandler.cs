using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using MediatR;

namespace Gameboard.Api.Features.GameEngine.Requests;

internal class GetSubmissionsRequestHandler : IRequestHandler<GetSubmissionsQuery, IEnumerable<GameEngineSectionSubmission>>
{
    private readonly IGameEngineService _gameEngine;
    private readonly IStore _store;

    // validators
    private readonly IGameboardRequestValidator<GetSubmissionsQuery> _validator;

    // authorizers 
    private readonly UserRoleAuthorizer _roleAuthorizer;

    public GetSubmissionsRequestHandler
    (
        IGameEngineService gameEngine,
        UserRoleAuthorizer roleAuthorizer,
        IStore store,
        IGameboardRequestValidator<GetSubmissionsQuery> validator
    )
    {
        _gameEngine = gameEngine;
        _roleAuthorizer = roleAuthorizer;
        _store = store;
        _validator = validator;
    }

    public async Task<IEnumerable<GameEngineSectionSubmission>> Handle(GetSubmissionsQuery request, CancellationToken cancellationToken)
    {
        _roleAuthorizer
            .AllowRoles(UserRole.Admin, UserRole.Support, UserRole.Designer)
            .Authorize();

        await _validator.Validate(request, cancellationToken);

        var challenge = await _store.FirstOrDefaultAsync<Data.Challenge>(c => c.Id == request.ChallengeId, cancellationToken);
        return await _gameEngine.AuditChallenge(challenge);
    }
}
