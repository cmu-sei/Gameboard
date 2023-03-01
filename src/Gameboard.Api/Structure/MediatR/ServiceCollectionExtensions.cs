using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using MediatR.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace Gameboard.Api.Structure;

internal static class MediatRExtensionsToServiceCollection
{
    internal static IServiceCollection AddGameboardMediatR(this IServiceCollection services)
    {
        return services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>())
            .AddScoped(typeof(IPipelineBehavior<,>), typeof(AuthorizationPipelineStep<,>))
            .AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineStep<,>))
            .AddScoped(typeof(GameboardPipelineContextService<,>))
            .AddScoped(typeof(IGameboardMediator<,>), typeof(GameboardMediator<,>))
            .AddScoped<GetGameStateValidator>();
    }
}
