using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.ApiKeys;

public interface IApiKeysService
{
    Task<Data.User> Authenticate(string headerValue);
    Task<CreateApiKeyResult> Create(NewApiKey newApiKey);
    Task Delete(string apiKeyId);
    Task<Data.User> GetUserFromApiKey(string apiKey);
    Task<IEnumerable<ApiKeyViewModel>> ListKeys(string userId);
}

internal class ApiKeysService : IApiKeysService
{
    private readonly IGuidService _guids;
    private readonly IMapper _mapper;
    private readonly INowService _now;
    private readonly IRandomService _rng;
    private readonly IApiKeysStore _store;
    private readonly ApiKeyOptions _options;
    private readonly IStore<Data.User> _userStore;

    public ApiKeysService(
        ApiKeyOptions options,
        IGuidService guids,
        IMapper mapper,
        INowService now,
        IRandomService rng,
        IApiKeysStore store,
        IStore<Data.User> userStore)
    {
        _guids = guids;
        _mapper = mapper;
        _now = now;
        _rng = rng;
        _options = options;
        _store = store;
        _userStore = userStore;
    }

    public async Task<Data.User> Authenticate(string headerValue)
        => await GetUserFromApiKey(headerValue.Trim());

    public async Task<CreateApiKeyResult> Create(NewApiKey newApiKey)
    {
        var user = await _userStore.Retrieve(newApiKey.UserId);
        if (user is null)
            throw new ResourceNotFound<User>(newApiKey.UserId);

        var generatedKey = GenerateKey();

        var entity = new ApiKey
        {
            Id = _guids.GetGuid(),
            Name = newApiKey.Name,
            GeneratedOn = _now.Get(),
            ExpiresOn = newApiKey.ExpiresOn,
            Key = generatedKey.ToSha256(),
            OwnerId = newApiKey.UserId
        };

        await _store.Create(entity);

        var result = _mapper.Map<CreateApiKeyResult>(entity);
        result.PlainKey = generatedKey;

        return result;
    }

    public async Task Delete(string apiKeyId)
        => await _store.Delete(apiKeyId);

    public async Task<Data.User> GetUserFromApiKey(string apiKey)
    {
        var hashedKey = apiKey.ToSha256();

        return await _userStore
            .ListWithNoTracking()
            .Include(u => u.ApiKeys)
            // we use SingleOrDefaultAsync to ensure that we only get one result -
            // if we get more than one, some weird stuff is happening and we need to know.
            .SingleOrDefaultAsync(u => u.ApiKeys.Any(k => k.Key == hashedKey));
    }

    public async Task<IEnumerable<ApiKeyViewModel>> ListKeys(string userId)
    {
        return await _mapper
            .ProjectTo<ApiKeyViewModel>(_store.List(userId))
            .ToArrayAsync();
    }

    internal string GenerateKey()
    {
        var keyRaw = _rng.GetString(_options.RandomCharactersLength, generatedBytes: _options.BytesOfRandomness);
        return keyRaw.Substring(0, Math.Min(keyRaw.Length, _options.RandomCharactersLength));
    }

    internal bool IsValidKey(string hashedKey, ApiKey candidate)
        => hashedKey == candidate.Key &&
        (
            candidate.ExpiresOn == null ||
            DateTimeOffset.Compare(candidate.ExpiresOn.Value, _now.Get()) == 1
        );
}
