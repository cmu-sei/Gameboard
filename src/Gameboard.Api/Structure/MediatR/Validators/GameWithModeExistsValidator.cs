using System;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Structure.MediatR.Validators;

internal class GameWithModeExistsValidator<TModel> : IGameboardValidator<TModel>
{
    private Func<TModel, string> _idProperty;

    private readonly IStore _store;
    private string _requiredEngineMode;
    private bool _requiredSyncStartValue;

    public GameWithModeExistsValidator(IStore store)
    {
        _store = store;
    }

    public Func<TModel, RequestValidationContext, Task> GetValidationTask()
    {
        return async (model, context) =>
        {
            var id = _idProperty(model);
            var entity = await _store
                .WithNoTracking<Data.Game>()
                .Select(g => new
                {
                    g.Id,
                    EngineMode = g.Mode,
                    IsSyncStart = g.RequireSynchronizedStart
                })
                .SingleOrDefaultAsync(g => g.Id == id);

            if (entity is null)
                context.AddValidationException(new ResourceNotFound<Data.Game>(id));

            if (entity.EngineMode != _requiredEngineMode)
                context.AddValidationException(new GameHasUnexpectedEngineMode(id, _requiredEngineMode, entity.EngineMode));

            if (entity.IsSyncStart != _requiredSyncStartValue)
                context.AddValidationException(new GameHasUnexpectedSyncStart(id, _requiredSyncStartValue));
        };
    }

    public GameWithModeExistsValidator<TModel> UseIdProperty(Func<TModel, string> idProperty)
    {
        _idProperty = idProperty;
        return this;
    }

    public GameWithModeExistsValidator<TModel> WithEngineMode(string engineMode)
    {
        _requiredEngineMode = engineMode;
        return this;
    }

    public GameWithModeExistsValidator<TModel> WithSyncStartRequired(bool isRequired)
    {
        _requiredSyncStartValue = isRequired;
        return this;
    }
}
