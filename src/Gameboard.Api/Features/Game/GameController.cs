// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Features.games;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Games.ImportExport;
using Gameboard.Api.Features.Games.Requests;
using Gameboard.Api.Features.Games.Start;
using Gameboard.Api.Features.Scores;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Services;
using Gameboard.Api.Validators;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Controllers;

[Authorize]
public class GameController
(
    IActingUserService actingUserService,
    ILogger<GameController> logger,
    IDistributedCache cache,
    GameService gameService,
    IScoreDenormalizationService scoreDenormalization,
    GameValidator validator,
    CoreOptions options,
    IMediator mediator,
    IHostEnvironment env,
    IUserRolePermissionsService permissionsService
) : GameboardLegacyController(actingUserService, logger, cache, validator)
{
    GameService GameService { get; } = gameService;
    public CoreOptions Options { get; } = options;
    public IHostEnvironment Env { get; } = env;

    /// <summary>
    /// Create new game
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("api/game")]
    public async Task<Game> Create([FromBody] GameDetail model)
    {
        await Authorize(permissionsService.Can(PermissionKey.Games_CreateEditDelete));
        return await GameService.Create(model);
    }

    [HttpPost("api/game/clone")]
    [Authorize]
    public Task<Game> Clone([FromBody] CloneGameCommand request, CancellationToken cancellationToken)
        => mediator.Send(request, cancellationToken);

    /// <summary>
    /// Retrieve game
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("api/game/{id}")]
    [AllowAnonymous]
    public async Task<Game> Retrieve([FromRoute] string id)
    {
        return await GameService.Retrieve(id, await permissionsService.Can(PermissionKey.Games_ViewUnpublished));
    }

    [HttpGet("api/game/{id}/specs")]
    [Authorize]
    public async Task<ChallengeSpec[]> GetChallengeSpecs([FromRoute] string id)
    {
        await Validate(new Entity { Id = id });
        return await GameService.RetrieveChallengeSpecs(id);
    }

    [HttpGet("api/game/{gameId}/session-availability")]
    [Authorize]
    public Task<GameSessionAvailibilityResponse> GetSessionAvailability([FromRoute] string gameId)
        => mediator.Send(new GameSessionAvailabilityQuery(gameId));

    [HttpGet("api/game/{id}/sessions")]
    [Authorize]
    public async Task<SessionForecast[]> GetSessionForecast([FromRoute] string id)
    {
        await Validate(new Entity { Id = id });
        return await GameService.SessionForecast(id);
    }

    [HttpPost("api/game/{gameId}/resources")]
    [Authorize]
    public Task DeployResources([FromRoute] string gameId, [FromBody] DeployGameResourcesBody body)
        => mediator.Send(new DeployGameResourcesCommand(gameId, body?.TeamIds));

    /// <summary>
    /// Change game
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPut("api/game")]
    public async Task<Data.Game> Update([FromBody] ChangedGame model)
    {
        await Authorize(permissionsService.Can(PermissionKey.Games_CreateEditDelete));
        await Validate(model);
        return await GameService.Update(model);
    }

    /// <summary>
    /// Delete game
    /// </summary>
    /// <param name="id"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpDelete("/api/game/{id}")]
    public Task Delete([FromRoute] string id, [FromBody] DeleteGameRequest request)
        => mediator.Send(new DeleteGameCommand(id, request.AllowPlayerDeletion));

    /// <summary>
    /// Find games
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpGet("/api/games")]
    [AllowAnonymous]
    public async Task<IEnumerable<Game>> List([FromQuery] GameSearchFilter model)
        => await GameService.List(model, await permissionsService.Can(PermissionKey.Games_ViewUnpublished));

    /// <summary>
    /// List games for admin interfaces.
    /// 
    /// NOTE: This endpoint will eventually replace /api/games and take its path - it's just a modernized
    /// expression of the same utility.
    /// </summary>
    /// <param name="query"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("/api/games/admin")]
    public Task<ListGamesResponse> ListAdmin([FromQuery] ListGamesQuery query, CancellationToken cancellationToken)
        => mediator.Send(query, cancellationToken);

    /// <summary>
    /// List games grouped by year and month
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpGet("/api/games/grouped")]
    [AllowAnonymous]
    public async Task<GameGroup[]> ListGrouped([FromQuery] GameSearchFilter model)
        => await GameService.ListGrouped(model, await permissionsService.Can(PermissionKey.Games_ViewUnpublished));

    [HttpGet("/api/game/{gameId}/ready")]
    [Authorize]
    public Task<SyncStartState> GetSyncStartState(string gameId)
        => mediator.Send(new GetSyncStartStateQuery(gameId, Actor));

    [HttpGet("/api/game/{gameId}/play-state")]
    [Authorize]
    public Task<GamePlayState> GetGamePlayState(string gameId)
        => mediator.Send(new GetGamePlayStateQuery(gameId, Actor.Id));

    [HttpPost("/api/games/export")]
    public Task<GameImportExportBatch> ExportGames([FromBody] ExportGamesCommand request, CancellationToken cancellationToken)
        => mediator.Send(request, cancellationToken);

    [HttpGet("/api/games/export-batches")]
    public Task<ListExportBatchesResponse> ListGameExportBatches(CancellationToken cancellationToken)
        => mediator.Send(new ListExportBatchesQuery(), cancellationToken);

    [HttpDelete("/api/games/export-batches/{exportBatchId}")]
    public Task DeleteExportPackage(string exportBatchId, CancellationToken cancellationToken)
        => mediator.Send(new DeleteExportBatchCommand(exportBatchId), cancellationToken);

    [HttpGet("/api/games/export-batches/{exportBatchId}")]
    public async Task<FileContentResult> DownloadExportPackage(string exportBatchId, CancellationToken cancellationToken)
    {
        var bytes = await mediator.Send(new DownloadExportPackageRequest(exportBatchId), cancellationToken);
        return new FileContentResult(bytes, "application/zip");
    }

    [HttpPost("/api/games/import")]
    public async Task<ImportedGame[]> ImportGames([FromForm] ImportGamesRequest request, CancellationToken cancellationToken)
    {
        var package = await request.PackageFile.ToBytes(cancellationToken);

        // the gameIds are passed as a comma-delimited string (because the request accepts formdata)
        var parsedGameIds = Array.Empty<string>();
        if (request.DelimitedGameIds.IsNotEmpty())
        {
            parsedGameIds = request.DelimitedGameIds.Split(',');
        }

        // TODO: should do a nullable boolean JsonConverter at some point, but
        var setPublishStatus = default(bool?);
        if (request.SetGamesPublishStatus is not null)
        {
            setPublishStatus = request.SetGamesPublishStatus.Equals("true", StringComparison.CurrentCultureIgnoreCase);
        }

        return await mediator.Send(new ImportGamesCommand(package, parsedGameIds, setPublishStatus), cancellationToken);
    }

    [HttpPost("/api/games/import/preview")]
    public async Task<GameImportExportBatch> PreviewImportPackage([FromForm] IFormFile packageFile, CancellationToken cancellationToken)
    {
        var package = await packageFile.ToBytes(cancellationToken);
        return await mediator.Send(new PreviewImportPackageQuery(package), cancellationToken);
    }

    [HttpGet("/api/game/{gameId}/team/{teamId}/gamespace-limit")]
    public Task<TeamGamespaceLimitState> GetTeamGamespaceLimitState([FromRoute] string gameId, [FromRoute] string teamId)
        => mediator.Send(new GetTeamGamespaceLimitStateQuery(gameId, teamId, Actor));

    [HttpPost("api/game/{id}/image/card")]
    public async Task<ActionResult<UploadedFile>> UploadCardImage(string id, IFormFile file, CancellationToken cancellationToken)
    {
        await Authorize(permissionsService.Can(PermissionKey.Games_CreateEditDelete));
        return Ok(await GameService.SaveCardImage(id, file, cancellationToken));
    }

    [HttpPost("api/game/{id}/image/map")]
    public async Task<UploadedFile> UploadMapImage(string id, IFormFile file, CancellationToken cancellationToken)
    {
        await Authorize(permissionsService.Can(PermissionKey.Games_CreateEditDelete));
        return await GameService.SaveMapImage(id, file, cancellationToken);
    }

    [HttpDelete("api/game/{id}/image/card")]
    [Authorize]
    public async Task DeleteGameCard([FromRoute] string id, CancellationToken cancellationToken)
    {
        await Authorize(permissionsService.Can(PermissionKey.Games_CreateEditDelete));
        await GameService.DeleteCardImage(id, cancellationToken);
    }

    [HttpDelete("api/game/{gameId}/image/map")]
    [Authorize]
    public async Task DeleteGameMapImage([FromRoute] string gameId, CancellationToken cancellationToken)
    {
        await Authorize(permissionsService.Can(PermissionKey.Games_CreateEditDelete));
        await GameService.DeleteMapImage(gameId, cancellationToken);
    }

    /// <summary>
    /// Rerank a game's players
    /// </summary>
    /// <param name="id">id</param>
    /// <param name="cancellationToken">id</param>
    /// <returns></returns>
    [HttpPost("/api/game/{id}/rerank")]
    public async Task Rerank([FromRoute] string id, CancellationToken cancellationToken)
    {
        await Authorize(permissionsService.Can(PermissionKey.Scores_RegradeAndRerank));
        await Validate(new Entity { Id = id });

        await scoreDenormalization.DenormalizeGame(id, cancellationToken);
        await mediator.Publish(new GameCacheInvalidateNotification(id), cancellationToken);
    }
}
