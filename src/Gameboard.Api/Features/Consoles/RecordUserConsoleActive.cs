// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Features.Practice;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;

namespace Gameboard.Api.Features.Consoles;

/// <summary>
/// Issued whenever a user's challenge console reports activity. Currently, this only 
/// matters if they're in practice mode.
/// </summary>
/// <param name="ConsoleId">The ID of the console being interacted with.</param>
/// <param name="ActingUser">The user who owns the challenge console that has reported activity.</param>
public record RecordUserConsoleActiveCommand(ConsoleId ConsoleId, User ActingUser) : IRequest<ConsoleActionResponse>;

internal class RecordUserConsoleActiveHandler(
    ChallengeService challengeService,
    ConsoleActorMap consoleActorMap,
    INowService nowService,
    IPracticeService practiceService,
    ITeamService teamService,
    EntityExistsValidator<RecordUserConsoleActiveCommand, Data.User> userExists,
    IValidatorService<RecordUserConsoleActiveCommand> validatorService
    ) : IRequestHandler<RecordUserConsoleActiveCommand, ConsoleActionResponse>
{
    internal static int EXTEND_THRESHOLD_MINUTES = 10;
    internal static string MESSAGE_EXTENDED = "Session extended.";
    internal static string MESSAGE_NOT_EXTENDED = "Session not extended.";

    private readonly INowService _nowService = nowService;
    private readonly IPracticeService _practiceService = practiceService;
    private readonly ITeamService _teamService = teamService;
    private readonly EntityExistsValidator<RecordUserConsoleActiveCommand, Data.User> _userExists = userExists;
    private readonly IValidatorService<RecordUserConsoleActiveCommand> _validatorService = validatorService;

    public async Task<ConsoleActionResponse> Handle(RecordUserConsoleActiveCommand request, CancellationToken cancellationToken)
    {
        // validate
        _validatorService.AddValidator(_userExists.UseProperty(r => r.ActingUser.Id));
        await _validatorService.Validate(request, cancellationToken);

        // associate the actor with the console in the console actor map
        var consoleActor = await challengeService.SetConsoleActor(request.ConsoleId, request.ActingUser.Id, request.ActingUser.ApprovedName);
        consoleActorMap.Update(consoleActor);

        // determine if this player has an active practice session
        var now = _nowService.Get();
        var player = await _practiceService.GetUserActivePracticeSession(request.ActingUser.Id, cancellationToken);
        if (player is null || !player.IsPractice)
        {
            return new ConsoleActionResponse { Message = null };
        }

        // if the player's session has less time remaining than the extension threshold, automatically extend 
        if (player.SessionEnd - now >= TimeSpan.FromMinutes(EXTEND_THRESHOLD_MINUTES))
        {
            return new ConsoleActionResponse { Message = MESSAGE_NOT_EXTENDED };
        }

        // NOTE: due to the way session extension currently works, it actually doesn't matter what you pass
        // for the NewSessionEnd here. The team service extends practice sessions by a max of one hour up
        // to a cap for the overall session length. Cleaning up this architecture is on our list.
        var preExtensionSessionEnd = player.SessionEnd;
        var postExtensionSessionEnd = _nowService.Get().AddHours(1);
        var extensionResult = await _teamService.ExtendSession(new ExtendTeamSessionRequest
        {
            Actor = request.ActingUser,
            NewSessionEnd = postExtensionSessionEnd,
            TeamId = player.TeamId
        }, cancellationToken);

        // Really shouldn't do this with strings, but I bamboozled myself a little - we need to know
        // that something happened, but it feels weird for it to return an object specifically about
        // practice mode session extension when the endpoint is meant to be more generally about
        // user activity.
        var isExtended = extensionResult.SessionEnd != preExtensionSessionEnd;
        return new ConsoleActionResponse
        {
            Message = isExtended ? $"{MESSAGE_EXTENDED}: {postExtensionSessionEnd}" : MESSAGE_NOT_EXTENDED,
            SessionAutoExtended = isExtended,
            SessionExpiresAt = extensionResult.SessionEnd
        };
    }
}
