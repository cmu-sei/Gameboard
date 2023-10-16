using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Practice;

public sealed class AutoExtendPraticeChallengeResult
{
    public required bool IsExtended { get; set; }
    public required DateTimeOffset SessionEnd { get; set; }
}

public record AutoExtendPracticeChallengeCommand(string TeamId, User Actor) : IRequest<AutoExtendPraticeChallengeResult>;

internal class AutoExtendPracticeChallengeHandler : IRequestHandler<AutoExtendPracticeChallengeCommand, AutoExtendPraticeChallengeResult>
{
    private readonly INowService _now;
    private readonly IStore _store;
    private readonly ITeamService _teamService;
    private readonly IValidatorService<AutoExtendPracticeChallengeCommand> _validatorService;

    public AutoExtendPracticeChallengeHandler
    (
        INowService now,
        IStore store,
        ITeamService teamService,
        IValidatorService<AutoExtendPracticeChallengeCommand> validatorService
    )
    {
        _now = now;
        _store = store;
        _teamService = teamService;
        _validatorService = validatorService;
    }

    public async Task<AutoExtendPraticeChallengeResult> Handle(AutoExtendPracticeChallengeCommand request, CancellationToken cancellationToken)
    {
        // validate
        var now = _now.Get();
        var players = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.TeamId == request.TeamId)
            .ToArrayAsync(cancellationToken);

        _validatorService.AddValidator((req, context) =>
        {
            if (players is null || !players.Any())
                context.AddValidationException(new ResourceNotFound<Team>(request.TeamId));

            var nonPracticePlayers = players.Where(p => p.Mode != PlayerMode.Practice);
            if (nonPracticePlayers.Any())
                context.AddValidationException(new CantExtendNonPracticeSession(request.TeamId, nonPracticePlayers.Select(p => p.Id)));

            var captain = _teamService.ResolveCaptain(players);
            if (captain.SessionEnd < now)
                context.AddValidationException(new CantExtendEndedPracticeSession(request.TeamId));
        });

        await _validatorService.Validate(request, cancellationToken);

        // check if we should auto extend based on the remaining time
        var captain = _teamService.ResolveCaptain(players);
        var finalSessionEnd = captain.SessionEnd;
        var timeRemaining = captain.SessionEnd - _now.Get();
        if (timeRemaining < TimeSpan.FromMinutes(10))
        {
            var updatedCaptain = await _teamService.ExtendSession
            (
                new ExtendTeamSessionRequest
                {
                    TeamId = request.TeamId,
                    NewSessionEnd = _now.Get().AddHours(1),
                    Actor = request.Actor
                },
                cancellationToken
            );
            finalSessionEnd = updatedCaptain.SessionEnd;
        }

        return new AutoExtendPraticeChallengeResult
        {
            IsExtended = finalSessionEnd != captain.SessionEnd,
            SessionEnd = finalSessionEnd
        };
    }
}
