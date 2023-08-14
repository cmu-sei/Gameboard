using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.ApiKeys;

public interface IApiKeysStore
{
    Task Delete(string id);
    Task<bool> Exists(string id);
    Task Create(ApiKey apiKey);
    IQueryable<ApiKey> List(string userId);
}

public class ApiKeysStore : IApiKeysStore
{
    private readonly GameboardDbContext _dbContext;

    public ApiKeysStore(GameboardDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Exists(string apiKeyId)
        => (await _dbContext
            .ApiKeys
            .SingleOrDefaultAsync(k => k.Id == apiKeyId)) != null;

    public async Task Create(ApiKey apiKey)
    {
        _dbContext
            .ApiKeys
            .Add(apiKey);

        await _dbContext.SaveChangesAsync();
    }

    public async Task Delete(string id)
    {
        await _dbContext
            .ApiKeys
            .Where(k => k.Id == id)
            .ExecuteDeleteAsync();
    }

    public IQueryable<ApiKey> List(string userId)
        => _dbContext
            .ApiKeys
            .AsNoTracking()
            .Where(k => k.OwnerId == userId);
}
