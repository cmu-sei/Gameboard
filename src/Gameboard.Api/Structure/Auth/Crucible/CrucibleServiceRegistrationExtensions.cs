// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.Extensions.DependencyInjection;

namespace Gameboard.Api.Structure.Auth.Crucible;

public sealed class CrucibleServiceAccountAuthenticationConfig
{
    public required string OidcAudience { get; set; }
    public required string OidcAuthority { get; set; }
    public required string ClientId { get; set; }
    public required string ClientSecret { get; set; }

    /// <summary>
    /// An access token that will expire in this amount of time or less (in seconds) will automatically
    /// be renewed before it's attached to a request. Defaults to 120 (2 minutes).
    /// </summary>
    public int RenewTokenThreshold { get; set; } = 120;
}

public static class CrucibleServiceRegistrationExtensions
{
    public static IServiceCollection AddCrucibleServiceAccountAuthentication(this IServiceCollection services, CrucibleServiceAccountAuthenticationConfig config)
    {
        services.AddSingleton(config);
        services.AddSingleton<ICrucibleServiceAccountTokenService, CrucibleServiceAccountTokenService>();
        services.AddTransient<CrucibleServiceAccountBearerTokenHandler>();

        return services;
    }
}
