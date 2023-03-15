using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gameboard.Api.Data;

public static class ModelCreationExtensions
{
    public static PropertyBuilder HasStandardGuidLength(this PropertyBuilder builder)
    {
        return builder.HasMaxLength(40);
    }

    public static PropertyBuilder HasStandardNameLength(this PropertyBuilder builder)
    {
        return builder.HasMaxLength(64);
    }
}
