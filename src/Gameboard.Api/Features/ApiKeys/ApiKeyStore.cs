using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.ApiKeys;

public interface IApiKeyStore
{
    Task<Data.User> GetUserWithApiKeys(string apiKeyOwnerId);
}

public class ApiKeyStore : IApiKeyStore
{
    private readonly GameboardDbContext _dbContext;

    public ApiKeyStore(GameboardDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Data.User> GetUserWithApiKeys(string apiKeyOwnerId)
        => await _dbContext
            .Users
                .Include(u => u.ApiKeys)
            .SingleOrDefaultAsync(u => u.ApiKeyOwnerId == apiKeyOwnerId);

    // public Task<ApiKey> Create(ApiKey entity)
    // {
    //     throw new NotImplementedException();
    // }

    // public Task<IEnumerable<ApiKey>> Create(IEnumerable<ApiKey> range)
    // {
    //     throw new NotImplementedException();
    // }

    // public Task Delete(string id)
    // {
    //     throw new NotImplementedException();
    // }

    // public IQueryable<ApiKey> List(string term = null)
    // {
    //     throw new NotImplementedException();
    // }

    // public Task<ApiKey> Retrieve(string id)
    // {
    //     throw new NotImplementedException();
    // }

    // public Task<ApiKey> Retrieve(string id, Func<IQueryable<ApiKey>, IQueryable<ApiKey>> includes)
    // {
    //     throw new NotImplementedException();
    // }

    // public Task Update(ApiKey entity)
    // {
    //     throw new NotImplementedException();
    // }

    // public Task Update(IEnumerable<ApiKey> range)
    // {
    //     throw new NotImplementedException();
    // }
}
