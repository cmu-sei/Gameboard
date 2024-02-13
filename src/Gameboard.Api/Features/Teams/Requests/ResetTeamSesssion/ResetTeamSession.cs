using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Scores;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Teams;

public record ResetTeamSessionCommand(string TeamId, bool UnenrollTeam, bool ArchiveChallenges, User ActingUser) : IRequest;

internal class ResetTeamSessionHandler : IRequestHandler<ResetTeamSessionCommand>
{
    private readonly ChallengeService _challengeService;
    private readonly IInternalHubBus _hubBus;
    private readonly ILogger<ResetTeamSessionHandler> _logger;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly IStore _store;
    private readonly ISyncStartGameService _syncStartGameService;
    private readonly ITeamService _teamService;
    private readonly IGameboardRequestValidator<ResetTeamSessionCommand> _validator;

    public ResetTeamSessionHandler
    (
        ChallengeService challengeService,
        IInternalHubBus hubBus,
        ILogger<ResetTeamSessionHandler> logger,
        IMapper mapper,
        IMediator mediator,
        IStore store,
        ISyncStartGameService syncStartGameService,
        ITeamService teamService,
        IGameboardRequestValidator<ResetTeamSessionCommand> validator
    )
    {
        _challengeService = challengeService;
        _hubBus = hubBus;
        _logger = logger;
        _mapper = mapper;
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

        if (request.ArchiveChallenges)
        {
            _logger.LogInformation($"Archiving challenges for team {request.TeamId}");
            await _challengeService.ArchiveTeamChallenges(request.TeamId);
        }

        // delete players from the team iff. requested
        if (request.UnenrollTeam)
        {
            _logger.LogInformation($"Deleting player records for team {request.TeamId}");

            // for now, we only raise the score changing event if we're keeping the team enrolled
            // (need to do this in the opposite order if we're resetting)
            // we need to do this _before_ deleting the team above
            await _mediator.Publish(new ScoreChangedNotification(request.TeamId), cancellationToken);

            await _teamService.DeleteTeam(request.TeamId, new SimpleEntity { Id = request.ActingUser.Id, Name = request.ActingUser.ApprovedName }, cancellationToken);

            // also get rid of any external game artifacts if they have any
            await _store
                .WithNoTracking<ExternalGameTeam>()
                .Where(t => t.TeamId == request.TeamId)
                .ExecuteDeleteAsync(cancellationToken);
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
                        .SetProperty(p => p.CorrectCount, 0)
                        .SetProperty(p => p.IsReady, false)
                        .SetProperty(p => p.PartialCount, 0)
                        .SetProperty(p => p.Rank, 0)
                        .SetProperty(p => p.Score, 0)
                        .SetProperty(p => p.SessionBegin, DateTimeOffset.MinValue)
                        .SetProperty(p => p.SessionEnd, DateTimeOffset.MinValue)
                        .SetProperty(p => p.SessionMinutes, 0),
                    cancellationToken
                );

            // can do this after cleaning up the actual players
            await _mediator.Publish(new ScoreChangedNotification(request.TeamId), cancellationToken);

            // notify the SignalR hub (which only matters for external games right now - we clean some
            // local storage stuff up if there's a reset).
            var captain = await _teamService.ResolveCaptain(request.TeamId, cancellationToken);
            await _hubBus.SendTeamSessionReset(_mapper.Map<Api.Player>(captain), request.ActingUser);
        }

        if (gameInfo.RequireSynchronizedStart)
            await _syncStartGameService.HandleSyncStartStateChanged(gameInfo.Id, cancellationToken);
    }
}
