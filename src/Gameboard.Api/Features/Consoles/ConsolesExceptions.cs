// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.Consoles;

public sealed class ConsoleTeamNoAccessException(string teamId) : GameboardValidationException($"You can't access consoles owned by team {teamId}.")
{
}
