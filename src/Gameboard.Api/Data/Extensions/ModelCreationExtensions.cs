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
