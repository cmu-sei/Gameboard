using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.Games.External;

public sealed record DeleteExternalGameHostRequest(string ReplaceHostId);

public sealed class DeleteAndReplaceHostIdsMustBeDifferent : GameboardValidationException
{
    public DeleteAndReplaceHostIdsMustBeDifferent(string hostId)
        : base($"To delete a host, you must supply the ID of a different host to which its games will be migrated ({hostId}).") { }
}
