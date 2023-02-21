using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Validators;

namespace Gameboard.Api.Features.ApiKeys;

public class ApiKeysValidator : IModelValidator
{
    private readonly INowService _now;
    private readonly IApiKeysStore _store;
    private readonly UserValidator _userValidator;

    public ApiKeysValidator(IApiKeysStore store, INowService now, UserValidator userValidator)
    {
        _now = now;
        _store = store;
        _userValidator = userValidator;
    }

    public Task Validate(object model)
    {
        if (model is DeleteApiKeyRequest)
            return _validate(model as DeleteApiKeyRequest);
        if (model is NewApiKey)
            return _validate(model as NewApiKey);
        if (model is ListApiKeysRequest)
            return _validate(model as ListApiKeysRequest);

        throw new ValidationTypeFailure<ApiKeysValidator>(model.GetType());
    }

    private async Task _validate(NewApiKey model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
            throw new ApiKeyNoName();

        if (model.ExpiresOn.HasValue && model.ExpiresOn < _now.Now())
            throw new IllegalApiKeyExpirationDate(model.ExpiresOn.GetValueOrDefault(), _now.Now());

        await _userValidator.Validate(new Entity { Id = model.UserId });
    }

    private async Task _validate(DeleteApiKeyRequest request)
    {
        if ((await _store.Exists(request.ApiKeyId)).Equals(false))
            throw new ResourceNotFound<ApiKey>(request.ApiKeyId);
    }

    private async Task _validate(ListApiKeysRequest request)
    {
        await _userValidator.Validate(new Entity { Id = request.UserId });
    }
}
