using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;

namespace Gameboard.Api.Structure.MediatR.Validators;

internal class EntityExistsValidator<TEntity> : IGameboardValidator where TEntity : class, IEntity
{
    private readonly IStore<TEntity> _store;

    public EntityExistsValidator(IStore<TEntity> store)
    {
        _store = store;
    }

    public Task<GameboardValidationException> Validate<TModel>(TModel model)
    {
        return ValidateEntity(model.ToString());
    }

    private async Task<GameboardValidationException> ValidateEntity(string id)
    {
        if (!(await _store.Exists(id)))
            return new ResourceNotFound<TEntity>(id);

        return null;
    }
}
