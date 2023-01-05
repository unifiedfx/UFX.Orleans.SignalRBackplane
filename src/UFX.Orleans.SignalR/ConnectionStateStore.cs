using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using UFX.Orleans.SignalR.Abstractions;

namespace UFX.Orleans.SignalR;

public class ConnectionStateStore
{
    public HubConnectionStore Store { get; } = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> groups = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> users = new();

    public bool ContainsGroup(string groupName) => groups.ContainsKey(groupName);
    public bool ContainsUser(string userName) => users.ContainsKey(userName);
    public HashSet<string> GetGroups() => groups.Keys.ToHashSet();
    public HashSet<string> GetUsers() => users.Keys.ToHashSet();
    public List<string> GetGroupConnections(string groupName) =>
        groups.TryGetValue(groupName, out var store) ? store.ToList() : new();
    public List<string> GetUserConnections(string userName) =>
        users.TryGetValue(userName, out var store) ? store.ToList() : new();
    public ValueTask<bool> AddConnectionToGroup(string connectionId, string groupName)
    {
        var connection = Store[connectionId];
        if (connection is null) return new(false);
        var feature = connection.Features.GetRequiredFeature<ISignalRGroupFeature>();
        var groupNames = feature.Groups;
        lock (groupNames)
        {
            if (!groupNames.Add(groupName)) return new(false);
        }
        var store = groups.GetOrAdd(groupName, _ => new ());
        var first = !store.Any();
        store.Add(connection.ConnectionId);
        return new(first);
    }
    public ValueTask<bool> RemoveConnectionFromGroup(string connectionId, string groupName)
    {
        var connection = Store[connectionId];
        if (connection is null) return new(false);
        var feature = connection.Features.GetRequiredFeature<ISignalRGroupFeature>();
        var groupNames = feature.Groups;
        lock (groupNames)
        {
            groupNames.Remove(groupName);
        }
        if(!groups.TryGetValue(groupName, out var store)) return new(false);
        store.Remove(connection.ConnectionId);
        if(store.Any()) return new(false);
        if(!groups.TryRemove(groupName, out _)) return new(false);
        return new(true);
    }

    public ValueTask<bool> AddConnectionToUser(HubConnectionContext connection)
    {
        Store.Add(connection);
        return string.IsNullOrEmpty(connection.UserIdentifier)
            ? new(false)
            : AddConnectionToUser(connection.ConnectionId, connection.UserIdentifier);
    }
    public ValueTask<bool> AddConnectionToUser(string connectionId, string userName)
    {
        var connection = Store[connectionId];
        if (connection is null) return new(false);
        var store = users.GetOrAdd(userName, _ => new ());
        store.Add(connection.ConnectionId);
        return new(true);
    }
    public ValueTask<bool> RemoveConnectionFromUser(HubConnectionContext connection)
    {
        return string.IsNullOrEmpty(connection.UserIdentifier)
            ? new(false)
            : RemoveConnectionFromUser(connection.ConnectionId, connection.UserIdentifier);
    }
    public ValueTask<bool> RemoveConnectionFromUser(string connectionId, string userName)
    {
        var connection = Store[connectionId];
        if (connection is null) return new(false);
        if(!users.TryGetValue(userName, out var store)) return new(true);
        store.Remove(connection.ConnectionId);
        if(store.Count > 0) return new(false);
        if(!groups.TryRemove(userName, out _)) return new(false);
        return new(true);
    }
}