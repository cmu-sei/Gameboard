using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.Player;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;

namespace Gameboard.Api.Features.Games;

public record StartStandardNonSyncGameCommand(string GameId, User ActingUser) : IRequest<StartStandardNonSyncGameResult>;

internal class StartStandardNonSyncGameHandler : IRequestHandler<StartStandardNonSyncGameCommand, StartStandardNonSyncGameResult>
{
    private readonly EntityExistsValidator<StartStandardNonSyncGameCommand, Data.Game> _gameExists;
    private readonly IGameService _gameService;
    private readonly IGameStore _gameStore;
    private readonly PlayerService _playerService;
    private readonly IValidatorService<StartStandardNonSyncGameCommand> _validator;

    public StartStandardNonSyncGameHandler
    (
        EntityExistsValidator<StartStandardNonSyncGameCommand, Data.Game> gameExists,
        IGameService gameService,
        IGameStore gameStore,
        PlayerService playerService,
        IValidatorService<StartStandardNonSyncGameCommand> validator
    )
    {
        _gameExists = gameExists;
        _gameService = gameService;
        _gameStore = gameStore;
        _playerService = playerService;
        _validator = validator;
    }

    public async Task<StartStandardNonSyncGameResult> Handle(StartStandardNonSyncGameCommand request, CancellationToken cancellationToken)
    {
        // validate
        _validator
            .AddValidator(_gameExists.UseProperty(r => r.GameId))
            .AddValidator
            (
                ((req, ctx) =>
                {
                    if (req.ActingUser == null)
                        ctx.AddValidationException(new CantStartStandardGameWithoutActingUserParameter(req.GameId));

                    return Task.CompletedTask;
                })
            )
            .AddValidator
            (
                async (req, ctx) =>
                {
                    var player = await _playerService.RetrieveByUserId(req.ActingUser.Id);

                    if (player == null)
                        ctx.AddValidationException(new UserIsntPlayingGame(req.ActingUser.Id, req.GameId, $"""Can't start standard nonsync game "{req.GameId}" because user "{req.ActingUser.Id}" hasn't registered to play it."""));

                    if (player.SessionBegin.IsNotEmpty())
                        ctx.AddValidationException(new SessionAlreadyStarted(player.Id, $"""Can't start player "{player.Id}"'s session for standard nonsync game "{req.GameId}" because it's already started ({player.SessionBegin})"""));
                }
            );

        await _validator.Validate(request, cancellationToken);

        // start
        // for now, just delegate this to PlayerService, which is what was doing this before
        var player = await _playerService.RetrieveByUserId(request.ActingUser.Id);
        var startedPlayer = await _playerService.StartSession
        (
            new SessionStartRequest { PlayerId = player.Id },
            request.ActingUser,
            _gameService.IsGameStartSuperUser(request.ActingUser)
        );

        return new StartStandardNonSyncGameResult
        {
            Player = startedPlayer
        };
    }
}

