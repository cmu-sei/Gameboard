// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Cors.Infrastructure;
using System;

namespace Gameboard.Api
{
    public class AppSettings
    {
        public string PathBase { get; set; }
        public OidcOptions Oidc { get; set; } = new OidcOptions();
        public CacheOptions Cache { get; set; } = new CacheOptions();
        public CoreOptions Core { get; set; } = new CoreOptions();
        public DatabaseOptions Database { get; set; } = new DatabaseOptions();
        public HeaderOptions Headers { get; set; } = new HeaderOptions();
        public OpenApiOptions OpenApi { get; set; } = new OpenApiOptions();
        public Defaults Defaults { get; set; } = new Defaults();

    }

    public class OidcOptions
    {
        public string Authority { get; set; } = "http://localhost:5000";
        public string Audience { get; set; } = "gameboard-api";
        public bool RequireHttpsMetadata { get; set; } = true;
        public int MksCookieMinutes { get; set; } = 60;
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
        public string Provider { get; set; } = "InMemory";
        public string ConnectionString { get; set; } = "gameboard_db";
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
        public string[] Origins { get; set; } = new string[]{};
        public string[] Methods { get; set; } = new string[]{};
        public string[] Headers { get; set; } = new string[]{};
        public bool AllowCredentials { get; set; }

        public CorsPolicy Build()
        {
            CorsPolicyBuilder policy = new CorsPolicyBuilder();

            var origins = Origins.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            if (origins.Any()) {
                if (origins.First() == "*") policy.AllowAnyOrigin(); else policy.WithOrigins(origins);
                if (AllowCredentials && origins.First() != "*") policy.AllowCredentials(); else policy.DisallowCredentials();
            }

            var methods = Methods.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            if (methods.Any()) {
                if (methods.First() == "*") policy.AllowAnyMethod(); else policy.WithMethods(methods);
            }

            var headers = Headers.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            if (headers.Any()) {
                if (headers.First() == "*") policy.AllowAnyHeader(); else policy.WithHeaders(headers);
            }

            policy.SetIsOriginAllowedToAllowWildcardSubdomains();

            return policy.Build();
        }
    }

    public class CoreOptions
    {
        public string GameEngineUrl { get; set; } = "http://localhost:5004";
        public string GamebrainUrl { get; set; } = "https://foundry.local/gamebrain";
        public string GameEngineClientName { get; set; }
        public string GameEngineClientSecret { get; set; }
        public int GameEngineMaxRetries { get; set; } = 2;
        public string ImageFolder { get; set; } = "wwwroot/img";
        public string DocFolder { get; set; } = "wwwroot/doc";
        public string SupportUploadsRequestPath { get; set; } = "supportfiles";
        public string SupportUploadsFolder { get; set; } = "wwwroot/supportfiles";
        public string ChallengeDocUrl { get; set; }
        public string SafeNamesFile { get; set; } = "names.json";
        public string KeyPrefix { get; set; } = "GB";
    }

    public class Defaults
    {
        public string FeedbackTemplateFile { get; set; }
        public string FeedbackTemplate { get; set; } = "";
        public string CertificateTemplateFile { get; set; }
        public string CertificateTemplate { get; set; } = "";
        // The timezone to format support shifts in
        public string ShiftTimezone { get; set; }
        public static string ShiftTimezoneFallback { get; set; } = "Eastern Standard Time";
        // The support shifts; each string[] is the shift start time and the shift end time
        public string[][] ShiftStrings { get; set; }
        public static string[][] ShiftStringsFallback { get; } = new string[][] {
            new string[] { "8:00 AM", "4:00 PM" },
            new string[] { "4:00 PM", "11:00 PM" }
        };
        // Get date-formatted versions of the shifts
        public DateTimeOffset[][] Shifts { get; set; }
        public static DateTimeOffset[][] ShiftsFallback { get; set; } = GetShifts(ShiftStringsFallback);

        // Helper method to format shifts as DateTimeOffset objects
        public static DateTimeOffset[][] GetShifts(string[][] shiftStrings) {
            DateTimeOffset[][] offsets = new DateTimeOffset[shiftStrings.Length][];
            // Create a new DateTimeOffset representation for every string time given
            for (int i = 0; i < shiftStrings.Length; i++)
            {
                offsets[i] = new DateTimeOffset[] { 
                    ConvertTime(shiftStrings[i][0], ShiftTimezoneFallback), 
                    ConvertTime(shiftStrings[i][1], ShiftTimezoneFallback) };
            }
            return offsets;
        }

        // Helper method to convert a given string time into a DateTimeOffset representation
        public static DateTimeOffset ConvertTime(string time, string shiftTimezone) {
            return TimeZoneInfo.ConvertTime(DateTimeOffset.Parse(time), TimeZoneInfo.FindSystemTimeZoneById(shiftTimezone));
        }
    }

}
