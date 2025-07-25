// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using Gameboard.Api.Data;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Serilog.Events;

namespace Gameboard.Api;

public class AppSettings
{
    public ApiKeyOptions ApiKey { get; set; } = new ApiKeyOptions();
    public CacheOptions Cache { get; set; } = new CacheOptions();
    public CoreOptions Core { get; set; } = new CoreOptions();
    public CrucibleOptions Crucible { get; set; } = new CrucibleOptions();
    public DatabaseOptions Database { get; set; } = new DatabaseOptions();
    public Defaults Defaults { get; set; } = new Defaults();
    public HeaderOptions Headers { get; set; } = new HeaderOptions();
    public LoggingSettings Logging { get; set; } = new LoggingSettings();
    public OidcOptions Oidc { get; set; } = new OidcOptions();
    public OpenApiOptions OpenApi { get; set; } = new OpenApiOptions();
    public string PathBase { get; set; }
}

public class ApiKeyOptions
{
    public int BytesOfRandomness { get; set; } = 32;
    public int RandomCharactersLength { get; set; } = 36;
}

public class LoggingSettings
{
    public LogEventLevel MinimumLogLevel { get; set; } = LogEventLevel.Information;
    public string[] NamespacesErrorLevel { get; set; } = [];
    public string[] NamespacesFatalLevel { get; set; } = [];
    public string[] NamespacesInfoLevel { get; set; } = [];
    public string[] NamespacesWarningLevel { get; set; } = [];
    public string SeqInstanceApiKey { get; set; }
    public string SeqInstanceUrl { get; set; }
}

public class OidcOptions
{
    public string Authority { get; set; } = "http://localhost:5000";
    public string Audience { get; set; } = "gameboard-api";
    public string DefaultUserNameClaimType { get; set; } = null;
    public bool DefaultUserNameInferFromEmail { get; set; } = false;
    public int MksCookieMinutes { get; set; } = 60;
    public bool RequireHttpsMetadata { get; set; } = true;
    public bool StoreUserEmails { get; set; } = false;
}

public class OpenIdClient
{
    public string ClientId { get; set; }
    public string ClientName { get; set; }
    public string ClientSecret { get; set; }
}

public class OAuth2Client
{
    public string AuthorizationUrl { get; set; }
    public string TokenUrl { get; set; }
    public string ClientId { get; set; }
    public string ClientName { get; set; }
    public string ClientSecret { get; set; }
}

public class OpenApiOptions
{
    public string ApiName { get; set; } = "Gameboard.Api";
    public bool Enabled { get; set; } = true;
    public OAuth2Client Client { get; set; } = new OAuth2Client();
}

public class CacheOptions
{
    public string Key { get; set; }
    public string RedisUrl { get; set; }
    public string SharedFolder { get; set; }
    public string DataProtectionFolder { get; set; } = ".dpk";
    public int CacheExpirationSeconds { get; set; } = 300;
}

public class DatabaseOptions
{
    public string AdminId { get; set; }
    public string AdminName { get; set; } = "Gameboard Admin";
    public UserRoleKey AdminRole { get; set; } = UserRoleKey.Admin;
    public string ConnectionString { get; set; } = "Server=localhost;Port=5432;Database=gameboard;User ID=setmepls;Password=setmepls;";
    public string SeedFile { get; set; } = "seed-data.json";
}

public class HeaderOptions
{
    public bool LogHeaders { get; set; }
    public bool UseHsts { get; set; }
    public CorsPolicyOptions Cors { get; set; } = new CorsPolicyOptions();
    public SecurityHeaderOptions Security { get; set; } = new SecurityHeaderOptions();
    public ForwardHeaderOptions Forwarding { get; set; } = new ForwardHeaderOptions();
}

public class ForwardHeaderOptions
{
    public int ForwardLimit { get; set; } = 1;
    public string KnownProxies { get; set; } = "127.0.0.1 ::1";
    public string KnownNetworks { get; set; } = "10.0.0.0/8 172.16.0.0/12 192.168.0.0/24 ::ffff:a00:0/104 ::ffff:ac10:0/108 ::ffff:c0a8:0/120";
    public string TargetHeaders { get; set; } = "None";
    public string ForwardedForHeaderName { get; set; }
}

public class SecurityHeaderOptions
{
    public string ContentSecurity { get; set; } = "default-src 'self' 'unsafe-inline'";
    public string XContentType { get; set; } = "nosniff";
    public string XFrame { get; set; } = "SAMEORIGIN";
}

public class CorsPolicyOptions
{
    public string Name { get; set; } = "default";
    public string[] Origins { get; set; } = [];
    public string[] Methods { get; set; } = [];
    public string[] Headers { get; set; } = [];
    public bool AllowCredentials { get; set; }

