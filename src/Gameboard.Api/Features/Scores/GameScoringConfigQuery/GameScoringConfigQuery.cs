// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;

namespace Gameboard.Api.Features.Scores;

public record GameScoringConfigQuery(string GameId) : IRequest<GameScoringConfig>;

internal class GameScoringConfigQueryHandler : IRequestHandler<GameScoringConfigQuery, GameScoringConfig>
{
    private readonly EntityExistsValidator<GameScoringConfigQuery, Data.Game> _gameExists;
    private readonly IScoringService _scoringService;
    private readonly IValidatorService<GameScoringConfigQuery> _validator;

    public GameScoringConfigQueryHandler
    (
        EntityExistsValidator<GameScoringConfigQuery, Data.Game> gameExists,
        IScoringService scoringService,
        IValidatorService<GameScoringConfigQuery> validator
    )
    {
        _gameExists = gameExists;
        _scoringService = scoringService;
        _validator = validator;
    }

    public async Task<GameScoringConfig> Handle(GameScoringConfigQuery request, CancellationToken cancellationToken)
    {
        // validate
        _validator.AddValidator(_gameExists.UseProperty(query => query.GameId));
        await _validator.Validate(request, cancellationToken);

        // and go!
        return await _scoringService.GetGameScoringConfig(request.GameId);
    }
}
