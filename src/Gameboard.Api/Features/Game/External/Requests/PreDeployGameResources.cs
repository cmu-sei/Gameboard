using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games.Start;
using Gameboard.Api.Structure;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gameboard.Api.Features.Games.External;

public record PreDeployExternalGameResourcesCommand(string GameId, User Actor) : IRequest;

internal class PreDeployExternalGameResourcesHandler : IRequestHandler<PreDeployExternalGameResourcesCommand>
{
    private readonly IExternalGameHostAccessTokenProvider _accessTokenProvider;
    private readonly IAppUrlService _appUrlService;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;
    private readonly BackgroundTaskContext _backgroundTaskContext;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IStore _store;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<PreDeployExternalGameResourcesCommand> _validator;

    public PreDeployExternalGameResourcesHandler
    (
        IExternalGameHostAccessTokenProvider accessTokenProvider,
        IAppUrlService appUrlService,
        IBackgroundTaskQueue backgroundTaskQueue,
        BackgroundTaskContext backgroundTaskContext,
        IServiceScopeFactory serviceScopeFactory,
        IStore store,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<PreDeployExternalGameResourcesCommand> validator
    )
    {
        _accessTokenProvider = accessTokenProvider;
        _appUrlService = appUrlService;
        _backgroundTaskQueue = backgroundTaskQueue;
        _backgroundTaskContext = backgroundTaskContext;
        _serviceScopeFactory = serviceScopeFactory;
        _store = store;
        _userRoleAuthorizer = userRoleAuthorizer;
        _validator = validator;
    }

    public async Task Handle(PreDeployExternalGameResourcesCommand request, CancellationToken cancellationToken)
    {
        // auth and validate
        _userRoleAuthorizer.AllowRoles(UserRole.Admin).Authorize();

        _validator.AddValidator(async (req, ctx) =>
        {
            var game = await _store
                .WithNoTracking<Data.Game>()
                .SingleOrDefaultAsync(g => g.Id == req.GameId);

            if (game is null)
            {
                ctx.AddValidationException(new ResourceNotFound<Data.Game>(req.GameId));
                return;
            }

            if (game.Mode != GameEngineMode.External || !game.RequireSynchronizedStart)
                ctx.AddValidationException(new CantPreDeployNonExternalGame(req.GameId));
        });
        await _validator.Validate(request, cancellationToken);

        // do the predeploy stuff
        // (note that we fire and forget this because updates are provided over signalR in the GameHub).
        _backgroundTaskContext.AccessToken = await _accessTokenProvider.GetToken();
        _backgroundTaskContext.ActingUser = request.Actor;
        _backgroundTaskContext.AppBaseUrl = _appUrlService.GetBaseUrl();

        await _backgroundTaskQueue.QueueBackgroundWorkItemAsync
        (
            async cancellationToken =>
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var gameStartService = scope.ServiceProvider.GetRequiredService<IGameStartService>();

                await gameStartService.PreDeployGameResources(new GameStartRequest { GameId = request.GameId }, cancellationToken);
            }
        );
    }
}
