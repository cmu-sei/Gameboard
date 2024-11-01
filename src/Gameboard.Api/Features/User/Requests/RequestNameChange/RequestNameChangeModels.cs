using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.Users;

public sealed class RequestNameChangeRequest
{
    public required string RequestedName { get; set; }
}

public sealed class RequestNameChangeResponse
{
    public required string Name { get; set; }
    public required string Status { get; set; }
}

public sealed class NameChangesDisabled : GameboardValidationException
{
    public NameChangesDisabled() : base($"Name changes are disabled for non-elevated users.") { }
}
