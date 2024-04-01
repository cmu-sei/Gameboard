using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Gameboard.Api.Hubs;

namespace Gameboard.Api.Features.Hubs;

public interface IGameboardHubConnectionMap
{
    void AddToGroup(string groupId, GameboardHubUserConnection connection);
    IEnumerable<GameboardHubUserConnection> GetGroupConnections(string groupId);
    IEnumerable<string> GetUserConnectionIds(string userId);
    void RemoveFromGroup(string groupId, string connectionId);
    void RemoveUserFromGroup(string groupId, string userId);
}

internal class GameboardHubConnectionMap : IGameboardHubConnectionMap
{
    private static readonly ConcurrentDictionary<string, IList<GameboardHubUserConnection>> _connections = new();

    public void AddToGroup(string groupId, GameboardHubUserConnection connection)
    {
        _connections.AddOrUpdate
        (
            groupId,
            groupId => new List<GameboardHubUserConnection>() { connection },
            (groupId, connectionList) =>
            {
                connectionList.Add(connection);
                return connectionList;
            }
        );
    }

    public IEnumerable<GameboardHubUserConnection> GetGroupConnections(string groupId)
        => _connections.GetOrAdd(groupId, new List<GameboardHubUserConnection>());

    public void RemoveFromGroup(string groupId, string connectionId)
    {
        lock (_connections)
        {
            var existingConnections = _connections[groupId];
            _connections.TryUpdate
            (
                groupId,
                existingConnections.Where(c => c.ConnectionId != connectionId).ToList(),
                existingConnections
            );
        }
    }

    public IEnumerable<string> GetUserConnectionIds(string userId)
    {
        return _connections
            .Values
            .SelectMany(v => v.Where(c => c.UserId == userId).Select(c => c.ConnectionId));
    }

    public void RemoveUserFromGroup(string groupId, string userId)
    {
        lock (_connections)
        {
            var hasGroup = _connections.TryGetValue(groupId, out var connections);
            if (!hasGroup)
                return;

            _connections.TryUpdate
            (
                groupId,
                connections.Where(c => c.UserId != userId).ToList(),
                connections
            );
        }
    }
}
