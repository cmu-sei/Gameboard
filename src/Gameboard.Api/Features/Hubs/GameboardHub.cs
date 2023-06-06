using Microsoft.AspNetCore.SignalR;

namespace Gameboard.Api.Hubs;

public interface IGameboardHub : ICanonicalGroupIdProvider
{
    public HubCallerContext Context { get; }
    public IGroupManager Groups { get; }
}

public interface IGameboardHubBus : ICanonicalGroupIdProvider { }
