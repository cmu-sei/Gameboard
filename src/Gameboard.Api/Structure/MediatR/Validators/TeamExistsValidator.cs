using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Teams;
using Microsoft.EntityFrameworkCore;
using ServiceStack;

namespace Gameboard.Api.Structure.MediatR.Validators;

internal class TeamExistsValidator<TModel> : IGameboardValidator<TModel>
{
    private readonly IStore _store;
    private Func<TModel, string> _teamIdProperty;
    private Func<TModel, IEnumerable<string>> _teamIdsProperty;

    public TeamExistsValidator(IStore store)
    {
        _store = store;
    }

    public TeamExistsValidator<TModel> UseProperty(Func<TModel, string> propertyExpression)
    {
        _teamIdProperty = propertyExpression;
        return this;
    }

    public TeamExistsValidator<TModel> UseProperty(Func<TModel, IEnumerable<string>> propertyExpression)
    {
        _teamIdsProperty = propertyExpression;
        return this;
    }

    public Func<TModel, RequestValidationContext, Task> GetValidationTask()
    {
        return async (model, context) =>
        {
            var allTeamIDs = new List<string>();

            if (_teamIdProperty is not null)
                allTeamIDs.Add(_teamIdProperty(model));

            if (_teamIdsProperty is not null)
                allTeamIDs.AddDistinctRange(_teamIdsProperty(model));

            var finalTeamIds = allTeamIDs
                .Where(tId => !tId.IsNullOrEmpty())
                .Distinct()
                .ToArray();

            if (!finalTeamIds.Any())
                context.AddValidationException(new MissingRequiredInput<string>(nameof(_teamIdProperty), _teamIdProperty?.ToString()));

            // grab the gameId as a representation of each player, because we also need to know if they're somehow
            // in different games
            var players = await _store
                .WithNoTracking<Data.Player>()
                .Select(p => new { p.TeamId, p.GameId })
                .Where(p => finalTeamIds.Contains(p.TeamId))
                .GroupBy(p => p.TeamId)
                .ToDictionaryAsync(gr => gr.Key, gr => gr.ToArray());

            foreach (var teamId in finalTeamIds)
            {
                if (!players.ContainsKey(teamId))
                    context.AddValidationException(new ResourceNotFound<Team>(teamId));
            }

            var gameIds = players.Values.SelectMany(t => t.Select(p => p.GameId)).Distinct();
            if (gameIds.Distinct().Count() > 1)
                context.AddValidationException(new PlayersAreInMultipleGames(gameIds));
        };
    }
}
