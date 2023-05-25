using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Scores;

public record GameScoringConfigQuery(string GameId) : IRequest<GameScoringConfig>;

internal class GameScoringConfigQueryHandler : IRequestHandler<GameScoringConfigQuery, GameScoringConfig>
{
    private readonly IMapper _mapper;
    private readonly EntityExistsValidator<GameScoringConfigQuery, Data.Game> _gameExists;
    private readonly IGameStore _gameStore;
    private readonly IChallengeSpecStore _specStore;
    private readonly IValidatorService<GameScoringConfigQuery> _validator;

    public GameScoringConfigQueryHandler
    (
        EntityExistsValidator<GameScoringConfigQuery, Data.Game> gameExists,
        IChallengeSpecStore specStore,
        IGameStore gameStore,
        IMapper mapper,
        IValidatorService<GameScoringConfigQuery> validator
    )
    {
        _gameExists = gameExists;
        _gameStore = gameStore;
        _mapper = mapper;
        _specStore = specStore;
        _validator = validator;
    }

    public async Task<GameScoringConfig> Handle(GameScoringConfigQuery request, CancellationToken cancellationToken)
    {
        // validate
        _validator.AddValidator(_gameExists.UseProperty(query => query.GameId));
        await _validator.Validate(request);

        // and go!
        var game = await _gameStore.Retrieve(request.GameId);

        var challengeSpecs = await _specStore
            .List()
            .AsNoTracking()
            .Where(s => s.GameId == request.GameId)
            .Include(s => s.Bonuses)
            .ToArrayAsync();

        // transform
        return new GameScoringConfig
        {
            Game = new SimpleEntity { Id = game.Id, Name = game.Name },
            ChallengeSpecScoringConfigs = challengeSpecs.Select(s =>
            {
                // note - we're currently assuming here that there's a max of one bonus per team, but
                // that doesn't have to necessarily be true forever
                var maxPossibleScore = (double)s.Points;

                if (s.Bonuses.Any(b => b.PointValue > 0))
                {
                    maxPossibleScore += s.Bonuses.OrderByDescending(b => b.PointValue).First().PointValue;
                }

                return new GameScoringConfigChallengeSpec
                {
                    ChallengeSpec = new SimpleEntity { Id = s.Id, Name = s.Description },
                    CompletionScore = s.Points,
                    PossibleBonuses = s.Bonuses.Select(b => _mapper.Map<GameScoringConfigChallengeBonus>(b)),
                    MaxPossibleScore = maxPossibleScore
                };
            })
        };
    }
}
