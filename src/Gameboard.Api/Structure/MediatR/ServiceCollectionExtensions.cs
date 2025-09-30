// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.Api.Structure.MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Gameboard.Api.Structure;

internal static class MediatRExtensionsToServiceCollection
{
    internal static IServiceCollection AddGameboardMediatR(this IServiceCollection services)
    {
        return services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>())
            .AddConcretesFromNamespaceStartsWith("Gameboard.Api.Structure.MediatR")
            .AddImplementationsOf<IGameboardValidator>()
            .AddImplementationsOf(typeof(IGameboardValidator<>))
            .AddImplementationsOf(typeof(IGameboardRequestValidator<>))
            .AddScoped(typeof(IValidatorService<>), typeof(ValidatorService<>));
    }
}
