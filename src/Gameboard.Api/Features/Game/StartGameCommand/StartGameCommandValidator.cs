using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;

namespace Gameboard.Api.Features.Games;

internal class StartGameCommandValidator : IGameboardRequestValidator<StartGameCommand>
{
    private readonly EntityExistsValidator<StartGameCommand, Data.Game> _gameExists;
    private readonly IGameService _gameService;
    private readonly ITeamService _teamService;
    private readonly IValidatorService<StartGameCommand> _validatorService;

    public StartGameCommandValidator
    (
        EntityExistsValidator<StartGameCommand, Data.Game> gameExists,
        IGameService gameService,
        ITeamService teamService,
        IValidatorService<StartGameCommand> validatorService
    )
    {
        _gameExists = gameExists;
        _gameService = gameService;
        _teamService = teamService;
        _validatorService = validatorService;
    }

    public async Task Validate(StartGameCommand request, CancellationToken cancellationToken)
    {
        _validatorService.AddValidator(_gameExists.UseProperty(g => g.GameId));

        // admins and testers are allowed to do whatever they want
        if (request.ActingUser.IsAdmin || request.ActingUser.IsTester)
            return;

        var game = await _gameService.Retrieve(request.GameId);

        // Rule: teams can only have a number of sessions less than or equal to the game session limit (unless an admin
        // or tester is acting)
        _validatorService.AddValidator
        (
            async (request, context) =>
            {
                if (game.PlayerMode != PlayerMode.Competition)
                {
                    var sessionCount = await _teamService.GetSessionCount(request.TeamId, request.GameId, cancellationToken);
                    if (game.SessionLimit > 0 && sessionCount > game.SessionLimit)
                        context.AddValidationException(new SessionLimitReached(request.TeamId, request.GameId, sessionCount, game.SessionLimit));
                }
            }
        );

        // Rule: games can't start if the current date/time isn't in the "execution window"
        // _validatorService.AddValidator(new SimpleValidator<StartGameCommand, )
        await _validatorService.Validate(request, cancellationToken);
    }
}
