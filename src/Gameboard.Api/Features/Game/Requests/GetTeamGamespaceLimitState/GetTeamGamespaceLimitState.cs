using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games.Validators;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;

namespace Gameboard.Api.Features.Games;

public class TeamGamespaceLimitState
{
    public required string GameId { get; set; }
    public required string TeamId { get; set; }
    public required IEnumerable<SimpleEntity> ChallengesWithActiveGamespace { get; set; }
    public required int GamespaceLimit { get; set; }
}

public record GetTeamGamespaceLimitStateQuery(string GameId, string TeamId, User ActingUser) : IRequest<TeamGamespaceLimitState>;

internal class GetTeamGamespaceLimitStateQueryHandler : IRequestHandler<GetTeamGamespaceLimitStateQuery, TeamGamespaceLimitState>
{
    private readonly EntityExistsValidator<GetTeamGamespaceLimitStateQuery, Data.Game> _gameExists;
    private readonly IStore _store;
    private readonly TeamExistsValidator<GetTeamGamespaceLimitStateQuery> _teamExists;
    private readonly ITeamService _teamService;
    private readonly UserIsPlayingGameValidator<GetTeamGamespaceLimitStateQuery> _userIsPlayingGame;
    private readonly IValidatorService<GetTeamGamespaceLimitStateQuery> _validatorService;

    public GetTeamGamespaceLimitStateQueryHandler
    (
        EntityExistsValidator<GetTeamGamespaceLimitStateQuery, Data.Game> gameExists,
        IStore store,
        TeamExistsValidator<GetTeamGamespaceLimitStateQuery> teamExists,
        ITeamService teamService,
        UserIsPlayingGameValidator<GetTeamGamespaceLimitStateQuery> userIsPlayingGame,
        IValidatorService<GetTeamGamespaceLimitStateQuery> validatorService
    )
    {
        _gameExists = gameExists;
        _store = store;
        _teamExists = teamExists;
        _teamService = teamService;
        _userIsPlayingGame = userIsPlayingGame;
        _validatorService = validatorService;
    }

    public async Task<TeamGamespaceLimitState> Handle(GetTeamGamespaceLimitStateQuery request, CancellationToken cancellationToken)
    {
        await _validatorService
            .AddValidator(_gameExists.UseProperty(r => r.GameId))
            .AddValidator(_teamExists.UseProperty(r => r.TeamId))
            .AddValidator
            (
                _userIsPlayingGame
                    .UseGameIdProperty(r => r.GameId)
                    .UseUserProperty(r => r.ActingUser)
            )
            .Validate(request, cancellationToken);

        var activeChallenges = await _teamService.GetChallengesWithActiveGamespace(request.TeamId, request.GameId, cancellationToken);
        var game = await _store.FirstOrDefaultAsync<Data.Game>(g => g.Id == request.GameId, cancellationToken);

        return new TeamGamespaceLimitState
        {
            GameId = request.GameId,
            TeamId = request.TeamId,
            ChallengesWithActiveGamespace = activeChallenges,
            GamespaceLimit = game.IsPracticeMode ? 1 : game.GamespaceLimitPerSession
        };
    }
}
