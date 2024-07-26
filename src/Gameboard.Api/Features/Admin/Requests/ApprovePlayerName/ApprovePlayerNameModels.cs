namespace Gameboard.Api.Features.Admin;

public sealed class ApprovePlayerNameRequest
{
    public required string Name { get; set; }
    public required string RevisionReason { get; set; }
}
