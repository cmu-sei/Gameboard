using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Gameboard.Api.Tests.Unit;

public class GameImportExportModelsMappingTests
{
    [Fact]
    public void GameImportExportExternalHosts_ExportsProperties()
    {
        var allowSkipProperties = new string[] { "HostApiKey", "UsedByGames" };
        var properties = typeof(GameImportExportExternalHost).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var propertyNames = properties.Select(p => p.Name).ToArray();
        var gameProperties = typeof(ExternalGameHost)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(p => !p.HasAttribute<NotMappedAttribute>());

        gameProperties.Where
        (
            p => !propertyNames.Contains(p.Name) && !allowSkipProperties.Contains(p.Name)
        )
        .ToArray()
        .Length.ShouldBe(0);
    }

    [Fact]
    public void GameImportExportExternalHosts_AttributeStrategy_ExportsProperties()
    {
        var properties = typeof(GameImportExportExternalHost).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var propertyNames = properties.Select(p => p.Name).ToArray();
        var gameProperties = typeof(ExternalGameHost)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(p => !p.HasAttribute<NotMappedAttribute>())
            .Where(p => !p.HasAttribute<DontExportAttribute>());

        gameProperties.Where(p => !propertyNames.Contains(p.Name))
        .ToArray()
        .Length.ShouldBe(0);
    }
}
