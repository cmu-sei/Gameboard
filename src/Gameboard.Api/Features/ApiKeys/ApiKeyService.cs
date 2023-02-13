using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data;
using Gameboard.Api.Services;

namespace Gameboard.Api.Features.ApiKeys;

public interface IApiKeyService
{
    Task<Data.User> Authenticate(string headerValue);
    Task<CreateApiKeyResult> CreateKey(NewApiKey newApiKey);
    bool IsEnabled();
}

internal class ApiKeyService : IApiKeyService
{
    private readonly IGuidService _guids;
    private readonly IMapper _mapper;
    private readonly INowService _now;
    private readonly IHashService _hasher;
    private readonly IRandomService _rng;
    private readonly IApiKeyStore _store;
    private readonly ApiKeyOptions _options;

    public ApiKeyService(
        ApiKeyOptions options,
        IGuidService guids,
        IMapper mapper,
        INowService now,
        IHashService hasher,
        IRandomService rng,
        IApiKeyStore store)
    {
        _guids = guids;
        _hasher = hasher;
        _mapper = mapper;
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

    public async Task<CreateApiKeyResult> CreateKey(NewApiKey newApiKey)
    {
        var generatedKey = GenerateKey();

        var entity = new ApiKey
        {
            Id = _guids.GetGuid(),
            Name = newApiKey.Name,
            GeneratedOn = _now.Now(),
            ExpiresOn = newApiKey.ExpiryDate,
            Key = generatedKey.HashedApiKey,
            OwnerId = newApiKey.UserId
        };

        await _store.Create(entity);

        var result = _mapper.Map<CreateApiKeyResult>(entity);
        result.UnhashedKey = generatedKey.UserApiKey;

        return result;
    }

    internal ApiKeyHash GenerateKey()
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
