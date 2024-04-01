using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
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
    private readonly IScoringService _scoreService;
    private readonly IStore _store;
    private readonly TeamExistsValidator<GetTeamScoreQuery> _teamExists;
    private readonly ITeamService _teamService;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<GetTeamScoreQuery> _validatorService;

    public GetTeamScoreHandler(
        IScoringService scoreService,
        IStore store,
        TeamExistsValidator<GetTeamScoreQuery> teamExists,
        ITeamService teamService,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<GetTeamScoreQuery> validatorService)
    {
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

        if (!_userRoleAuthorizer.AllowRoles(UserRole.Admin, UserRole.Observer).WouldAuthorize())
        {
            _validatorService.AddValidator(async (req, ctx) =>
            {
                if (!await _scoreService.CanAccessTeamScoreDetail(request.TeamId, cancellationToken))
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
