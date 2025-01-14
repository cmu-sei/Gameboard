using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Admin;

public record GetGameCenterContextQuery(string GameId) : IRequest<GameCenterContext>;

internal class GetGameCenterContextHandler
(
    EntityExistsValidator<GetGameCenterContextQuery, Data.Game> gameExists,
    INowService now,
    IStore store,
    ITeamService teamService,
    TicketService ticketService,
    IValidatorService<GetGameCenterContextQuery> validator
) : IRequestHandler<GetGameCenterContextQuery, GameCenterContext>
{
    private readonly EntityExistsValidator<GetGameCenterContextQuery, Data.Game> _gameExists = gameExists;
    private readonly INowService _now = now;
    private readonly IStore _store = store;
    private readonly ITeamService _teamService = teamService;
    private readonly TicketService _ticketService = ticketService;
    private readonly IValidatorService<GetGameCenterContextQuery> _validator = validator;

    public async Task<GameCenterContext> Handle(GetGameCenterContextQuery request, CancellationToken cancellationToken)
    {
        await _validator
            .Auth(config => config.RequirePermissions(PermissionKey.Admin_View))
            .AddValidator(_gameExists.UseProperty(r => r.GameId))
            .Validate(request, cancellationToken);

        var nowish = _now.Get();
        var gameData = await _store
            .WithNoTracking<Data.Game>()
            .Select(g => new
            {
                g.Id,
                g.Name,
                g.GameStart,
                g.GameEnd,
                g.Competition,
                g.Season,
                g.Track,
                g.Logo,
                HasScoreboard = g.PlayerMode == PlayerMode.Competition || g.Players.Any(p => p.Mode == PlayerMode.Competition),
                IsExternal = g.Mode == "external",
                IsLive = g.GameStart <= nowish && g.GameEnd >= nowish,
                g.IsPracticeMode,
                g.IsPublished,
                IsRegistrationActive = g.RegistrationType == GameRegistrationType.Open && g.RegistrationOpen <= nowish && g.RegistrationClose >= nowish,
                IsTeamGame = g.MaxTeamSize > 1
            })
            .SingleAsync(g => g.Id == request.GameId, cancellationToken);

        var challengeData = await _store
            .WithNoTracking<Data.ChallengeSpec>()
            .Where(s => s.GameId == request.GameId)
            .Where(s => !s.Disabled)
            .Where(s => !s.IsHidden)
            .GroupBy(s => s.GameId)
            .Select(gr => new
            {
                ChallengeCount = gr.Count(),
                PointsAvailable = gr.Sum(s => s.Points),
            })
            .SingleOrDefaultAsync(cancellationToken);

        var gameTotalTicketCount = await _ticketService
            .GetGameTicketsQuery(request.GameId)
            .CountAsync(cancellationToken);
        var gameOpenTicketCount = await _ticketService
            .GetGameOpenTicketsQuery(request.GameId)
            .CountAsync(cancellationToken);

        var topScore = await _store
            .WithNoTracking<DenormalizedTeamScore>()
            .Where(s => s.GameId == request.GameId)
            .Where(s => s.Rank > 0)
            .OrderByDescending(s => s.ScoreOverall)
                .ThenBy(s => s.CumulativeTimeMs)
            .FirstOrDefaultAsync(cancellationToken);
        var topScoringTeamName = string.Empty;

        if (topScore is not null)
        {
            topScoringTeamName = (await _teamService.ResolveCaptain(topScore.TeamId, cancellationToken)).ApprovedName;
        }

        var playerActivity = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.GameId == request.GameId)
            .Select(p => new
            {
                p.GameId,
                p.UserId,
                p.TeamId,
                p.Mode,
                IsActive = p.SessionBegin <= nowish && p.SessionEnd >= nowish,
                IsStarted = p.SessionBegin != DateTimeOffset.MinValue,
                IsEnded = p.Mode == PlayerMode.Competition && p.SessionEnd != DateTimeOffset.MinValue && p.SessionEnd < nowish
            })
            .GroupBy(p => p.GameId)
            .Select(gr => new GameCenterContextStats
            {
                AttemptCountPractice = gr.Where(p => p.Mode == PlayerMode.Practice).Count(),
                PlayerCountActive = gr
                    .Where(p => p.IsActive)
                    .Count(),
                PlayerCountCompetitive = gr
                    .Where(p => p.Mode == PlayerMode.Competition)
                    .Select(p => p.UserId)
                    .Distinct()
                    .Count(),
                PlayerCountPractice = gr
                    .Where(p => p.Mode == PlayerMode.Practice)
                    .Select(p => p.UserId)
                    .Distinct()
                    .Count(),
                PlayerCountTotal = gr
                    .Select(p => p.UserId)
                    .Distinct()
                    .Count(),
                TeamCountActive = gr
                    .Where(p => p.IsActive)
                    .Select(p => p.TeamId)
                    .Distinct()
                    .Count(),
                TeamCountComplete = gr
                    .Where(p => p.IsEnded)
                    .Select(p => p.TeamId)
                    .Distinct()
                    .Count(),
                TeamCountCompetitive = gr
                    .Where(p => p.Mode == PlayerMode.Competition)
                    .Select(p => p.TeamId)
                    .Distinct()
                    .Count(),
                TeamCountPractice = gr
                    .Where(p => p.Mode == PlayerMode.Practice)
                    .Select(p => p.TeamId)
                    .Distinct()
                    .Count(),
                TeamCountNotStarted = gr
                    .Where(p => !p.IsStarted)
                    .Select(p => p.TeamId)
                    .Distinct()
                    .Count(),
                TeamCountTotal = gr
                    .Select(p => p.TeamId)
                    .Distinct()
                    .Count(),
                TopScore = topScore == null ? null : topScore.ScoreOverall,
                TopScoreTeamName = topScoringTeamName
            })
            .SingleOrDefaultAsync(cancellationToken);

        return new GameCenterContext
        {
            // the game
            Id = gameData.Id,
            Name = gameData.Name,
            Logo = gameData.Logo,
            ExecutionWindow = new DateRange(gameData.GameStart, gameData.GameEnd),
            Competition = gameData.Competition,
            Season = gameData.Season,
            Track = gameData.Track,
            HasScoreboard = gameData.HasScoreboard,
            IsExternal = gameData.IsExternal,
            IsLive = gameData.IsLive,
            IsPractice = gameData.IsPracticeMode,
            IsPublished = gameData.IsPublished,
            IsRegistrationActive = gameData.IsRegistrationActive,
            IsTeamGame = gameData.IsTeamGame,
            Stats = playerActivity ?? new(),

            // aggregates
            ChallengeCount = challengeData?.ChallengeCount ?? 0,
            PointsAvailable = challengeData?.PointsAvailable ?? 0,
            OpenTicketCount = gameOpenTicketCount,
            TotalTicketCount = gameTotalTicketCount
        };
    }
}
