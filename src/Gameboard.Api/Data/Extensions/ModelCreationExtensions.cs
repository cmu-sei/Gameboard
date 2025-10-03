// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gameboard.Api.Data;

public static class ModelCreationExtensions
{
    public static PropertyBuilder HasStandardGuidLength(this PropertyBuilder builder)
        => builder.HasMaxLength(40);

    public static PropertyBuilder HasStandardNameLength(this PropertyBuilder builder)
        => builder.HasMaxLength(64);

    public static PropertyBuilder HasStandardUrlLength(this PropertyBuilder builder)
        => builder.HasMaxLength(200);
}
