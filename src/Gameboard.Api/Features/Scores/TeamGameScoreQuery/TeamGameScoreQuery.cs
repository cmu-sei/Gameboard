using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Scores;

public sealed class TeamGameScoreQueryResponse
{
    public GameScoreGameInfo GameInfo { get; set; }
    public GameScoreTeam Score { get; set; }
}

public record TeamGameScoreQuery(string TeamId) : IRequest<TeamGameScoreQueryResponse>;

internal class TeamGameScoreQueryHandler : IRequestHandler<TeamGameScoreQuery, TeamGameScoreQueryResponse>
{
    private readonly IScoringService _scoreService;
    private readonly IStore _store;
    private readonly TeamExistsValidator<TeamGameScoreQuery> _teamExists;
    private readonly ITeamService _teamService;
    private readonly IValidatorService<TeamGameScoreQuery> _validatorService;

    public TeamGameScoreQueryHandler(
        IScoringService scoreService,
        IStore store,
        TeamExistsValidator<TeamGameScoreQuery> teamExists,
        ITeamService teamService,
        IValidatorService<TeamGameScoreQuery> validatorService)
    {
        _scoreService = scoreService;
        _store = store;
        _teamExists = teamExists;
        _teamService = teamService;
        _validatorService = validatorService;
    }

    public async Task<TeamGameScoreQueryResponse> Handle(TeamGameScoreQuery request, CancellationToken cancellationToken)
    {
        _validatorService.AddValidator(_teamExists.UseProperty(r => r.TeamId));
        await _validatorService.Validate(request, cancellationToken);

        // there are definitely extra reads in here but i just can't
        var gameId = await _teamService.GetGameId(request.TeamId, cancellationToken);
        var game = await _store.WithNoTracking<Data.Game>().SingleAsync(g => g.Id == gameId, cancellationToken);
        var gameSpecConfig = await _scoreService.GetGameScoringConfig(game.Id);

        return new TeamGameScoreQueryResponse
        {
            GameInfo = new GameScoreGameInfo
            {
                Id = game.Id,
                Name = game.Name,
                IsTeamGame = game.IsTeamGame(),
                Specs = gameSpecConfig.Specs
            },
            Score = await _scoreService.GetTeamGameScore(request.TeamId, 1)
        };
    }
}
