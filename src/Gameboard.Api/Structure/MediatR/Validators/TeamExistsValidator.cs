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
    public required Func<TModel, string> TeamIdProperty { get; set; }

    public TeamExistsValidator(IStore store) => _store = store;

    public Func<TModel, RequestValidationContext, Task> GetValidationTask()
    {
        return async (model, context) =>
        {
            var teamId = TeamIdProperty(model);

            if (string.IsNullOrEmpty(teamId))
                context.AddValidationException(new MissingRequiredInput<string>(nameof(teamId), teamId));

            // grab the gameId as a representation of each player, because we also need to know if they're somehow
            // in different games
            var players = await _store
                .ListAsNoTracking<Data.Player>()
                .ToListAsync();

            if (!players.Any())
                context.AddValidationException(new ResourceNotFound<Team>(teamId));

            var gameIds = players.Select(p => p.GameId);
            if (gameIds.Distinct().Count() > 1)
                context.AddValidationException(new PlayersAreInMultipleGames(gameIds));

            var teamIds = players.Select(p => p.TeamId);
            if (teamIds.Distinct().Count() > 1)
                context.AddValidationException(new PlayersAreFromMultipleTeams(teamIds));
        };
    }

    public TeamExistsValidator<TModel> UseProperty(Func<TModel, string> propertyExpression)
    {
        TeamIdProperty = propertyExpression;
        return this;
    }
}
