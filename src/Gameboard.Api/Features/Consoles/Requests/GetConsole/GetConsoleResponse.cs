// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;

namespace Gameboard.Api.Features.Consoles;

public sealed class GetConsoleResponse
{
    public required ConsoleState ConsoleState { get; set; }
    public required bool IsViewOnly { get; set; }
    public required DateTimeOffset ExpiresAt { get; set; }
}
