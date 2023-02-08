using System;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Services;
using Microsoft.AspNetCore.Identity;

namespace Gameboard.Api.Features.ApiKeys;

public interface IApiKeyService
{
    Task<Data.User> Authenticate(string headerValue);
    bool IsEnabled();
    ApiKeyHash GenerateKey(Data.User user);
}

internal class ApiKeyService : IApiKeyService
{
    private readonly IPasswordHasher<Data.User> _hasher;
    private readonly IRandomService _rng;
    private readonly IApiKeyStore _store;
    private readonly AppSettings _settings;

    public ApiKeyService(
        AppSettings settings,
        IPasswordHasher<Data.User> hasher,
        IRandomService rng,
        IApiKeyStore store)
    {
        _hasher = hasher;
        _rng = rng;
        _settings = settings;
        _store = store;
    }

    public async Task<Data.User> Authenticate(string headerValue)
    {
        var splits = headerValue.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (splits.Length != 2)
            throw new InvalidApiKeyFormat(headerValue);

        var ownerId = splits[0];
        var plainKey = splits[1];

        var user = await _store.GetUserWithApiKeys(ownerId);
        return user != null && user.ApiKeys.Any(k => k.Key == _hasher.HashPassword(user, plainKey)) ? user : null;
    }

    public bool IsEnabled() => _settings.ApiKey.IsEnabled;

    public ApiKeyHash GenerateKey(Data.User user)
    {
        var plainKey = GeneratePlainKey();

        return new ApiKeyHash
        {
            UserApiKey = plainKey,
            HashedApiKey = _hasher.HashPassword(user, plainKey)
        };
    }

    internal string GeneratePlainKey()
    {
        var keyRandomness = _rng.GetString(generatedBytes: _settings.ApiKey.BytesOfRandomness);
        var keyRaw = $"{_settings.ApiKey.KeyPrefix}{keyRandomness}";

        return keyRaw.Substring(0, Math.Min(keyRaw.Length, _settings.ApiKey.KeyPrefix.Length + _settings.ApiKey.RandomCharactersLength));
    }
}
