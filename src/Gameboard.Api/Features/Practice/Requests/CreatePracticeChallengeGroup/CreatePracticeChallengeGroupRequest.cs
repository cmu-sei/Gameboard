// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Gameboard.Api.Features.Practice;

public sealed class CreatePracticeChallengeGroupRequest
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public IFormFile Image { get; set; }
    public required bool IsFeatured { get; set; }
    public string ParentGroupId { get; set; }
}
