using System;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;

namespace Gameboard.Api.Structure.MediatR.Validators;

public class EntityExistsValidator<TEntity>(IStore store) : IGameboardValidator where TEntity : class, IEntity
{
    public string IdValue { get; private set; }
    private readonly IStore _store = store;

    public EntityExistsValidator<TEntity> UseValue(string idValue)
    {
        IdValue = idValue;
        return this;
    }

    public Func<RequestValidationContext, Task> GetValidationTask(CancellationToken cancellationToken)
    {
        if (IdValue.IsEmpty())
            throw new InvalidOperationException($"Can't evaluate if entity exists - id value not configured.");

        return async (ctx) =>
        {
            if (!await _store.AnyAsync<TEntity>(e => e.Id == IdValue, cancellationToken))
                ctx.AddValidationException(new ResourceNotFound<TEntity>(IdValue));
        };
    }
}

public class EntityExistsValidator<TModel, TEntity>(IStore store) : IGameboardValidator<TModel>
    where TModel : class
    where TEntity : class, IEntity
{
    private readonly IStore _store = store;
    private Func<TModel, string> _idProperty;

    public Func<TModel, RequestValidationContext, Task> GetValidationTask()
    {
        return async (model, context) =>
        {
            var id = _idProperty(model);
            if (!await _store.AnyAsync<TEntity>(e => e.Id == id, default))
                context.AddValidationException(new ResourceNotFound<TEntity>(id));
        };
    }

    public EntityExistsValidator<TModel, TEntity> UseProperty(Func<TModel, string> idProperty)
    {
        _idProperty = idProperty;
        return this;
    }
}
