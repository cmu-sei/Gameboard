// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;

namespace Gameboard.Api.Features.Consoles;

public sealed class ConsoleId
{
    public required string Name { get; set; }
    public required string ChallengeId { get; set; }

    public override string ToString()
    {
        return $"{Name.Trim()}#{ChallengeId.Trim()}";
    }
}

public sealed class ConsoleActionResponse
{
    public required string Message { get; set; }
    public bool SessionAutoExtended { get; set; } = false;
    public DateTimeOffset SessionExpiresAt { get; set; }
}

public sealed class ConsoleState
{
    public required ConsoleId Id { get; set; }
    public required string AccessTicket { get; set; }
    public required bool IsRunning { get; set; }
    public required string Url { get; set; }
}
