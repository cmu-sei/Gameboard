using System;

namespace Gameboard.Api.Features.Consoles;

public sealed class GetConsoleResponse
{
    public required ConsoleState ConsoleState { get; set; }
    public required bool IsViewOnly { get; set; }
    public required DateTimeOffset ExpiresAt { get; set; }
}
