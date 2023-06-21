using System;
using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.Teams;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Structure.MediatR.Validators;

internal class TeamExistsValidator<TModel> : IGameboardValidator<TModel>
{
    private readonly IPlayerStore _playerStore;
    private Func<TModel, string> _teamIdProperty;

    public TeamExistsValidator(IPlayerStore playerStore)
    {
        _playerStore = playerStore;
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

            var count = await _playerStore.List().CountAsync(p => p.TeamId == teamId);
            if (count == 0)
            {
                context.AddValidationException(new ResourceNotFound<Team>(teamId));
            }
        };
    }
}
