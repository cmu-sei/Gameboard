using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;

namespace Gameboard.Api.Features.Scores;

public record GameScoreQuery(string GameId) : IRequest<GameScore>;

internal sealed class GameScoreQueryHandler : IRequestHandler<GameScoreQuery, GameScore>
{
    private readonly IScoringService _scoringService;

    private readonly EntityExistsValidator<GameScoreQuery, Data.Game> _gameExists;
    private readonly IValidatorService<GameScoreQuery> _validator;

    public GameScoreQueryHandler
    (
        EntityExistsValidator<GameScoreQuery, Data.Game> gameExists,
        IScoringService scoringService,
        IValidatorService<GameScoreQuery> validator
    )
    {
        _scoringService = scoringService;

        _gameExists = gameExists.UseProperty(q => q.GameId);
        _validator = validator;
    }

    public async Task<GameScore> Handle(GameScoreQuery request, CancellationToken cancellationToken)
    {
        // validate
        _validator.AddValidator(_gameExists);
        await _validator.Validate(request, cancellationToken);

        // and go
        return await _scoringService.GetGameScore(request.GameId, cancellationToken);
    }
}
