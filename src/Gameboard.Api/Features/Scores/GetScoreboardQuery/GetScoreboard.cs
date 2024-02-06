using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ServiceStack;

namespace Gameboard.Api.Features.Scores;

public record GetScoreboardQuery(string GameId) : IRequest<ScoreboardData>;

internal class GetScoreboardHandler : IRequestHandler<GetScoreboardQuery, ScoreboardData>
{
    private readonly EntityExistsValidator<GetScoreboardQuery, Data.Game> _gameExists;
    private readonly IStore _store;
    private readonly IValidatorService<GetScoreboardQuery> _validatorService;

    public GetScoreboardHandler
    (
        EntityExistsValidator<GetScoreboardQuery, Data.Game> gameExists,
        IStore store,
        IValidatorService<GetScoreboardQuery> validatorService
    )
    {
        _gameExists = gameExists;
        _store = store;
        _validatorService = validatorService;
    }

    public async Task<ScoreboardData> Handle(GetScoreboardQuery request, CancellationToken cancellationToken)
    {
        // validate
        await _validatorService
            .AddValidator(_gameExists.UseProperty(r => r.GameId))
            .Validate(request, cancellationToken);

        // assemble the data
        var game = await _store
            .WithNoTracking<Data.Game>()
            .SingleAsync(g => g.Id == request.GameId, cancellationToken);

        var teams = await _store
            .WithNoTracking<DenormalizedTeamScore>()
            .Where(s => s.GameId == request.GameId)
            .OrderByDescending(s => s.ScoreOverall)
                .ThenBy(s => s.CumulativeTimeMs)
            .ToArrayAsync(cancellationToken);

        var teamPlayers = await _store
            .WithNoTracking<Data.Player>()
            .Include(p => p.Sponsor)
            .GroupBy(p => p.TeamId).ToDictionaryAsync(k => k.Key, k => k.Select(p => new PlayerWithSponsor
            {
                Id = p.Id,
                Name = p.ApprovedName,
                Sponsor = new SimpleSponsor
                {
                    Id = p.SponsorId,
                    Name = p.Sponsor.Name,
                    Logo = p.Sponsor.Logo
                }
            }));

        var specCount = await _store
            .WithNoTracking<Data.ChallengeSpec>()
            .Where(s => s.GameId == game.Id)
            .Where(s => !s.Disabled)
            .CountAsync(cancellationToken);

        return new ScoreboardData
        {
            Game = new ScoreboardDataGame
            {
                Id = game.Id,
                Name = game.Name,
                IsTeamGame = game.IsTeamGame(),
                SpecCount = specCount
            },
            Teams = teams.Select(t => new ScoreboardDataTeam
            {
                Players = teamPlayers.TryGetValue(t.TeamId, out IEnumerable<PlayerWithSponsor> value) ? value : Array.Empty<PlayerWithSponsor>(),
                Score = t
            })
        };
    }
}
