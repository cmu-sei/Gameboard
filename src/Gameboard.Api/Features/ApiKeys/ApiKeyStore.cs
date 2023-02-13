using System.Threading.Tasks;
using Gameboard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.ApiKeys;

public interface IApiKeyStore
{
    Task<Data.User> GetUserWithApiKeys(string apiKeyOwnerId);
    Task Create(ApiKey apiKey);
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

    public async Task Create(ApiKey apiKey)
    {
        _dbContext
            .ApiKeys
            .Add(apiKey);

        await _dbContext.SaveChangesAsync();
    }
}
