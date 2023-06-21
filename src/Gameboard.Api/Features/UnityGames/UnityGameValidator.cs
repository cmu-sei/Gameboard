using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.Teams;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.UnityGames;

public class UnityGamesValidator : IModelValidator
{
    private readonly IChallengeStore _challengeStore;
    private readonly IGameStore _gameStore;
    private readonly IUnityStore _store;
    private readonly ITeamService _teamService;

    public UnityGamesValidator
    (
        IChallengeStore challengeStore,
        IGameStore gameStore,
        IUnityStore store,
        ITeamService teamService
    )
    {
        _challengeStore = challengeStore;
        _gameStore = gameStore;
        _store = store;
        _teamService = teamService;
    }

    public async Task Validate(object model)
    {
        if (model is NewUnityChallenge)
        {
            var typedModel = model as NewUnityChallenge;

            if (!(await _gameStore.Exists(typedModel.GameId)))
            {
                throw new ResourceNotFound<Game>(typedModel.GameId);
            }

            if (!(await _teamService.GetExists(typedModel.TeamId)))
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

            if (!(await _challengeStore.Exists(typedModel.ChallengeId)))
            {
                throw new ResourceNotFound<Challenge>(typedModel.ChallengeId);
            }

            if (!(await _teamService.GetExists(typedModel.TeamId)))
            {
                throw new ResourceNotFound<Team>(typedModel.TeamId);
            }
        }
        else if (model is UnityMissionUpdate)
        {
            var typedModel = model as UnityMissionUpdate;

            if (!(await _teamService.GetExists(typedModel.TeamId)))
            {
                throw new ResourceNotFound<Team>(typedModel.TeamId);
            }
        }
        else
        {
            throw new ValidationTypeFailure<UnityGamesValidator>(model.GetType());
        }
    }
}
