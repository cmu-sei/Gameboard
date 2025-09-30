// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Scores;

public record GameScoreQuery(string GameId) : IRequest<GameScore>;

internal sealed class GameScoreQueryHandler(
    EntityExistsValidator<GameScoreQuery, Data.Game> gameExists,
    INowService nowService,
    IScoringService scoringService,
    IStore store,
    IValidatorService<GameScoreQuery> validator
    ) : IRequestHandler<GameScoreQuery, GameScore>
{
    private readonly EntityExistsValidator<GameScoreQuery, Data.Game> _gameExists = gameExists.UseProperty(q => q.GameId);
    private readonly INowService _nowService = nowService;
    private readonly IScoringService _scoringService = scoringService;
    private readonly IStore _store = store;
    private readonly IValidatorService<GameScoreQuery> _validator = validator;

    public async Task<GameScore> Handle(GameScoreQuery request, CancellationToken cancellationToken)
    {
        // validate
        _validator
            .Auth(config =>
            {
                config
                    .Require(PermissionKey.Scores_ViewLive)
                    .Unless(async () =>
                    {
                        var now = _nowService.Get();
                        return await _store
                            .WithNoTracking<Data.Game>()
                            .Select(g => new
                            {
                                g.Id,
                                g.GameEnd
                            })
                            .AnyAsync(g => g.Id == request.GameId && g.GameEnd <= now);
                    }, new CantAccessThisScore("game hasn't ended"));
            });
        _validator.AddValidator(_gameExists);

        await _validator.Validate(request, cancellationToken);

        // and go
        return await _scoringService.GetGameScore(request.GameId, cancellationToken);
    }
}
