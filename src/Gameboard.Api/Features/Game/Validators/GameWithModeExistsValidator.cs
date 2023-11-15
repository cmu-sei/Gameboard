using System;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Games.External;

internal class GameWithModeExistsValidator<TModel> : IGameboardValidator<TModel>
{
    private Func<TModel, string> _idProperty;

    private readonly IStore _store;
    private Func<TModel, string> _engineModeProperty;
    private Func<TModel, bool> _requireSyncStartProperty;
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
                    Id = g.Id,
                    IsExternal = g.Mode == GameEngineMode.External,
                    IsSyncStart = g.RequireSynchronizedStart
                })
                .SingleOrDefaultAsync(g => g.Id == id);

            if (entity is null)
                context.AddValidationException(new ResourceNotFound<Data.Game>(id));
        };
    }

    public GameWithModeExistsValidator<TModel> UseIdProperty(Func<TModel, string> idProperty)
    {
        _idProperty = idProperty;
        return this;
    }

    public GameWithModeExistsValidator<TModel> WithEngineMode(string engineMode, Func<TModel, string> engineModeProperty)
    {
        _engineModeProperty = engineModeProperty;
        _requiredEngineMode = engineMode;

        return this;
    }

    public GameWithModeExistsValidator<TModel> WithSyncStartRequired(bool isRequired, Func<TModel, bool> syncStartRequiredProperty)
    {
        _requiredSyncStartValue = isRequired;
        _requireSyncStartProperty = syncStartRequiredProperty;

        return this;
    }
}
