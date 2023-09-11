using System;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Teams;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Structure.MediatR.Validators;

internal class TeamExistsValidator<TModel> : IGameboardValidator<TModel>
{
    private readonly IStore _store;
    private Func<TModel, string> _teamIdProperty;

    public TeamExistsValidator(IStore store)
    {
        _store = store;
    }

    public TeamExistsValidator<TModel> UseProperty(Func<TModel, string> propertyExpression)
    {
        _teamIdProperty = propertyExpression;
        return this;
    }

    public Func<TModel, RequestValidationContext, Task> GetValidationTask()
    {
        return async (model, context) =>
        {
            var teamId = _teamIdProperty(model);

            if (string.IsNullOrEmpty(teamId))
                context.AddValidationException(new MissingRequiredInput<string>(nameof(teamId), teamId));

            var count = await _store
                .List<Data.Player>()
                .Where(p => p.TeamId == teamId)
                .CountAsync();

            if (count == 0)
            {
                context.AddValidationException(new ResourceNotFound<Team>(teamId));
            }
        };
    }
}
