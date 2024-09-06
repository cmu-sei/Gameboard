using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
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

internal class ApiKeysService(
    ApiKeyOptions options,
    IGuidService guids,
    IMapper mapper,
    INowService now,
    IRandomService rng,
    IStore store) : IApiKeysService
{
    private readonly IGuidService _guids = guids;
    private readonly IMapper _mapper = mapper;
    private readonly INowService _now = now;
    private readonly IRandomService _rng = rng;
    private readonly IStore _store = store;
    private readonly ApiKeyOptions _options = options;

    public async Task<Data.User> Authenticate(string headerValue)
        => await GetUserFromApiKey(headerValue.Trim());

    public async Task<CreateApiKeyResult> Create(NewApiKey newApiKey)
    {
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

    public Task Delete(string apiKeyId)
        => _store.Delete<ApiKey>(apiKeyId);

    public async Task<Data.User> GetUserFromApiKey(string apiKey)
    {
        var hashedKey = apiKey.ToSha256();

        return await _store
            .WithNoTracking<Data.User>()
            .Include(u => u.ApiKeys)
            // we use SingleOrDefaultAsync to ensure that we only get one result -
            // if we get more than one, some weird stuff is happening and we need to know.
            .SingleOrDefaultAsync(u => u.ApiKeys.Any(k => k.Key == hashedKey));
    }

    public async Task<IEnumerable<ApiKeyViewModel>> ListKeys(string userId)
    {
        var query = _store
            .WithNoTracking<ApiKey>()
            .Where(k => k.OwnerId == userId);

        return await _mapper
            .ProjectTo<ApiKeyViewModel>(query)
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
