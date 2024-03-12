using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Teams;

public record ResetTeamSessionCommand(string TeamId, bool UnenrollTeam, User ActingUser) : IRequest;

internal class ResetTeamSessionHandler : IRequestHandler<ResetTeamSessionCommand>
{
    private readonly IInternalHubBus _hubBus;
    private readonly ILogger<ResetTeamSessionHandler> _logger;
    private readonly IMediator _mediator;
    private readonly IStore _store;
    private readonly ISyncStartGameService _syncStartGameService;
    private readonly ITeamService _teamService;
    private readonly IGameboardRequestValidator<ResetTeamSessionCommand> _validator;

    public ResetTeamSessionHandler
    (
        IInternalHubBus hubBus,
        ILogger<ResetTeamSessionHandler> logger,
        IMediator mediator,
        IStore store,
        ISyncStartGameService syncStartGameService,
        ITeamService teamService,
        IGameboardRequestValidator<ResetTeamSessionCommand> validator
    )
    {
        _hubBus = hubBus;
        _logger = logger;
        _mediator = mediator;
        _store = store;
        _syncStartGameService = syncStartGameService;
        _teamService = teamService;
        _validator = validator;
    }

    public async Task Handle(ResetTeamSessionCommand request, CancellationToken cancellationToken)
    {
        await _validator.Validate(request, cancellationToken);

        // clean up all challenges
        _logger.LogInformation($"Resetting session for team {request.TeamId}");

        // we need to look up whether the game is sync start first, because we're about to delete the
        // team, possibly
        var gameInfo = await _store
            .WithNoTracking<Data.Player>()
                .Include(p => p.Game)
            .Where(p => p.TeamId == request.TeamId)
            .Select(p => new { p.Game.Id, p.Game.RequireSynchronizedStart })
            .FirstAsync(cancellationToken);

        // delete players from the team iff. requested
        if (request.UnenrollTeam)
        {
            _logger.LogInformation($"Deleting players/challenges/metadata for team {request.TeamId}");
            await _teamService.DeleteTeam(request.TeamId, new SimpleEntity { Id = request.ActingUser.Id, Name = request.ActingUser.ApprovedName }, cancellationToken);
        }
        else
        {
            // if we're not deleting the team, we still reset the session properties
            var players = await _store
                .WithTracking<Data.Player>()
                .Where(p => p.TeamId == request.TeamId)
                .ToArrayAsync(cancellationToken);

            // reset appropriate stats in the original denormalized score
            foreach (var player in players)
            {
                var advancedScore = player.AdvancedWithScore is not null ? player.AdvancedWithScore.Value : 0;

                player.CorrectCount = 0;
                player.IsReady = false;
                player.PartialCount = 0;
                player.Rank = 0;
                player.Score = (int)advancedScore;
                player.SessionBegin = DateTimeOffset.MinValue;
                player.SessionEnd = DateTimeOffset.MinValue;
                player.SessionMinutes = 0;
            }

            await _store.SaveUpdateRange(players);
        }

        // publish reset notification to listeners
        await _mediator.Publish(new TeamSessionResetNotification(gameInfo.Id, request.TeamId), cancellationToken);

        // notify the SignalR hub (which only matters for external games right now - we clean some
        // local storage stuff up if there's a reset).
        await _hubBus.SendTeamSessionReset(new TeamHubSessionResetEvent
        {
            Id = request.TeamId,
            GameId = gameInfo.Id,
            ActingUser = request.ActingUser.ToSimpleEntity()
        });

        if (gameInfo.RequireSynchronizedStart)
            await _syncStartGameService.HandleSyncStartStateChanged(gameInfo.Id, cancellationToken);
    }
}
