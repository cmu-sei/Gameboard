using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Validators;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.UnityGames;

public class UnityGamesValidator : IModelValidator
{
    private readonly IUnityStore _store;

    public UnityGamesValidator(IUnityStore store)
    {
        _store = store;
    }

    public async Task Validate(object model)
    {
        if (model is NewUnityChallenge)
        {
            var typedModel = model as NewUnityChallenge;

            if (!(await GameExists(typedModel.GameId)))
            {
                throw new ResourceNotFound<Game>(typedModel.GameId);
            }

            if (!(await TeamExists(typedModel.TeamId)))
            {
                throw new ResourceNotFound<Team>(typedModel.TeamId);
            }

            var teamPlayers = await _store.DbContext
                .Players
                .Where(p => p.TeamId == typedModel.TeamId)
                .ToListAsync();

            if (teamPlayers.Count == 0)
            {
                throw new TeamHasNoPlayersException();
            }

            if (teamPlayers.Any(p => p.GameId != typedModel.GameId))
            {
                throw new PlayerWrongGameIDException();
            }
        }
        else if (model is NewUnityChallengeEvent)
        {
            var typedModel = model as NewUnityChallengeEvent;

            if (!(await ChallengeExists(typedModel.ChallengeId)))
            {
                throw new ResourceNotFound<Challenge>(typedModel.ChallengeId);
            }

            if (!(await TeamExists(typedModel.TeamId)))
            {
                throw new ResourceNotFound<Team>(typedModel.TeamId);
            }
        }
        else
        {
            throw new ValidationTypeFailure<UnityGamesValidator>(model.GetType());
        }
    }

    private async Task<bool> ChallengeExists(string id)
        => id.HasValue() && (await _store.DbContext.Challenges.FindAsync(id)) is Data.Challenge;

    private async Task<bool> GameExists(string id)
        => !string.IsNullOrEmpty(id) && (await _store.DbContext.Games.FindAsync(id)) is Data.Game;

    private async Task<bool> TeamExists(string teamId)
        => !string.IsNullOrWhiteSpace(teamId) && (
            await _store
                .DbContext.Players
                .Where(p => p.TeamId == teamId)
                .FirstOrDefaultAsync()
            ) != null;

    private async Task<bool> UserExists(string id)
        => id.NotEmpty() && (await _store.DbContext.Users.FindAsync(id)) is Data.User;
}