// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.Users;

public sealed class RequestNameChangeRequest
{
    public required string RequestedName { get; set; }
    public string Status { get; set; }
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
