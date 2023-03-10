using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;

namespace Gameboard.Api.Structure.MediatR.Validators;

internal class EntityExistsValidator<TEntity> : IGameboardValidator<string, ResourceNotFound<TEntity>> where TEntity : class, IEntity
{
    private readonly IStore<TEntity> _store;

    public EntityExistsValidator(IStore<TEntity> store)
    {
        _store = store;
    }

    public async Task<ResourceNotFound<TEntity>> Validate(string id)
    {
        if (!(await _store.Exists(id)))
            return new ResourceNotFound<TEntity>(id);

        return null;
    }
}
