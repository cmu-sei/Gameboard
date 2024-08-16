using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using MediatR;

namespace Gameboard.Api.Features.GameEngine.Requests;

internal class GetSubmissionsRequestHandler(
    IGameEngineService gameEngine,
    IStore store,
    IGameboardRequestValidator<GetSubmissionsQuery> validator
    ) : IRequestHandler<GetSubmissionsQuery, IEnumerable<GameEngineSectionSubmission>>
{
    private readonly IGameEngineService _gameEngine = gameEngine;
    private readonly IStore _store = store;

    // validators
    private readonly IGameboardRequestValidator<GetSubmissionsQuery> _validator = validator;

    public async Task<IEnumerable<GameEngineSectionSubmission>> Handle(GetSubmissionsQuery request, CancellationToken cancellationToken)
    {
        await _validator.Validate(request, cancellationToken);

        var challenge = await _store.FirstOrDefaultAsync<Data.Challenge>(c => c.Id == request.ChallengeId, cancellationToken);
        return await _gameEngine.AuditChallenge(challenge);
    }
}
