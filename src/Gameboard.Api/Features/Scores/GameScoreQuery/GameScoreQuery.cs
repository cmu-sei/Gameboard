using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Scores;

public record GameScoreQuery(string GameId) : IRequest<GameScore>;

internal sealed class GameScoreQueryHandler : IRequestHandler<GameScoreQuery, GameScore>
{
    private readonly IMapper _mapper;
    private readonly IScoringService _scoringService;
    private readonly IStore _store;
    private readonly ITeamService _teamService;

    private readonly EntityExistsValidator<GameScoreQuery, Data.Game> _gameExists;
    private readonly IValidatorService<GameScoreQuery> _validator;

    public GameScoreQueryHandler
    (
        IMapper mapper,
        EntityExistsValidator<GameScoreQuery, Data.Game> gameExists,
        IScoringService scoringService,
        IStore store,
        ITeamService teamService,
        IValidatorService<GameScoreQuery> validator
    )
    {
        _mapper = mapper;
        _scoringService = scoringService;
        _store = store;
        _teamService = teamService;

        _gameExists = gameExists.UseProperty(q => q.GameId);
        _validator = validator;
    }

    public async Task<GameScore> Handle(GameScoreQuery request, CancellationToken cancellationToken)
    {
        // validate
        _validator.AddValidator(_gameExists);
        await _validator.Validate(request, cancellationToken);

        // and go
        var game = await _store.WithNoTracking<Data.Game>().SingleAsync(g => g.Id == request.GameId);
        var scoringConfig = await _scoringService.GetGameScoringConfig(request.GameId);
        var players = await _store
            .WithNoTracking<Data.Player>()
            .Include(p => p.Sponsor)
            .Where(p => p.GameId == request.GameId)
            .GroupBy(p => p.TeamId)
            .Select(g => new { TeamId = g.Key, Players = g.ToList() })
            // cap this at the top 50 for now
            .Take(50)
            .ToDictionaryAsync(g => g.TeamId, g => g.Players, cancellationToken);

        var captains = players.Keys
            .Select(teamId => _teamService.ResolveCaptain(players[teamId]))
            .ToDictionary(captain => captain.TeamId, captain => captain);

        // have to do these synchronously because we can't reuse the dbcontext
        // TODO: maybe a scoring service function that retrieves all at once and composes
        var teamScores = new Dictionary<string, GameScoreTeam>();
        foreach (var teamId in captains.Keys)
        {
            teamScores.Add(teamId, await _scoringService.GetTeamGameScore(teamId, 1));
        }

        var teamRanks = _scoringService.ComputeTeamRanks(teamScores.Values.ToList());

        return new GameScore
        {
            Game = new GameScoreGameInfo
            {
                Id = game.Id,
                Name = game.Name,
                Specs = scoringConfig.Specs,
                IsTeamGame = game.IsTeamGame()
            },
            Teams = players.Keys.Select(teamId => teamScores[teamId]).OrderBy(t => t.Rank)
        };
    }
}
