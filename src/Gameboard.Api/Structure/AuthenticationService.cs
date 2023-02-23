// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gameboard.Api
{
    public interface IAuthenticationService
    {
        TokenResponse GetToken(CancellationToken ct = new CancellationToken());
        void InvalidateToken();
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly object _lock = new();
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly CrucibleOptions _settings;
        private readonly ILogger<AuthenticationService> _logger;

        private TokenResponse _tokenResponse;

        public AuthenticationService(IHttpClientFactory httpClientFactory, CrucibleOptions settings, ILogger<AuthenticationService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _settings = settings;
            _logger = logger;
        }

        public TokenResponse GetToken(CancellationToken ct = new CancellationToken())
        {
            if (!ValidateToken())
            {
                lock (_lock)
                {
                    // Check again so we don't renew again if
                    // another thread already did while we were waiting on the lock
                    if (!ValidateToken())
                    {
                        _tokenResponse = RenewToken(ct);
                    }
                }
            }

            return _tokenResponse;
        }

        public void InvalidateToken()
        {
            _tokenResponse = null;
        }

        private bool ValidateToken()
        {
            if (_tokenResponse == null || _tokenResponse.ExpiresIn <= 3600)//_clientOptions.CurrentValue.TokenRefreshSeconds)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private TokenResponse RenewToken(CancellationToken ct)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("identity");
                var response = httpClient.RequestPasswordTokenAsync(new PasswordTokenRequest
                {
                    Address = _settings.TokenUrl,
                    ClientId = _settings.ClientId,
                    Scope = _settings.Scope,
                    UserName = _settings.UserName,
                    Password = _settings.Password
                }, ct).Result;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception renewing auth token.", ex);
            }

            return null;
        }
    }
}
