using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Structure.MediatR.Validators;

internal class TeamExistsValidator : IGameboardValidator
{
    private readonly IPlayerStore _playerStore;

    public TeamExistsValidator(IPlayerStore playerStore)
    {
        _playerStore = playerStore;
    }

    public async Task<GameboardValidationException> Validate<T>(T teamId)
    {
        if (typeof(T) != typeof(string))
        {
            throw new MissingRequiredInput<string>(nameof(teamId), teamId.ToString());
        }
        var teamIdString = teamId.ToString();
        var count = await _playerStore.List().CountAsync(p => p.TeamId == teamIdString);

        if (count == 0)
        {
            return new ResourceNotFound<Team>(teamIdString);
        }

        return null;
    }
}
