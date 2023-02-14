using System.Collections.Generic;
using System.Threading.Tasks;
using Gameboard.Api.Controllers;
using Gameboard.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.ApiKeys;

public class ApiKeysController : _Controller
{
    private readonly IApiKeysService _apiKeyService;

    public ApiKeysController
    (
        ILogger<ApiKeysController> logger,
        IDistributedCache cache,
        ApiKeysValidator validator,
        IApiKeysService apiKeyService
    ) : base(logger, cache, validator)
    {
        _apiKeyService = apiKeyService;
    }

    [HttpPost("api/api-keys")]
    [Authorize]
    public async Task<CreateApiKeyResult> CreateApiKey([FromBody] NewApiKey newApiKey)
    {
        AuthorizeAny
        (
            () => Actor.IsAdmin,
            () => Actor.IsRegistrar
        );

        await Validate(newApiKey);

        return await _apiKeyService.Create(newApiKey);
    }

    [HttpGet("api/users/{userId}/api-keys")]
    [Authorize]
    public async Task<IEnumerable<ApiKeyViewModel>> ListApiKeys(string userId)
    {
        AuthorizeAny
        (
            () => Actor.IsAdmin,
            () => Actor.IsRegistrar
        );

        await Validate(new ListApiKeysRequest { UserId = userId });

        return await _apiKeyService.ListKeys(userId);
    }

    [HttpDelete("api/api-keys/{keyId}")]
    [Authorize]
    public async Task DeleteApiKey(string keyId)
    {
        AuthorizeAny
        (
            () => Actor.IsAdmin,
            () => Actor.IsRegistrar
        );

        await Validate(new DeleteApiKeyRequest { ApiKeyId = keyId });

        await _apiKeyService.Delete(keyId);
    }
}
