using System;
using System.Threading.Tasks;
using Gameboard.Api.Hubs;
using Gameboard.Api.Structure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Games;

public interface IGameHubApi { }

[Authorize(AppConstants.HubPolicy)]
public class GameHub : Hub<IGameHubEvent>, IGameHubApi, IGameboardHub
{
    private readonly IGameHubService _gameHubService;
    private readonly ILogger<GameHub> _logger;

    public GameHub
    (
        IGameHubService gameHubService,
        ILogger<GameHub> logger
    )
    {
        _gameHubService = gameHubService;
        _logger = logger;
    }

    public GameboardHubType GroupType => GameboardHubType.Game;

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        this.LogOnConnected(_logger, Context);

        var activeEnrollments = await _gameHubService.GetActiveEnrollments(Context.UserIdentifier);

        foreach (var enrollment in activeEnrollments)
            await JoinGame(enrollment);

        await _gameHubService.SendYourActiveGamesChanged(Context.UserIdentifier);
    }

    public override Task OnDisconnectedAsync(Exception ex)
    {
        this.LogOnDisconnected(_logger, Context, ex);
        return base.OnDisconnectedAsync(ex);
    }

    private async Task JoinGame(GameHubActiveEnrollment enrollment)
    {
        _logger.LogInformation(LogEventId.GameHub_Group_JoinStart, message: $"""User {Context.UserIdentifier} is joining the group for game "{enrollment.Game.Id}"...""");
        await Groups.AddToGroupAsync(Context.ConnectionId, enrollment.Game.Id);
        _logger.LogInformation(LogEventId.GameHub_Group_JoinEnd, message: $"""User {Context.UserIdentifier} joined the group for game "{enrollment.Game.Id}".""");
    }
}
