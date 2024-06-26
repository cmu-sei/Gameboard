using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gameboard.Api.Features.Games;

public record DeployGameResourcesCommand(string GameId, IEnumerable<string> TeamIds) : IRequest;

internal class DeployGameResourcesHandler : IRequestHandler<DeployGameResourcesCommand>
{
    private readonly IActingUserService _actingUserService;
    private readonly IAppUrlService _appUrlService;
    private readonly IBackgroundAsyncTaskQueueService _backgroundTaskQueue;
    private readonly BackgroundAsyncTaskContext _backgroundTaskContext;
    private readonly GameWithModeExistsValidator<DeployGameResourcesCommand> _gameExists;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IStore _store;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<DeployGameResourcesCommand> _validator;

    public DeployGameResourcesHandler
    (
        IActingUserService actingUserService,
        IAppUrlService appUrlService,
        IBackgroundAsyncTaskQueueService backgroundTaskQueue,
        BackgroundAsyncTaskContext backgroundTaskContext,
        GameWithModeExistsValidator<DeployGameResourcesCommand> gameExists,
        IServiceScopeFactory serviceScopeFactory,
        IStore store,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<DeployGameResourcesCommand> validator
    )
    {
        _actingUserService = actingUserService;
        _appUrlService = appUrlService;
        _backgroundTaskQueue = backgroundTaskQueue;
        _backgroundTaskContext = backgroundTaskContext;
        _gameExists = gameExists;
        _serviceScopeFactory = serviceScopeFactory;
        _store = store;
        _userRoleAuthorizer = userRoleAuthorizer;
        _validator = validator;
    }

    public async Task Handle(DeployGameResourcesCommand request, CancellationToken cancellationToken)
    {
        // auth and validate
        _userRoleAuthorizer.AllowRoles(UserRole.Admin).Authorize();
        _validator.AddValidator(_gameExists.UseIdProperty(r => r.GameId));
        await _validator.Validate(request, cancellationToken);

        // do the predeploy stuff
        // (note that we fire and forget this because updates are provided over signalR in the GameHub).
        _backgroundTaskContext.ActingUser = _actingUserService.Get();
        _backgroundTaskContext.AppBaseUrl = _appUrlService.GetBaseUrl();

        var finalTeamIds = request is null || request.TeamIds.IsEmpty() ? Array.Empty<string>() : request.TeamIds;
        if (finalTeamIds.IsEmpty() && request.GameId.IsNotEmpty())
            finalTeamIds = await _store
                .WithNoTracking<Data.Player>()
                .Where(p => p.GameId == request.GameId)
                .Select(p => p.TeamId)
                .Distinct()
                .ToArrayAsync(cancellationToken);

        await _backgroundTaskQueue.QueueBackgroundWorkItemAsync
        (
            async cancellationToken =>
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var resourcesDeploymentService = scope.ServiceProvider.GetRequiredService<IGameResourcesDeployService>();

                await resourcesDeploymentService.DeployResources(finalTeamIds, cancellationToken);
            }
        );
    }
}
