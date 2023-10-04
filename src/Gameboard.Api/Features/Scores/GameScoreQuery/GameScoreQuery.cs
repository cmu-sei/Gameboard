using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Scores;

public record GameScoreQuery(string GameId) : IRequest<GameScore>;

internal sealed class GameScoreQueryHandler : IRequestHandler<GameScoreQuery, GameScore>
{
    private readonly IMapper _mapper;
    private readonly IGameStore _gameStore;
    private readonly IPlayerStore _playerStore;
    private readonly IScoringService _scoringService;

    private readonly EntityExistsValidator<GameScoreQuery, Data.Game> _gameExists;
    private readonly IValidatorService<GameScoreQuery> _validator;

    public GameScoreQueryHandler
    (
        IMapper mapper,
        IGameStore gameStore,
        IPlayerStore playerStore,
        EntityExistsValidator<GameScoreQuery, Data.Game> gameExists,
        IScoringService scoringService,
        IValidatorService<GameScoreQuery> validator
    )
    {
        _mapper = mapper;
        _gameStore = gameStore;
        _playerStore = playerStore;
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
        var game = await _gameStore.Retrieve(request.GameId);

        var players = await _playerStore
            .List()
            .AsNoTracking()
            .Where(p => p.GameId == request.GameId)
            .GroupBy(p => p.TeamId)
            .Select(g => new { TeamId = g.Key, Players = g.ToList() })
            .ToDictionaryAsync(g => g.TeamId, g => g.Players, cancellationToken);

        var managers = players.Keys
            .Select(teamId => players[teamId].FirstOrDefault(p => p.Role == PlayerRole.Manager))
            .ToDictionary(p => p.TeamId, p => p);

        // have to do these synchronously because we can't reuse the dbcontext
        // TODO: maybe a scoring service function that retrieves all at once and composes
        var teamScores = new List<TeamGameScoreSummary>();
        foreach (var teamId in managers.Keys)
        {
            teamScores.Add(await _scoringService.GetTeamGameScore(teamId));
        }

        var teamRanks = _scoringService.ComputeTeamRanks(teamScores);

        return new GameScore
        {
            Game = _mapper.Map<SimpleEntity>(game),
            Teams = players.Keys.Select(teamId => new GameScoreTeam
            {
                Team = new SimpleEntity { Id = managers[teamId].TeamId, Name = managers[teamId].ApprovedName },
                Players = _mapper.Map<SimpleEntity[]>(players[teamId]),
                Rank = teamRanks[teamId],
                Challenges = teamScores.First(s => s.Team.Id == teamId).ChallengeScoreSummaries
            })
        };
    }
}
