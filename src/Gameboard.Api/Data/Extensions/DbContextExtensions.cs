using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Data;

public static class DbContextExtensions
{
    /// <summary>
    /// Sets all entities attached to the DbContext's changetracker in state Unchanged to
    /// Detached instead.
    /// 
    /// We use this in cases where some previous action in a request state has caused entities
    /// to become attached, but we no longer have access to that action's context. For example,
    /// some component might load challenges to read them and incorrectly keep them attached.
    /// When another component tries to read the same challenges, EF complains because it's already
    /// tracking entities with the same IDs.
    /// 
    /// WARNING: Use this sparingly. The fact that we ever need this is suggestive that we need to 
    /// evaluate how IStore is consumed and how it uses dbContext. 
    /// </summary>
    /// <typeparam name="TDbContext"></typeparam>
    /// <param name="dbContext"></param>
    /// <returns></returns>
    public static TDbContext DetachUnchanged<TDbContext>(this TDbContext dbContext) where TDbContext : DbContext
    {
        foreach (var entry in dbContext.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Unchanged)
            {
                entry.State = EntityState.Detached;
            }
        }

        return dbContext;
    }
}
