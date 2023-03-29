using System;
using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Structure.MediatR.Validators;

internal class TeamExistsValidator<TModel> : IGameboardValidator<string>, IValidationPropertyProvider<TModel, string>
{
    private readonly IPlayerStore _playerStore;

    public TeamExistsValidator(IPlayerStore playerStore)
    {
        _playerStore = playerStore;
    }

    public Func<TModel, string> ValidationProperty => throw new NotImplementedException();

    public async Task<GameboardValidationException> Validate(string teamId)
    {
        if (string.IsNullOrEmpty(teamId))
            return new MissingRequiredInput<string>(nameof(teamId), teamId);

        var count = await _playerStore.List().CountAsync(p => p.TeamId == teamId);
        if (count == 0)
        {
            return new ResourceNotFound<Team>(teamId);
        }

        return null;
    }
}
