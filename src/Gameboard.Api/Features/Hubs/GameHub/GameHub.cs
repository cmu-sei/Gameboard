using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games.Validators;
using Gameboard.Api.Hubs;
using Gameboard.Api.Services;
using Gameboard.Api.Structure;
using Gameboard.Api.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Games;

public interface IGameHubApi
{
    Task JoinGame(GameJoinRequest request);
    Task LeaveGame(GameLeaveRequest request);
}

[Authorize(AppConstants.HubPolicy)]
public class GameHub : Hub<IGameHubEvent>, IGameHubApi, IGameboardHub
{
    private readonly IGameHubBus _hubBus;
    private readonly ILogger<GameHub> _logger;
    private readonly PlayerService _playerService;
    private readonly IStore _store;
    private readonly UserIsPlayingGameValidator _userIsPlayingGame;
    private readonly IValidatorServiceFactory _validatorServiceFactory;

    public GameHub
    (
        IGameHubBus hubBus,
        ILogger<GameHub> logger,
        PlayerService playerService,
        IStore store,
        UserIsPlayingGameValidator userIsPlayingGame,
        IValidatorServiceFactory validatorServiceFactory
    )
    {
        _hubBus = hubBus;
        _logger = logger;
        _playerService = playerService;
        _store = store;
        _userIsPlayingGame = userIsPlayingGame;
        _validatorServiceFactory = validatorServiceFactory;
    }

    public GameboardHubType GroupType => GameboardHubType.Game;

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        this.LogOnConnected(_logger, Context);

        var activeGameIds = await GetActiveGameIds(Context.UserIdentifier);
        foreach (var gameId in activeGameIds)
            await this.JoinGroup(gameId);
    }

    public override Task OnDisconnectedAsync(Exception ex)
    {
        this.LogOnDisconnected(_logger, Context, ex);
        return base.OnDisconnectedAsync(ex);
    }

    public async Task JoinGame(GameJoinRequest request)
    {
        _logger.LogInformation(LogEventId.GameHub_Group_JoinStart, message: $"""User "{Context.UserIdentifier}" is joining the group for game "{request.GameId}"...""");

        // validate
        var validator = _validatorServiceFactory.Get();
        validator.AddValidator
        (
            _userIsPlayingGame
                .UseGameId(request.GameId)
                .UseUserId(Context.UserIdentifier)
        );
        await validator.Validate();

        // join
        await this.JoinGroup(request.GameId);

        // remember the player record for this game 
        var player = await _playerService.RetrieveByUserId(Context.UserIdentifier);
        Context.Items[BuildPlayerContextKey(request.GameId)] = player;

        // notify this player and other game members
        await _hubBus.SendYouJoined(Context.UserIdentifier, new YouJoinedEvent { GameId = request.GameId });
        await _hubBus.SendPlayerJoined(Context.ConnectionId, new PlayerJoinedEvent
        {
            GameId = request.GameId,
            Player = new SimpleEntity { Id = player.Id, Name = player.ApprovedName }
        });

        _logger.LogInformation(LogEventId.GameHub_Group_JoinEnd, message: $"""User "{Context.UserIdentifier}" joined the group for game "{request.GameId}".""");
    }

    public async Task LeaveGame(GameLeaveRequest request)
    {
        // validate
        var validator = _validatorServiceFactory.Get();
        validator.AddValidator
        (
            _userIsPlayingGame
                .UseGameId(request.GameId)
                .UseUserId(Context.UserIdentifier)
        );

        // leave
        await this.LeaveGroup(request.GameId);

        // forget the player record if it exists
        Context.Items.Remove(BuildPlayerContextKey(request.GameId));
    }

    private async Task<IEnumerable<string>> GetActiveGameIds(string userId)
    {
        return await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.Game.GameEnd != DateTimeOffset.MinValue || p.Game.GameEnd > DateTimeOffset.UtcNow)
            .Where(p => p.UserId == userId)
            .Where(p => p.Mode == p.Game.PlayerMode)
            .Select(p => p.GameId)
            .Distinct()
            .ToArrayAsync();
    }

    private string BuildPlayerContextKey(string gameId)
        => $"{gameId}-player";
}
