using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
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
    private readonly INowService _now;
    private readonly IStore _store;
    private readonly TicketService _ticketService;
    private readonly UserRoleAuthorizer _userRole;
    private readonly IValidatorService<GetGameCenterContextQuery> _validator;

    public GetGameCenterContextHandler
    (
        EntityExistsValidator<GetGameCenterContextQuery, Data.Game> gameExists,
        INowService now,
        IStore store,
        TicketService ticketService,
        UserRoleAuthorizer userRole,
        IValidatorService<GetGameCenterContextQuery> validator
    )
    {
        _gameExists = gameExists;
        _now = now;
        _store = store;
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
            IsRegistrationActive = gameData.IsRegistrationActive,
            IsTeamGame = gameData.IsTeamGame,

            // aggregates
            ChallengeCount = challengeData?.ChallengeCount ?? 0,
            PointsAvailable = challengeData?.PointsAvailable ?? 0,
            OpenTicketCount = openTicketCount
        };
    }
}
