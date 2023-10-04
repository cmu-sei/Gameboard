// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.Extensions.Logging;
using Polly;

namespace Gameboard.Api
{
    public class AuthenticatingHandler : DelegatingHandler
    {
        private readonly IAsyncPolicy<HttpResponseMessage> _policy;
        private readonly IAuthenticationService _authenticationService;
        private readonly ILogger<AuthenticatingHandler> _logger;

        private TokenResponse _token;
        private AuthenticationHeaderValue _authenticationHeader;

        public AuthenticatingHandler(IAuthenticationService authenticationService, ILogger<AuthenticatingHandler> logger)
        {
            _authenticationService = authenticationService;
            _logger = logger;

            // Create a policy that tries to renew the access token if a 401 Unauthorized is received.
            _policy = Policy.HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.Unauthorized)
            .WaitAndRetryForeverAsync(retryAttempt =>
            {
                _logger.LogError($"Retrying connection after 401");
                Authenticate(true);
                return TimeSpan.FromSeconds(Math.Min(Math.Pow(2, retryAttempt), 120));
            });
        }

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_authenticationHeader == null)
            {
                Authenticate(false);
            }

            // Try to perform the request, re-authenticating gracefully if the call fails due to an expired or revoked access token.
            var result = await _policy.ExecuteAndCaptureAsync(async () =>
            {
                request.Headers.Authorization = _authenticationHeader;
                return await base.SendAsync(request, cancellationToken);
            });

            return result.Result ?? result.FinalHandledResult;
        }

        private void Authenticate(bool forceRefresh)
        {
            if (forceRefresh)
                _authenticationService.InvalidateToken();

            _token = _authenticationService.GetToken();

            if (!_token.IsError)
            {
                _authenticationHeader = new AuthenticationHeaderValue(_token.TokenType, _token.AccessToken);
            }
            else
            {
                _logger.LogError($"Error in {nameof(AuthenticatingHandler)}: {_token.Error}");
            }
        }
    }
}
