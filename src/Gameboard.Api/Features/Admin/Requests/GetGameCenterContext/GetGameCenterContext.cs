using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Admin;

public record GetGameCenterContextQuery(string GameId) : IRequest<GameCenterContext>;

internal class GetGameCenterContextHandler : IRequestHandler<GetGameCenterContextQuery, GameCenterContext>
{
    private readonly EntityExistsValidator<GetGameCenterContextQuery, Data.Game> _gameExists;
    private readonly IGameService _gameService;
    private readonly INowService _now;
    private readonly IStore _store;
    private readonly ITeamService _teamService;
    private readonly TicketService _ticketService;
    private readonly UserRoleAuthorizer _userRole;
    private readonly IValidatorService<GetGameCenterContextQuery> _validator;

    public GetGameCenterContextHandler
    (
        EntityExistsValidator<GetGameCenterContextQuery, Data.Game> gameExists,
        IGameService gameService,
        INowService now,
        IStore store,
        ITeamService teamService,
        TicketService ticketService,
        UserRoleAuthorizer userRole,
        IValidatorService<GetGameCenterContextQuery> validator
    )
    {
        _gameExists = gameExists;
        _gameService = gameService;
        _now = now;
        _store = store;
        _teamService = teamService;
        _ticketService = ticketService;
        _userRole = userRole;
        _validator = validator;
    }

    public async Task<GameCenterContext> Handle(GetGameCenterContextQuery request, CancellationToken cancellationToken)
    {
        _userRole
            .AllowAllElevatedRoles()
            .Authorize();

        _validator.AddValidator(_gameExists.UseProperty(r => r.GameId));
        await _validator.Validate(request, cancellationToken);

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

        var openTicketCount = await _ticketService
            .GetGameOpenTickets(request.GameId)
            .CountAsync(cancellationToken);

        var topScore = await _store
            .WithNoTracking<DenormalizedTeamScore>()
            .Where(s => s.GameId == request.GameId)
            .OrderByDescending(s => s.ScoreOverall)
            .FirstOrDefaultAsync(cancellationToken);
        var topScoringTeamName = string.Empty;

        if (topScore is not null)
            topScoringTeamName = (await _teamService.ResolveCaptain(topScore.TeamId, cancellationToken)).ApprovedName;

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
                IsStarted = p.SessionBegin != DateTimeOffset.MinValue
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
            OpenTicketCount = openTicketCount
        };
    }
}
