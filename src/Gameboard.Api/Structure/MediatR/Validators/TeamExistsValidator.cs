using System;
using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Structure.MediatR.Validators;

internal class TeamExistsValidator<TModel> : IGameboardValidator<TModel>
{
    private readonly IPlayerStore _playerStore;
    public required Func<TModel, string> TeamIdProperty { get; set; }

    public TeamExistsValidator(IPlayerStore playerStore)
    {
        _playerStore = playerStore;
    }

    public Func<TModel, RequestValidationContext, Task> GetValidationTask()
    {
        return async (model, context) =>
        {
            var teamId = TeamIdProperty(model);

            if (string.IsNullOrEmpty(teamId))
                context.AddValidationException(new MissingRequiredInput<string>(nameof(teamId), teamId));

            var count = await _playerStore.List().CountAsync(p => p.TeamId == teamId);
            if (count == 0)
            {
                context.AddValidationException(new ResourceNotFound<Team>(teamId));
            }
        };
    }

    public TeamExistsValidator<TModel> UseProperty(Func<TModel, string> propertyExpression)
    {
        TeamIdProperty = propertyExpression;
        return this;
    }
}
