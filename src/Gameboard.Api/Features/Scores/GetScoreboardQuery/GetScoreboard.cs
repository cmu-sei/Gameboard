using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Scores;

public record GetScoreboardQuery(string GameId) : IRequest<ScoreboardData>;

internal class GetScoreboardHandler : IRequestHandler<GetScoreboardQuery, ScoreboardData>
{
    private readonly IActingUserService _actingUser;
    private readonly EntityExistsValidator<GetScoreboardQuery, Data.Game> _gameExists;
    private readonly INowService _now;
    private readonly IScoreDenormalizationService _scoringDenormalizationService;
    private readonly IScoringService _scoringService;
    private readonly IStore _store;
    private readonly ITeamService _teamService;
    private readonly IValidatorService<GetScoreboardQuery> _validatorService;

    public GetScoreboardHandler
    (
        IActingUserService actingUser,
        EntityExistsValidator<GetScoreboardQuery, Data.Game> gameExists,
        INowService now,
        IScoreDenormalizationService scoringDenormalizationService,
        IScoringService scoringService,
        IStore store,
        ITeamService teamService,
        IValidatorService<GetScoreboardQuery> validatorService
    )
    {
        _actingUser = actingUser;
        _gameExists = gameExists;
        _now = now;
        _scoringDenormalizationService = scoringDenormalizationService;
        _scoringService = scoringService;
        _store = store;
        _teamService = teamService;
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
            .ToDictionaryAsync(k => k.Key, k => k.Select(p => new
            {
                p.Id,
                p.ApprovedName,
                p.SessionEnd,
                p.SponsorId,
                SponsorName = p.Sponsor.Name,
                SponsorLogo = p.Sponsor.Logo
            }), cancellationToken);

        var specCount = await _store
            .WithNoTracking<Data.ChallengeSpec>()
            .Where(s => s.GameId == game.Id)
            .Where(s => !s.Disabled)
            .CountAsync(cancellationToken);

        var now = _now.Get();
        var isLive = game.GameStart.IsNotEmpty() && game.GameStart <= now && game.GameEnd.IsNotEmpty() && game.GameEnd >= now;

        // it also matters who's asking for the score: if they're an Observer, they can see it anytime.
        // otherwise, they have to wait til the game is over and ensure the game is set to make its scoreboard accessible after.
        var currentUser = _actingUser.Get();

        // build tasks that load the data for each team.
        // this isn't quite as crazy as it looks, as TeamService caches relationships between users and teams for
        // fast lookups
        var teamTasks = teams.Select(async t =>
        {
            var teamData = teamPlayers.ContainsKey(t.TeamId) ? teamPlayers[t.TeamId] : null;
            var sessionEnd = teamData?.FirstOrDefault()?.SessionEnd;
            var userIsOnTeam = currentUser is not null && await _teamService.IsOnTeam(t.TeamId, currentUser.Id);

            if (sessionEnd is null || sessionEnd.Value.IsEmpty() || sessionEnd < now)
                sessionEnd = null;

            return new ScoreboardDataTeam
            {
                Id = t.TeamId,
                IsAdvancedToNextRound = false,
                SessionEnds = sessionEnd,
                Players = teamData is null ? Array.Empty<PlayerWithSponsor>() : teamData.Select(p => new PlayerWithSponsor
                {
                    Id = p.Id,
                    Name = p.ApprovedName,
                    Sponsor = new SimpleSponsor
                    {
                        Id = p.SponsorId,
                        Name = p.SponsorName,
                        Logo = p.SponsorLogo
                    }
                }),
                Score = t,
                UserCanAccessScoreDetail = await _scoringService.CanAccessTeamScoreDetail(t.TeamId, cancellationToken),
                UserIsOnTeam = userIsOnTeam
            };
        });

        var scoreboardTeams = new List<ScoreboardDataTeam>();
        foreach (var task in teamTasks)
            scoreboardTeams.Add(await task);

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
            Teams = scoreboardTeams
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
