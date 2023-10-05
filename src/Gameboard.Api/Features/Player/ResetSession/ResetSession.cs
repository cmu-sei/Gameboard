using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games.Start;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Teams;

public record ResetSessionCommand(string TeamId, bool UnenrollTeam, User ActingUser = null) : IRequest;

internal class ResetSessionHandler : IRequestHandler<ResetSessionCommand>
{
    private readonly ChallengeService _challengeService;
    private readonly IGameStartService _gameStartService;
    private readonly ILogger<ResetSessionHandler> _logger;
    private readonly IStore _store;
    private readonly ITeamService _teamService;
    private readonly IGameboardRequestValidator<ResetSessionCommand> _validator;

    public ResetSessionHandler
    (
        ChallengeService challengeService,
        IGameStartService gameStartService,
        ILogger<ResetSessionHandler> logger,
        IStore store,
        ITeamService teamService,
        IGameboardRequestValidator<ResetSessionCommand> validator
    )
    {
        _challengeService = challengeService;
        _gameStartService = gameStartService;
        _logger = logger;
        _store = store;
        _teamService = teamService;
        _validator = validator;
    }

    public async Task Handle(ResetSessionCommand request, CancellationToken cancellationToken)
    {
        await _validator.Validate(request, cancellationToken);

        // clean up all challenges
        _logger.LogInformation($"Resetting session for team {request.TeamId}");
        await _challengeService.ArchiveTeamChallenges(request.TeamId);

        // we need to look up whether the game is sync start first, because we're about to delete the
        // team, possibly
        var game = await _store
            .WithNoTracking<Data.Player>()
                .Include(p => p.Game)
            .Where(p => p.TeamId == request.TeamId)
            .Select(p => p.Game)
            .FirstAsync(cancellationToken);

        // delete players from the team iff. requested
        if (request.UnenrollTeam)
        {
            await _teamService.DeleteTeam(request.TeamId, null, cancellationToken);
        }
        else
        {
            // if we're not deleting the team, we still reset the session properties
            await _store
                .WithNoTracking<Data.Player>()
                .Where(p => p.TeamId == request.TeamId)
                .ExecuteUpdateAsync
                (
                    p => p
                        .SetProperty(p => p.SessionBegin, DateTimeOffset.MinValue)
                        .SetProperty(p => p.SessionEnd, DateTimeOffset.MinValue)
                        .SetProperty(p => p.SessionMinutes, 0)
                        .SetProperty(p => p.IsReady, false),
                    cancellationToken
                );
        }

        if (game.RequireSynchronizedStart)
            await _gameStartService.HandleSyncStartStateChanged(game.Id, cancellationToken);
    }
}
