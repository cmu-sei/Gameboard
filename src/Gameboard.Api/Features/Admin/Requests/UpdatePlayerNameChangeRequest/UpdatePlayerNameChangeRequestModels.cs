namespace Gameboard.Api.Features.Admin;

public sealed class UpdatePlayerNameChangeRequestArgs
{
    public required string ApprovedName { get; set; }
    public required string RequestedName { get; set; }
    public required string Status { get; set; }
}
