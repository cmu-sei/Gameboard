using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Scores;

public record GameScoreQuery(string GameId) : IRequest<GameScore>;

internal sealed class GameScoreQueryHandler : IRequestHandler<GameScoreQuery, GameScore>
{
    private readonly EntityExistsValidator<GameScoreQuery, Data.Game> _gameExists;
    private readonly INowService _nowService;
    private readonly IScoringService _scoringService;
    private readonly IStore _store;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<GameScoreQuery> _validator;

    public GameScoreQueryHandler
    (
        EntityExistsValidator<GameScoreQuery, Data.Game> gameExists,
        INowService nowService,
        IScoringService scoringService,
        IStore store,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<GameScoreQuery> validator
    )
    {
        _gameExists = gameExists.UseProperty(q => q.GameId);
        _userRoleAuthorizer = userRoleAuthorizer;
        _nowService = nowService;
        _scoringService = scoringService;
        _store = store;
        _userRoleAuthorizer = userRoleAuthorizer;
        _validator = validator;
    }

    public async Task<GameScore> Handle(GameScoreQuery request, CancellationToken cancellationToken)
    {
        // validate
        _validator.AddValidator(_gameExists);

        if (!_userRoleAuthorizer.AllowRoles(UserRole.Admin, UserRole.Support, UserRole.Tester, UserRole.Designer).WouldAuthorize())
        {
            // can only access game score details when the game is over
            _validator.AddValidator(async (req, ctx) =>
            {
                var now = _nowService.Get();
                var game = await _store
                    .WithNoTracking<Data.Game>()
                    .Select(g => new
                    {
                        g.Id,
                        g.GameEnd
                    })
                    .SingleOrDefaultAsync(g => g.Id == req.GameId && g.GameEnd <= now);

                if (game is null)
                    ctx.AddValidationException(new CantAccessThisScore("game hasn't ended"));
            });
        }

        await _validator.Validate(request, cancellationToken);

        // and go
        return await _scoringService.GetGameScore(request.GameId, cancellationToken);
    }
}
