using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Scores;

public record GetScoreboardQuery(string GameId) : IRequest<ScoreboardData>;

internal class GetScoreboardHandler : IRequestHandler<GetScoreboardQuery, ScoreboardData>
{
    private readonly EntityExistsValidator<GetScoreboardQuery, Data.Game> _gameExists;
    private readonly INowService _now;
    private readonly IScoreDenormalizationService _scoringDenormalizationService;
    private readonly IStore _store;
    private readonly IValidatorService<GetScoreboardQuery> _validatorService;

    public GetScoreboardHandler
    (
        EntityExistsValidator<GetScoreboardQuery, Data.Game> gameExists,
        INowService now,
        IScoreDenormalizationService scoringDenormalizationService,
        IStore store,
        IValidatorService<GetScoreboardQuery> validatorService
    )
    {
        _gameExists = gameExists;
        _now = now;
        _scoringDenormalizationService = scoringDenormalizationService;
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

        var teams = await LoadDenormalizedTeams(request.GameId, cancellationToken);

        // if the teams aren't in the denormalized table, it's probably because this is an older game
        // that denormalized data hasn't been generated for yet. Do it here:
        if (!teams.Any())
        {
            // force the game to rerank
            await _scoringDenormalizationService.DenormalizeGame(request.GameId, cancellationToken);

            // then pull teams again
            teams = await LoadDenormalizedTeams(request.GameId, cancellationToken);
        }

        var teamPlayers = await _store
            .WithNoTracking<Data.Player>()
            .Include(p => p.Sponsor)
            .Where(p => p.GameId == request.GameId)
            .GroupBy(p => p.TeamId)
            .ToDictionaryAsync(k => k.Key, k => k.Select(p => new PlayerWithSponsor
            {
                Id = p.Id,
                Name = p.ApprovedName,
                Sponsor = new SimpleSponsor
                {
                    Id = p.SponsorId,
                    Name = p.Sponsor.Name,
                    Logo = p.Sponsor.Logo
                }
            }), cancellationToken);

        var specCount = await _store
            .WithNoTracking<Data.ChallengeSpec>()
            .Where(s => s.GameId == game.Id)
            .Where(s => !s.Disabled)
            .CountAsync(cancellationToken);

        var now = _now.Get();
        var isLive = game.GameStart.IsNotEmpty() && game.GameStart >= now && game.GameEnd.IsNotEmpty();

        return new ScoreboardData
        {
            Game = new ScoreboardDataGame
            {
                Id = game.Id,
                Name = game.Name,
                IsLiveUntil = isLive ? game.GameEnd : null,
                IsTeamGame = game.IsTeamGame(),
                SpecCount = specCount
            },
            Teams = teams.Select(t => new ScoreboardDataTeam
            {
                IsAdvancedToNextRound = false,
                Players = teamPlayers.TryGetValue(t.TeamId, out IEnumerable<PlayerWithSponsor> value) ? value : Array.Empty<PlayerWithSponsor>(),
                Score = t,
            })
        };
    }

    private async Task<IEnumerable<DenormalizedTeamScore>> LoadDenormalizedTeams(string gameId, CancellationToken cancellationToken)
        => await _store
            .WithNoTracking<DenormalizedTeamScore>()
            .Where(s => s.GameId == gameId)
            .OrderBy(s => s.Rank)
            .Where(s => s.ScoreOverall > 0)
            .Where(s => s.Rank != 0)
            .ToArrayAsync(cancellationToken);
}