    public CorsPolicy Build()
    {
        var policy = new CorsPolicyBuilder();

        var origins = Origins.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        if (origins.Length > 0)
        {
            if (origins.First() == "*") policy.AllowAnyOrigin(); else policy.WithOrigins(origins);
            if (AllowCredentials && origins.First() != "*") policy.AllowCredentials(); else policy.DisallowCredentials();
        }

        var methods = Methods.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        if (methods.Length > 0)
        {
            if (methods.First() == "*") policy.AllowAnyMethod(); else policy.WithMethods(methods);
        }

        var headers = Headers.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        if (headers.Length > 0)
        {
            if (headers.First() == "*") policy.AllowAnyHeader(); else policy.WithHeaders(headers);
        }

        policy.SetIsOriginAllowedToAllowWildcardSubdomains();

        return policy.Build();
    }
}

public class CoreOptions
{
    public string AppName { get; set; } = "Gameboard";
    public string AppUrl { get; set; } = "http://localhost:4202";
    public string ChallengeDocUrl { get; set; }
    public string DocFolder { get; set; } = "wwwroot/doc";
    public string GamebrainApiKey { get; set; }
    public string GameEngineClientName { get; set; }
    public string GameEngineClientSecret { get; set; }
    public int GameEngineDeployBatchSize { get; set; } = 2;
    public string GameEngineUrl { get; set; } = "http://localhost:5004";
    public int GameEngineMaxRetries { get; set; } = 2;
    public GameEngineType GameEngineType { get; set; } = GameEngineType.TopoMojo;
    public string ImageFolder { get; set; } = "wwwroot/img";
    public string KeyPrefix { get; set; } = "GB";
    public bool MojoEnabled { get; set; } = true;
    public bool NameChangeIsEnabled { get; set; } = true;
    public bool NameChangeRequiresApproval { get; set; } = true;
    public bool NamesImportFromIdp { get; set; } = false;
    public int PracticeDefaultSessionLength { get; set; } = 60;
    public int PracticeMaxSessionLength { get; set; } = 240;
    public string ExportFolder { get; set; } = "wwwroot/export";
    public string ImportFolder { get; set; } = "wwwroot/import";
    public string SafeNamesFile { get; set; } = "names.json";
    public string SupportUploadsRequestPath { get; set; } = "supportfiles";
    public string SupportUploadsFolder { get; set; } = "wwwroot/supportfiles";
    public string TempDirectory { get; set; } = "wwwroot/temp";
    public string TemplatesDirectory { get; set; } = "wwwroot/templates";
    public string WebHostRoot { get; set; }
}

public class CrucibleOptions
{
    public string ApiUrl { get; set; } = "http://localhost:4402/api";
    public string ClientId { get; set; } = "";
    public bool Enabled { get; set; } = false;
    public string Password { get; set; } = "";
    public string UserName { get; set; } = "";
    public string Scope { get; set; } = "";
    public string TokenUrl { get; set; } = "http://localhost:5000/connect/token";
}

public class Defaults
{
    public string DefaultSponsor { get; set; }
    public string FeedbackTemplateFile { get; set; }
    public string FeedbackTemplate { get; set; } = "";
    public string CertificateTemplateFile { get; set; }
    public string CertificateTemplate { get; set; } = "";
    // The timezone to format support shifts in
    public string ShiftTimezone { get; set; }
    public static string ShiftTimezoneFallback { get; set; } = "Eastern Standard Time";
    // The support shifts; each string[] is the shift start time and the shift end time
    public string[][] ShiftStrings { get; set; }
    public static string[][] ShiftStringsFallback { get; } = [
        ["8:00 AM", "4:00 PM"],
        ["4:00 PM", "11:00 PM"]
    ];
    // Get date-formatted versions of the shifts
    public DateTimeOffset[][] Shifts { get; set; }
    public static DateTimeOffset[][] ShiftsFallback { get; set; } = GetShifts(ShiftStringsFallback);

    // Helper method to format shifts as DateTimeOffset objects
    public static DateTimeOffset[][] GetShifts(string[][] shiftStrings)
    {
        var offsets = new DateTimeOffset[shiftStrings.Length][];
        // Create a new DateTimeOffset representation for every string time given
        for (int i = 0; i < shiftStrings.Length; i++)
        {
            offsets[i] =
            [
                ConvertTime(shiftStrings[i][0], ShiftTimezoneFallback),
                ConvertTime(shiftStrings[i][1], ShiftTimezoneFallback)
            ];
        }
        return offsets;
    }

    // Helper method to convert a given string time into a DateTimeOffset representation
    public static DateTimeOffset ConvertTime(string time, string shiftTimezone)
    {
        return TimeZoneInfo.ConvertTime(DateTimeOffset.Parse(time), TimeZoneInfo.FindSystemTimeZoneById(shiftTimezone));
    }
}
