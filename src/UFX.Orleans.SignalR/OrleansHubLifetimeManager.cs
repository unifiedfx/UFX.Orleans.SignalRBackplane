using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using UFX.Orleans.SignalR.Abstractions;

namespace UFX.Orleans.SignalR;

public class OrleansHubLifetimeManager<THub> : HubLifetimeManager<THub>, IServerObserver where THub : Hub
{
    private readonly HubConnectionStore connections = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> groups = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> users = new();
    private readonly IClusterClient client;
    private readonly ILogger<OrleansHubLifetimeManager<THub>> logger;
    private readonly string hubName;
    private readonly string serverId;
    private IHubGrain? hubGrain;
    private bool subscribed;

    public OrleansHubLifetimeManager(IClusterClient client, IHubServerIdProvider hubServerIdProvider, ILogger<OrleansHubLifetimeManager<THub>> logger)
    {
        var hubType = typeof(THub).BaseType?.GenericTypeArguments.FirstOrDefault() ?? typeof(THub);
        var name = hubType.Name.AsSpan();
        hubName = hubType.IsInterface && name[0] == 'I'
            ? new string(name[1..])
            : hubType.Name;
        this.client = client;
        this.logger = logger;
        serverId = hubServerIdProvider.GetServerId(hubName);
    }
    private IHubGrain GetHubGrain() => hubGrain ??= client.GetGrain<IHubGrain>(hubName);
    private IServerGrain GetServer(string server) => client.GetGrain<IServerGrain>(server);
    private IEnumerable<IServerGrain> GetServers(IEnumerable<string> servers) =>
        servers.Except(new[] {serverId}).Select(GetServer).ToList();
    private async Task SubscribeToServer()
    {
        var server = GetServer(serverId);
        subscribed = await server.CheckSubscriber();
        logger.LogInformation("CheckSubscriber: {Subscribed}", subscribed);
        if(subscribed) return;
        await GetServer(serverId).Subscribe(client.CreateObjectReference<IServerObserver>(this));
        subscribed = await server.CheckSubscriber();
        logger.LogInformation("CheckSubscriber: {Subscribed}", subscribed);
    }
    private async Task EnsureServerSubscription()
    {
        if(subscribed) return;
        await SubscribeToServer();
        await GetHubGrain().AddServer(serverId);
    }
    public override async Task OnConnectedAsync(HubConnectionContext connection)
    {
        await EnsureServerSubscription();
        connection.Features.Set<ISignalRGroupFeature>(new SignalRGroupFeature());
        connections.Add(connection);
        if (!string.IsNullOrEmpty(connection.UserIdentifier))
        {
            var store = users.GetOrAdd(connection.UserIdentifier, _ => new ());
            store.Add(connection.ConnectionId);
            await GetHubGrain().AddServerToUser(serverId, connection.UserIdentifier);
        }
    }
    public override async Task OnDisconnectedAsync(HubConnectionContext connection)
    {
        connections.Remove(connection);
        var tasks = new List<Task>();
        var feature = connection.Features.GetRequiredFeature<ISignalRGroupFeature>();
        var groupNames = feature.Groups;
        foreach (var group in groupNames.ToArray())
        {
            tasks.Add(RemoveGroupAsyncCore(connection, group));
        }
        tasks.Add(RemoveUserAsyncCore(connection));
        await Task.WhenAll(tasks);
    }
    private async Task RemoveUserAsyncCore(HubConnectionContext connection)
    {
        if(string.IsNullOrEmpty(connection.UserIdentifier)) return;
        if(!users.TryGetValue(connection.UserIdentifier, out var store)) return;
        store.Remove(connection.ConnectionId);
        if(store.Count > 0) return;
        if(!users.TryRemove(connection.UserIdentifier, out _)) return;
        await GetHubGrain().RemoveServerFromUser(serverId, connection.UserIdentifier);
    }
    public override async Task AddToGroupAsync(string connectionId, string groupName,
        CancellationToken cancellationToken = new CancellationToken())
    {
        if (connectionId == null) throw new ArgumentNullException(nameof(connectionId));
        if (groupName == null) throw new ArgumentNullException(nameof(groupName));
        var connection = connections[connectionId];
        if (connection != null) await AddGroupAsyncCore(connection, groupName);
        else
        {
            var servers = GetServers(await GetHubGrain().GetAllServers());
            var tasks = servers.Select(s => s.AddConnectionToGroup(connectionId, groupName));
            await Task.WhenAll(tasks);
        }
    }
    private async Task AddGroupAsyncCore(HubConnectionContext connection, string groupName)
    {
        var feature = connection.Features.GetRequiredFeature<ISignalRGroupFeature>();
        var groupNames = feature.Groups;
        lock (groupNames)
        {
            if (!groupNames.Add(groupName)) return;
        }
        var store = groups.GetOrAdd(groupName, _ => new ());
        store.Add(connection.ConnectionId);
        await GetHubGrain().AddServerToGroup(serverId, groupName);
    }
    public override async Task RemoveFromGroupAsync(string connectionId, string groupName,
        CancellationToken cancellationToken = new CancellationToken())
    {
        if (connectionId == null) throw new ArgumentNullException(nameof(connectionId));
        if (groupName == null) throw new ArgumentNullException(nameof(groupName));
        var connection = connections[connectionId];
        if (connection != null) await RemoveGroupAsyncCore(connection, groupName);
        else
        {
            var servers = GetServers(await GetHubGrain().GetAllServers());
            var tasks = servers.Select(s => s.RemoveConnectionFromGroup(connectionId, groupName));
            await Task.WhenAll(tasks);
        }
    }
    private async Task RemoveGroupAsyncCore(HubConnectionContext connection, string groupName)
    {
        var feature = connection.Features.GetRequiredFeature<ISignalRGroupFeature>();
        var groupNames = feature.Groups;
        lock (groupNames)
        {
            groupNames.Remove(groupName);
        }
        if(!groups.TryGetValue(groupName, out var store)) return;
        store.Remove(connection.ConnectionId);
        if(store.Count > 0) return;
        if(!groups.TryRemove(groupName, out _)) return;
        await GetHubGrain().RemoveServerFromGroup(serverId, groupName);
    }
    public override async Task SendAllAsync(string methodName, object?[] args, CancellationToken cancellationToken = new CancellationToken())
    {
        var request = new InvocationRequest(methodName, args);
        await SendToAllConnections(request, null, null, cancellationToken);
        var servers = GetServers(await GetHubGrain().GetAllServers());
        var tasks = servers.Select(s => s.SendToAll(request));
        await Task.WhenAll(tasks);
    }

