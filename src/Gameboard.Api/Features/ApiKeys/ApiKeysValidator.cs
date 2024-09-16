using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;

namespace Gameboard.Api.Features.ApiKeys;

public class ApiKeysValidator(IStore store, INowService now) : IModelValidator
{
    private readonly INowService _now = now;
    private readonly IStore _store = store;

    public Task Validate(object model)
    {
        if (model is DeleteApiKeyRequest)
            return Validate(model as DeleteApiKeyRequest);
        if (model is NewApiKey)
            return Validate(model as NewApiKey);

        throw new ValidationTypeFailure<ApiKeysValidator>(model.GetType());
    }

    private async Task Validate(NewApiKey model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
            throw new ApiKeyNoName();

        if (model.ExpiresOn.HasValue && model.ExpiresOn < _now.Get())
            throw new IllegalApiKeyExpirationDate(model.ExpiresOn.GetValueOrDefault(), _now.Get());

        if (!await _store.AnyAsync<Data.User>(u => u.Id == model.UserId, default))
            throw new ResourceNotFound<Data.User>(model.UserId);
    }

    private async Task Validate(DeleteApiKeyRequest request)
    {
        if (!await _store.AnyAsync<ApiKey>(k => k.Id == request.ApiKeyId, CancellationToken.None))
            throw new ResourceNotFound<ApiKey>(request.ApiKeyId);
    }
}
