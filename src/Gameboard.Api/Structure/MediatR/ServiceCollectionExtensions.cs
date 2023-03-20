using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Microsoft.Extensions.DependencyInjection;

namespace Gameboard.Api.Structure;

internal static class MediatRExtensionsToServiceCollection
{
    internal static IServiceCollection AddGameboardMediatR(this IServiceCollection services)
    {
        return services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>())
            .AddConcretesFromNamespaceStartsWith("Gameboard.Api.Structure.MediatR")
            .AddConcretesFromNamespace("Gameboard.Api.Features.GameEngine.Queries")
            .AddImplementationsOf<IAuthorizer>()
            .AddImplementationsOf<IGameboardValidator>()
            .AddImplementationsOf(typeof(IGameboardValidator<,>))
            .AddImplementationsOf(typeof(IGameboardRequestValidator<>))
            .AddScoped<IValidatorService, ValidatorService>();
    }
}