    public override async Task SendAllExceptAsync(string methodName, object?[] args, IReadOnlyList<string> excludedConnectionIds,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var request = new InvocationRequest(methodName, args);
        await SendToAllConnections(request, (connection, state) => !((IReadOnlyList<string>)state!).Contains(connection.ConnectionId), excludedConnectionIds, cancellationToken);
        var servers = GetServers(await GetHubGrain().GetAllServers());
        var tasks = servers.Select(s => s.SendToAll(request, excludedConnectionIds));
        await Task.WhenAll(tasks);
    }

    public override async Task SendConnectionAsync(string connectionId, string methodName, object?[] args,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var request = new InvocationRequest(methodName, args);
        if (connections[connectionId] is not null)
        {
            await SendToAllConnections(request, (connection, state) => ((string)state!).Equals(connection.ConnectionId), connectionId, cancellationToken);
        }
        else
        {
            var servers = GetServers(await GetHubGrain().GetAllServers());
            var tasks = servers.Select(s => s.SendToConnections(request, new[] {connectionId}));
            await Task.WhenAll(tasks);
        }
    }
    public override async Task SendConnectionsAsync(IReadOnlyList<string> connectionIds, string methodName, object?[] args,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var request = new InvocationRequest(methodName, args);
        await SendToAllConnections(request, (connection, state) => ((IReadOnlyList<string>)state!).Contains(connection.ConnectionId), connectionIds, cancellationToken);
        var servers = GetServers(await GetHubGrain().GetAllServers());
        var tasks = servers.Select(s => s.SendToConnections(request, connectionIds));
        await Task.WhenAll(tasks);
    }
    public override async Task SendGroupAsync(string groupName, string methodName, object?[] args,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var request = new InvocationRequest(methodName, args);
        if (groups.TryGetValue(groupName, out var list))
        {
            var connectionIds = list.ToList();
            await SendToAllConnections(request, (connection, state) => ((IReadOnlyList<string>)state!).Contains(connection.ConnectionId), connectionIds, cancellationToken);
        }
        var servers = GetServers(await GetHubGrain().GetGroupServers(groupName));
        var tasks = servers.Select(s => s.SendToGroup(groupName, request));
        await Task.WhenAll(tasks);
    }
    public override async Task SendGroupsAsync(IReadOnlyList<string> groupNames, string methodName, object?[] args,
        CancellationToken cancellationToken = new CancellationToken())
    {
        foreach (var groupName in groupNames)
        {
            if (string.IsNullOrEmpty(groupName)) throw new InvalidOperationException("Cannot send to an empty group name.");
            await SendGroupAsync(groupName, methodName, args, cancellationToken);
        }
    }
    public override async Task SendGroupExceptAsync(string groupName, string methodName, object?[] args, IReadOnlyList<string> excludedConnectionIds,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var request = new InvocationRequest(methodName, args);
        if (groups.TryGetValue(groupName, out var store) && store.Any())
        {
            var connectionIds = store.ToList().RemoveAll(excludedConnectionIds.Contains);
            await SendToAllConnections(request, (connection, state) => ((IReadOnlyList<string>)state!).Contains(connection.ConnectionId), connectionIds, cancellationToken);
        }
        var servers = GetServers(await GetHubGrain().GetGroupServers(groupName));
        var tasks = servers.Select(s => s.SendToGroup(groupName, request, excludedConnectionIds));
        await Task.WhenAll(tasks);
    }
    public override async Task SendUserAsync(string userId, string methodName, object?[] args,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var request = new InvocationRequest(methodName, args);
        if (users.TryGetValue(userId, out var store) && store.Any())
        {
            var connectionIds = store.ToList();
            await SendToAllConnections(request, (connection, state) => ((IReadOnlyList<string>)state!).Contains(connection.ConnectionId), connectionIds, cancellationToken);
        }
        var servers = GetServers(await GetHubGrain().GetUserServers(userId));
        var tasks = servers.Select(s => s.SendToUser(userId, request));
        await Task.WhenAll(tasks);
    }
    public override async Task SendUsersAsync(IReadOnlyList<string> userIds, string methodName, object?[] args,
        CancellationToken cancellationToken = new CancellationToken())
    {
        foreach (var userName in userIds)
        {
            if (string.IsNullOrEmpty(userName)) throw new InvalidOperationException("Cannot send to an empty user name.");
            await SendUserAsync(userName, methodName, args, cancellationToken);
        }
    }
    private Task SendToAllConnections(InvocationRequest request, Func<HubConnectionContext, object?, bool>? include, object? state = null, CancellationToken cancellationToken = default)
    {
        List<Task>? tasks = null;
        // foreach over HubConnectionStore avoids allocating an enumerator
        foreach (var connection in connections)
        {
            if (include != null && state != null && !include(connection, state)) continue;
            var task = connection.WriteAsync(request.ToMessage(), cancellationToken);
            if (!task.IsCompletedSuccessfully)
            {
                tasks ??= new List<Task>();
                tasks.Add(task.AsTask());
            }
            else
            {
                // If it's a IValueTaskSource backed ValueTask,
                // inform it its result has been read so it can reset
                task.GetAwaiter().GetResult();
            }
        }
        if (tasks == null) return Task.CompletedTask;
        // Some connections are slow
        return Task.WhenAll(tasks);
    }
    async Task IServerObserver.SendToConnections(InvocationRequest request, IReadOnlyList<string>? connectionIds)
    {
        await SendToAllConnections(request, (connection, state) => ((IReadOnlyList<string>)state!).Contains(connection.ConnectionId), connectionIds);
    }
    async Task IServerObserver.SendToAll(InvocationRequest request, IReadOnlyList<string>? excludedConnectionIds = default)
    {
        await SendToAllConnections(request, (connection, state) => !((IReadOnlyList<string>)state!).Contains(connection.ConnectionId), excludedConnectionIds);
    }
    async Task IServerObserver.SendToGroup(string group, InvocationRequest request, IReadOnlyList<string>? excludedConnectionIds = default)
    {
        if(!groups.TryGetValue(group, out var groupConnections)) return;
        if (excludedConnectionIds is not null) groupConnections = groupConnections.Except(excludedConnectionIds).ToHashSet();
        await SendToAllConnections(request, (connection, state) => ((HashSet<string>)state!).Contains(connection.ConnectionId), groupConnections);
    }
    async Task IServerObserver.SendToUser(string user, InvocationRequest request, IReadOnlyList<string>? excludedConnectionIds = default)
    {
        if(!users.TryGetValue(user, out var userConnections)) return;
        if (excludedConnectionIds is not null) userConnections = userConnections.Except(excludedConnectionIds).ToHashSet();
        await SendToAllConnections(request, (connection, state) => ((HashSet<string>)state!).Contains(connection.ConnectionId), userConnections);
    }
    async Task IServerObserver.AddConnectionToGroup(string connectionId, string group)
    {
        var connection = connections[connectionId];
        if(connection == null) return;
        await AddGroupAsyncCore(connection, group);
    }
    async Task IServerObserver.RemoveConnectionFromGroup(string connectionId, string group)
    {
        var connection = connections[connectionId];
        if(connection == null) return;
        await RemoveGroupAsyncCore(connection, group);
    }
}