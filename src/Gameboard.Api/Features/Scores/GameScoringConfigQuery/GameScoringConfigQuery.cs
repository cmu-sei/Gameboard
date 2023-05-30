using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;

namespace Gameboard.Api.Features.Scores;

public record GameScoringConfigQuery(string GameId) : IRequest<GameScoringConfig>;

internal class GameScoringConfigQueryHandler : IRequestHandler<GameScoringConfigQuery, GameScoringConfig>
{
    private readonly IMapper _mapper;
    private readonly EntityExistsValidator<GameScoringConfigQuery, Data.Game> _gameExists;
    private readonly IGameStore _gameStore;
    private readonly IScoringService _scoringService;
    private readonly IChallengeSpecStore _specStore;
    private readonly IValidatorService<GameScoringConfigQuery> _validator;

    public GameScoringConfigQueryHandler
    (
        EntityExistsValidator<GameScoringConfigQuery, Data.Game> gameExists,
        IGameStore gameStore,
        IMapper mapper,
        IScoringService scoringService,
        IChallengeSpecStore specStore,
        IValidatorService<GameScoringConfigQuery> validator
    )
    {
        _gameExists = gameExists;
        _gameStore = gameStore;
        _mapper = mapper;
        _scoringService = scoringService;
        _specStore = specStore;
        _validator = validator;
    }

    public async Task<GameScoringConfig> Handle(GameScoringConfigQuery request, CancellationToken cancellationToken)
    {
        // validate
        _validator.AddValidator(_gameExists.UseProperty(query => query.GameId));
        await _validator.Validate(request);

        // and go!
        return await _scoringService.GetGameScoringConfig(request.GameId);
    }
}
