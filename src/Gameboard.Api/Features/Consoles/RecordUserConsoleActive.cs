using System;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Features.Practice;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;

namespace Gameboard.Api.Features.Consoles;

/// <summary>
/// Issued whenever a user's challenge console reports activity. Currently, this only 
/// matters if they're in practice mode.
/// </summary>
/// <param name="ActingUser">The user who owns the challenge console that has reported activity.</param>
public record RecordUserConsoleActiveCommand(User ActingUser) : IRequest<ConsoleActionResponse>;

internal class RecordUserConsoleActiveHandler : IRequestHandler<RecordUserConsoleActiveCommand, ConsoleActionResponse>
{
    internal static string MESSAGE_EXTENDED = "Session extended.";
    internal static string MESSAGE_NOT_EXTENDED = "Session not extended.";

    private readonly INowService _nowService;
    private readonly IPracticeService _practiceService;
    private readonly ITeamService _teamService;
    private readonly EntityExistsValidator<RecordUserConsoleActiveCommand, Data.User> _userExists;
    private readonly IValidatorService<RecordUserConsoleActiveCommand> _validatorService;

    public RecordUserConsoleActiveHandler
    (
        INowService nowService,
        IPracticeService practiceService,
        ITeamService teamService,
        EntityExistsValidator<RecordUserConsoleActiveCommand, Data.User> userExists,
        IValidatorService<RecordUserConsoleActiveCommand> validatorService
    )
    {
        _nowService = nowService;
        _practiceService = practiceService;
        _teamService = teamService;
        _userExists = userExists;
        _validatorService = validatorService;
    }

    public async Task<ConsoleActionResponse> Handle(RecordUserConsoleActiveCommand request, CancellationToken cancellationToken)
    {
        // validate
        _validatorService.AddValidator(_userExists.UseProperty(r => r.ActingUser.Id));
        await _validatorService.Validate(request, cancellationToken);

        // determine if this player has an active practice session
        var now = _nowService.Get();
        var player = await _practiceService.GetUserActivePracticeSession(request.ActingUser.Id, cancellationToken);
        if (player is null || !player.IsPractice)
            return new ConsoleActionResponse { Message = null };

        // for now, we're hard coding 10 minutes as the threshold - if the player's session has less than
        // 10 minutes left, automatically extend it
        if (player.SessionEnd - now >= TimeSpan.FromMinutes(10))
            return new ConsoleActionResponse { Message = MESSAGE_NOT_EXTENDED };

        // NOTE: due to the way session extension currently works, it actually doesn't matter what you pass
        // for the NewSessionEnd here. The team service extends practice sessions by a max of one hour up
        // to a cap for the overall session length. Cleaning up this architecture is on our list.
        var preExtensionSessionEnd = player.SessionEnd;
        var extensionResult = await _teamService.ExtendSession(new ExtendTeamSessionRequest
        {
            Actor = request.ActingUser,
            NewSessionEnd = _nowService.Get().AddHours(1),
            TeamId = player.TeamId
        }, cancellationToken);

        // Really shouldn't do this with strings, but I bamboozled myself a little - we need to know
        // that something happened, but it feels weird for it to return an object specifically about
        // practice mode session extension when the endpoint is meant to be more generally about
        // user activity.
        var isExtended = extensionResult.SessionEnd != preExtensionSessionEnd;
        return new ConsoleActionResponse { Message = isExtended ? MESSAGE_EXTENDED : MESSAGE_NOT_EXTENDED };
    }
}
