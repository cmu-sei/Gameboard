namespace Gameboard.Api.Features.Admin;

public sealed class ApprovePlayerNameRequest
{
    public required string Name { get; set; }
    public string RevisionReason { get; set; }
}
