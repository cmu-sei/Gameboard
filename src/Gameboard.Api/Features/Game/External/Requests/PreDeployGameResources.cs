using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Features.Games.Start;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Gameboard.Api.Features.Games.External;

public record PreDeployExternalGameResourcesCommand(string GameId, IEnumerable<string> TeamIds = null) : IRequest;

internal class PreDeployExternalGameResourcesHandler : IRequestHandler<PreDeployExternalGameResourcesCommand>
{
    private readonly IExternalGameHostAccessTokenProvider _accessTokenProvider;
    private readonly IActingUserService _actingUserService;
    private readonly IAppUrlService _appUrlService;
    private readonly IBackgroundAsyncTaskQueueService _backgroundTaskQueue;
    private readonly BackgroundAsyncTaskContext _backgroundTaskContext;
    private readonly GameWithModeExistsValidator<PreDeployExternalGameResourcesCommand> _gameExists;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<PreDeployExternalGameResourcesCommand> _validator;

    public PreDeployExternalGameResourcesHandler
    (
        IExternalGameHostAccessTokenProvider accessTokenProvider,
        IActingUserService actingUserService,
        IAppUrlService appUrlService,
        IBackgroundAsyncTaskQueueService backgroundTaskQueue,
        BackgroundAsyncTaskContext backgroundTaskContext,
        GameWithModeExistsValidator<PreDeployExternalGameResourcesCommand> gameExists,
        IServiceScopeFactory serviceScopeFactory,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<PreDeployExternalGameResourcesCommand> validator
    )
    {
        _accessTokenProvider = accessTokenProvider;
        _actingUserService = actingUserService;
        _appUrlService = appUrlService;
        _backgroundTaskQueue = backgroundTaskQueue;
        _backgroundTaskContext = backgroundTaskContext;
        _gameExists = gameExists;
        _serviceScopeFactory = serviceScopeFactory;
        _userRoleAuthorizer = userRoleAuthorizer;
        _validator = validator;
    }

    public async Task Handle(PreDeployExternalGameResourcesCommand request, CancellationToken cancellationToken)
    {
        // auth and validate
        _userRoleAuthorizer.AllowRoles(UserRole.Admin).Authorize();

        _validator.AddValidator
        (
            _gameExists
                .UseIdProperty(r => r.GameId)
                .WithEngineMode(GameEngineMode.External)
                .WithSyncStartRequired(true)
        );
        await _validator.Validate(request, cancellationToken);

        // do the predeploy stuff
        // (note that we fire and forget this because updates are provided over signalR in the GameHub).
        _backgroundTaskContext.AccessToken = await _accessTokenProvider.GetToken();
        _backgroundTaskContext.ActingUser = _actingUserService.Get();
        _backgroundTaskContext.AppBaseUrl = _appUrlService.GetBaseUrl();

        await _backgroundTaskQueue.QueueBackgroundWorkItemAsync
        (
            async cancellationToken =>
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var gameStartService = scope.ServiceProvider.GetRequiredService<IGameStartService>();

                await gameStartService.PreDeployGameResources(new PreDeployResourcesRequest { GameId = request.GameId, TeamIds = request.TeamIds }, cancellationToken);
            }
        );
    }
}
