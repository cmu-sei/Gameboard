using System;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Data;
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
    private readonly INowService _now;
    private readonly IHashService _hasher;
    private readonly IRandomService _rng;
    private readonly IApiKeyStore _store;
    private readonly ApiKeyOptions _options;

    public ApiKeyService(
        ApiKeyOptions options,
        INowService now,
        IHashService hasher,
        IRandomService rng,
        IApiKeyStore store)
    {
        _hasher = hasher;
        _now = now;
        _rng = rng;
        _options = options;
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
        var hashedKey = _hasher.Hash(plainKey);

        return user.ApiKeys.Any(k => IsValidKey(hashedKey, k)) ? user : null;
    }

    public bool IsEnabled() => _options.IsEnabled;

    public ApiKeyHash GenerateKey(Data.User user)
    {
        var plainKey = GeneratePlainKey();

        return new ApiKeyHash
        {
            UserApiKey = plainKey,
            HashedApiKey = _hasher.Hash(plainKey)
        };
    }

    internal string GeneratePlainKey()
    {
        var keyRandomness = _rng.GetString(generatedBytes: _options.BytesOfRandomness);
        var keyRaw = $"{_options.KeyPrefix}{keyRandomness}";

        return keyRaw.Substring(0, Math.Min(keyRaw.Length, _options.KeyPrefix.Length + _options.RandomCharactersLength));
    }

    internal bool IsValidKey(string hashedKey, Data.ApiKey candidate)
        => hashedKey == candidate.Key &&
        (
            candidate.ExpiresOn == null ||
            DateTimeOffset.Compare(candidate.ExpiresOn.Value, _now.Now()) == 1
        );
}
