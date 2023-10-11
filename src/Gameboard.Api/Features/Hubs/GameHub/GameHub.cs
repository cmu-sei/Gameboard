using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gameboard.Api.Features.Games.Validators;
using Gameboard.Api.Hubs;
using Gameboard.Api.Services;
using Gameboard.Api.Structure;
using Gameboard.Api.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
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
    private readonly IMemoryCache _cache;
    private readonly IGameHubBus _hubBus;
    private readonly ILogger<GameHub> _logger;
    private readonly PlayerService _playerService;
    private readonly UserIsPlayingGameValidator _userIsPlayingGame;
    private readonly IValidatorServiceFactory _validatorServiceFactory;

    public GameHub
    (
        IMemoryCache cache,
        IGameHubBus hubBus,
        ILogger<GameHub> logger,
        PlayerService playerService,
        UserIsPlayingGameValidator userIsPlayingGame,
        IValidatorServiceFactory validatorServiceFactory
    )
    {
        _cache = cache;
        _hubBus = hubBus;
        _logger = logger;
        _playerService = playerService;
        _userIsPlayingGame = userIsPlayingGame;
        _validatorServiceFactory = validatorServiceFactory;
    }

    public GameboardHubGroupType GroupType { get => GameboardHubGroupType.Game; }

    public override async Task OnConnectedAsync()
    {
        this.LogOnConnected(_logger, Context);
        // add user to a group of themselves for easy addressing
        await Groups.AddToGroupAsync(Context.ConnectionId, Context.User.Name());
        await base.OnConnectedAsync();
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

        // cache a record of this user being in the game
        AddUserToGameCache(request.GameId, player);

        await _hubBus.SendYouJoined(Context.UserIdentifier, new YouJoinedEvent { GameId = request.GameId });

        // notify other game members
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
        RemoveUserFromGameCache(request.GameId, Context.UserIdentifier);
    }

    private IList<string> AddUserToGameCache(string gameId, Data.Player player)
    {
        IList<string> userIds;
        if (!_cache.TryGetValue(gameId, out userIds))
        {
            var opts = new MemoryCacheEntryOptions();
            opts.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(player.SessionMinutes);

            _cache.Set(gameId, new List<string> { player.UserId }, opts);
        }

        return userIds;
    }

    private void RemoveUserFromGameCache(string gameId, string userId)
    {
        IList<string> userIdList;
        if (_cache.TryGetValue<IList<string>>(gameId, out userIdList))
        {
            userIdList.Remove(userId);
            _cache.Set(gameId, userIdList);
        }
    }

    private string BuildPlayerContextKey(string gameId)
        => $"{gameId}-player";
}
