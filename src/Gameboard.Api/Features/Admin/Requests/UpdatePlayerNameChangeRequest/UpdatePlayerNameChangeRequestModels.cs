// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Gameboard.Api.Features.Admin;

public sealed class UpdatePlayerNameChangeRequestArgs
{
    public required string ApprovedName { get; set; }
    public required string RequestedName { get; set; }
    public required string Status { get; set; }
}
