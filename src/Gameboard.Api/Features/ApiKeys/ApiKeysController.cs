using System.Collections.Generic;
using System.Threading.Tasks;
using Gameboard.Api.Features.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.ApiKeys;

public class ApiKeysController(
    IApiKeysService apiKeyService,
    IUserRolePermissionsService permissionsService,
    ApiKeysValidator validator
    )
{
    private readonly IApiKeysService _apiKeyService = apiKeyService;
    private readonly IUserRolePermissionsService _permissionsService = permissionsService;
    private readonly ApiKeysValidator _validator = validator;

    [HttpPost("api/api-keys")]
    [Authorize]
    public async Task<CreateApiKeyResult> CreateApiKey([FromBody] NewApiKey newApiKey)
    {
        await Authorize();
        await _validator.Validate(newApiKey);
        return await _apiKeyService.Create(newApiKey);
    }

    [HttpGet("api/users/{userId}/api-keys")]
    [Authorize]
    public async Task<IEnumerable<ApiKeyViewModel>> ListApiKeys(string userId)
    {
        await Authorize();
        return await _apiKeyService.ListKeys(userId);
    }

    [HttpDelete("api/api-keys/{keyId}")]
    [Authorize]
    public async Task DeleteApiKey(string keyId)
    {
        await Authorize();
        await _validator.Validate(new DeleteApiKeyRequest { ApiKeyId = keyId });
        await _apiKeyService.Delete(keyId);
    }

    private async Task Authorize()
    {
        if (!await _permissionsService.Can(PermissionKey.Admin_CreateApiKeys))
            throw new ActionForbidden();
    }
}
