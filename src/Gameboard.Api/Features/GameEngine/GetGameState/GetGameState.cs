// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Structure.MediatR;
using MediatR;

namespace Gameboard.Api.Features.GameEngine;

public record GetGameStateQuery(string TeamId) : IRequest<IEnumerable<GameEngineGameState>>;

internal class GetGameStateHandler(
    IGameEngineStore gameEngineStore,
    IGameboardRequestValidator<GetGameStateQuery> validator
    ) : IRequestHandler<GetGameStateQuery, IEnumerable<GameEngineGameState>>
{
    private readonly IGameEngineStore _gameEngineStore = gameEngineStore;
    private readonly IGameboardRequestValidator<GetGameStateQuery> _validator = validator;

    public async Task<IEnumerable<GameEngineGameState>> Handle(GetGameStateQuery request, CancellationToken cancellationToken)
    {
        await _validator.Validate(request, cancellationToken);
        return await _gameEngineStore.GetGameStatesByTeam(request.TeamId);
    }
}
