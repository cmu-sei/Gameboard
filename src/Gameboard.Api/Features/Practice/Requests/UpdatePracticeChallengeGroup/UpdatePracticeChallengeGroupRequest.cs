// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Gameboard.Api.Features.Practice;

public sealed class UpdatePracticeChallengeGroupRequest
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public IFormFile Image { get; set; }
    public required bool IsFeatured { get; set; }
    public required string ParentGroupId { get; set; }

    // caller can either leave the image unchanged, replace it, or just delete the old one
    // non-null Image == replace
    // null Image && RemoveImage == delete 
    // null Image && !RemoveImage = leave unchanged
    public bool RemoveImage { get; set; }
}
