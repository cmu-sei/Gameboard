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

internal class GetScoreboardHandler
(
    IActingUserService actingUser,
    EntityExistsValidator<GetScoreboardQuery, Data.Game> gameExists,
    INowService now,
    IScoreDenormalizationService scoringDenormalizationService,
    IScoringService scoringService,
    IStore store,
    ITeamService teamService,
    IValidatorService<GetScoreboardQuery> validatorService
) : IRequestHandler<GetScoreboardQuery, ScoreboardData>
{
    private readonly IActingUserService _actingUser = actingUser;
    private readonly EntityExistsValidator<GetScoreboardQuery, Data.Game> _gameExists = gameExists;
    private readonly INowService _now = now;
    private readonly IScoreDenormalizationService _scoringDenormalizationService = scoringDenormalizationService;
    private readonly IScoringService _scoringService = scoringService;
    private readonly IStore _store = store;
    private readonly ITeamService _teamService = teamService;
    private readonly IValidatorService<GetScoreboardQuery> _validatorService = validatorService;

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

        // we grab competitive players only becaause later we filter out teams that have no competitive-mode players
        // (since the scoreboard isn't really for practice mode)
        var teamPlayers = await _store
            .WithNoTracking<Data.Player>()
            .Include(p => p.Sponsor)
            .Where(p => p.GameId == request.GameId)
            .Where(p => p.Mode == PlayerMode.Competition)
            .GroupBy(p => p.TeamId)
            .ToDictionaryAsync(k => k.Key, k => k.Select(p => new
            {
                p.Id,
                p.ApprovedName,
                p.Mode,
                p.SessionEnd,
                p.SponsorId,
                SponsorName = p.Sponsor.Name,
                SponsorLogo = p.Sponsor.Logo
            }), cancellationToken);

        var specCount = await _store
            .WithNoTracking<Data.ChallengeSpec>()
            .Where(s => s.GameId == game.Id)
            .Where(s => !s.Disabled && !s.IsHidden)
            .CountAsync(cancellationToken);

        var now = _now.Get();
        var isLive = game.GameStart.IsNotEmpty() && game.GameStart <= now && game.GameEnd.IsNotEmpty() && game.GameEnd >= now;

        // it also matters who's asking for the score: if they're an Observer, they can see it anytime.
        // otherwise, they have to wait til the game is over and ensure the game is set to make its scoreboard accessible after.
        var currentUser = _actingUser.Get();

        // build tasks that load the data for each team.
        // this isn't quite as crazy as it looks, as TeamService caches relationships between users and teams for
        // fast lookups
        var teamTasks = teams
            // .Where(t => !teamPlayers.ContainsKey(t.TeamId) || teamPlayers[t.TeamId].Any(p => p.Mode == PlayerMode.Competition))
            .Where(t => teamPlayers.TryGetValue(t.TeamId, out var lolPlayers) && lolPlayers.Any())
            .Select(async t =>
            {
                var teamData = teamPlayers.TryGetValue(t.TeamId, out var value) ? value : null;
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
            .ToArrayAsync(cancellationToken);
}
