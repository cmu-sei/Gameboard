using System;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Structure.MediatR.Validators;

internal class EntityExistsValidator<TEntity> : IGameboardValidator where TEntity : class, IEntity
{
    private string _idValue;
    private readonly IStore _store;

    public EntityExistsValidator(IStore store)
    {
        _store = store;
    }

    public Func<RequestValidationContext, Task> GetValidationTask()
    {
        return async (context) =>
        {
            if (!await _store.Exists<TEntity>(_idValue))
                context.AddValidationException(new ResourceNotFound<TEntity>(_idValue));
        };
    }

    public EntityExistsValidator<TEntity> UseValue(string id)
    {
        _idValue = id;
        return this;
    }
}

public class EntityExistsValidator<TModel, TEntity> : IGameboardValidator<TModel>
    where TModel : class
    where TEntity : class, IEntity
{
    private readonly IStore _store;
    private Func<TModel, string> _idProperty;

    public EntityExistsValidator(IStore store)
    {
        _store = store;
    }

    public Func<TModel, RequestValidationContext, Task> GetValidationTask()
    {
        return async (model, context) =>
        {
            var id = _idProperty(model);
            if (!await _store.List<TEntity>().AnyAsync(e => e.Id == id))
                context.AddValidationException(new ResourceNotFound<TEntity>(id));
        };
    }

    public EntityExistsValidator<TModel, TEntity> UseProperty(Func<TModel, string> idProperty)
    {
        _idProperty = idProperty;
        return this;
    }
}
