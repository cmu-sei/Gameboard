using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Games;

public record GameSessionAvailabilityQuery(string GameId) : IRequest<GameSessionAvailibilityResponse>;

internal sealed class GameSessionAvailabilityHandler
(
    IGameService gameService,
    IStore store,
    ITeamService teamService,
    IValidatorService validator
) : IRequestHandler<GameSessionAvailabilityQuery, GameSessionAvailibilityResponse>
{
    private readonly IGameService _gameService = gameService;
    private readonly IStore _store = store;
    private readonly ITeamService _teamService = teamService;
    private readonly IValidatorService _validator = validator;

    public async Task<GameSessionAvailibilityResponse> Handle(GameSessionAvailabilityQuery request, CancellationToken cancellationToken)
    {
        await _validator
            .Auth(c => c.RequireAuthentication())
            .AddEntityExistsValidator<Data.Game>(request.GameId)
            .Validate(cancellationToken);

        var sessionMax = await _store
            .WithNoTracking<Data.Game>()
            .Where(g => g.Id == request.GameId)
            .Select(g => g.SessionLimit)
            .SingleAsync(cancellationToken);

        var teamsWithActiveSession = await _gameService
            .GetTeamsWithActiveSession(request.GameId)
            .ToArrayAsync(cancellationToken);
        var nextSessionEnd = teamsWithActiveSession
            .Select(t => t.SessionEnd)
            .Where(d => d != DateTimeOffset.MinValue)
            .FirstOrDefault();

        return new GameSessionAvailibilityResponse
        {
            SessionsAvailable = Math.Max(sessionMax - teamsWithActiveSession.Length, 0),
            NextSessionEnd = nextSessionEnd == DateTimeOffset.MinValue ? null : nextSessionEnd
        };
    }
}
