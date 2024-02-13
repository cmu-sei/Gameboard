using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Features.Games.Validators;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;

namespace Gameboard.Api.Features.Scores;

public record GameScoreQuery(string GameId) : IRequest<GameScore>;

internal sealed class GameScoreQueryHandler : IRequestHandler<GameScoreQuery, GameScore>
{
    private readonly IActingUserService _actingUserService;
    private readonly EntityExistsValidator<GameScoreQuery, Data.Game> _gameExists;
    private readonly IScoringService _scoringService;
    private readonly UserIsPlayingGameValidator _userIsPlaying;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<GameScoreQuery> _validator;

    public GameScoreQueryHandler
    (
        IActingUserService actingUserService,
        EntityExistsValidator<GameScoreQuery, Data.Game> gameExists,
        IScoringService scoringService,
        UserIsPlayingGameValidator userIsPlaying,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<GameScoreQuery> validator
    )
    {
        _actingUserService = actingUserService;
        _gameExists = gameExists.UseProperty(q => q.GameId);
        _userRoleAuthorizer = userRoleAuthorizer;
        _scoringService = scoringService;
        _userIsPlaying = userIsPlaying;
        _userRoleAuthorizer = userRoleAuthorizer;
        _validator = validator;
    }

    public async Task<GameScore> Handle(GameScoreQuery request, CancellationToken cancellationToken)
    {
        // validate
        _validator.AddValidator(_gameExists);

        if (!_userRoleAuthorizer.AllowRoles(UserRole.Admin, UserRole.Support, UserRole.Tester, UserRole.Designer).WouldAuthorize())
        {
            _validator.AddValidator
            (
                _userIsPlaying
                    .UseGameId(request.GameId)
                    .UseUserId(_actingUserService.Get().Id)
            );
        }

        await _validator.Validate(request, cancellationToken);

        // and go
        return await _scoringService.GetGameScore(request.GameId, cancellationToken);
    }
}
