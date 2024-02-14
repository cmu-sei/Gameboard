using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Games.Validators;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Scores;

public sealed class TeamScoreQueryResponse
{
    public GameScoreGameInfo GameInfo { get; set; }
    public TeamScore Score { get; set; }
}

public record GetTeamScoreQuery(string TeamId) : IRequest<TeamScoreQueryResponse>;

internal class GetTeamScoreHandler : IRequestHandler<GetTeamScoreQuery, TeamScoreQueryResponse>
{
    private readonly IActingUserService _actingUserService;
    private readonly INowService _nowService;
    private readonly IScoringService _scoreService;
    private readonly IStore _store;
    private readonly TeamExistsValidator<GetTeamScoreQuery> _teamExists;
    private readonly ITeamService _teamService;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<GetTeamScoreQuery> _validatorService;

    public GetTeamScoreHandler(
        IActingUserService actingUserService,
        INowService nowService,
        IScoringService scoreService,
        IStore store,
        TeamExistsValidator<GetTeamScoreQuery> teamExists,
        ITeamService teamService,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<GetTeamScoreQuery> validatorService)
    {
        _actingUserService = actingUserService;
        _nowService = nowService;
        _scoreService = scoreService;
        _store = store;
        _teamExists = teamExists;
        _teamService = teamService;
        _userRoleAuthorizer = userRoleAuthorizer;
        _validatorService = validatorService;
    }

    public async Task<TeamScoreQueryResponse> Handle(GetTeamScoreQuery request, CancellationToken cancellationToken)
    {
        _validatorService.AddValidator(_teamExists.UseProperty(r => r.TeamId));

        if (!_userRoleAuthorizer.AllowRoles(UserRole.Admin, UserRole.Designer, UserRole.Support, UserRole.Tester).WouldAuthorize())
        {
            _validatorService.AddValidator(async (req, ctx) =>
            {
                var gameInfo = await _store
                    .WithNoTracking<Data.Game>()
                    .Where(g => g.Players.Any(p => p.TeamId == req.TeamId))
                    .Select(g => new
                    {
                        g.Id,
                        g.GameEnd
                    })
                    .SingleAsync(cancellationToken);

                var now = _nowService.Get();

                // if the game is over, this data is generally available
                if (gameInfo.GameEnd <= now)
                    return;

                // otherwise, you need to be on the team you're looking at
                var userId = _actingUserService.Get().Id;
                var isOnTeam = await _store
                    .WithNoTracking<Data.Player>()
                    .Where(p => p.TeamId == request.TeamId)
                    .Where(p => p.UserId == userId)
                    .Where(p => p.GameId == gameInfo.Id)
                    .AnyAsync(cancellationToken);

                if (!isOnTeam)
                    ctx.AddValidationException(new CantAccessThisScore("not on requested team"));
            });
        }

        await _validatorService.Validate(request, cancellationToken);

        // there are definitely extra reads in here but i just can't
        var gameId = await _teamService.GetGameId(request.TeamId, cancellationToken);
        var game = await _store.WithNoTracking<Data.Game>().SingleAsync(g => g.Id == gameId, cancellationToken);
        var gameSpecConfig = await _scoreService.GetGameScoringConfig(game.Id);

        return new TeamScoreQueryResponse
        {
            GameInfo = new GameScoreGameInfo
            {
                Id = game.Id,
                Name = game.Name,
                IsTeamGame = game.IsTeamGame(),
                Specs = gameSpecConfig.Specs
            },
            Score = await _scoreService.GetTeamScore(request.TeamId, cancellationToken)
        };
    }
}
