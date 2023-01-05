using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Orleans.Concurrency;
using UFX.Orleans.SignalR.Abstractions;

namespace UFX.Orleans.SignalR;

[Reentrant]
public class OrleansHubLifetimeManager<THub> : HubLifetimeManager<THub>, IServerObserver where THub : Hub
{
    private readonly SemaphoreSlim subscriptionLock = new (1, 1);
    private readonly IClusterClient client;
    private readonly ILogger<OrleansHubLifetimeManager<THub>> logger;
    private readonly ServerStateStore state;
    private readonly string hubName;
    private readonly string serverId;
    private bool subscribed;

    public OrleansHubLifetimeManager(IClusterClient client, IHubServerIdProvider hubServerIdProvider, ILogger<OrleansHubLifetimeManager<THub>> logger, IServerStateProvider serverStateProvider)
    {
        var hubType = typeof(THub).BaseType?.GenericTypeArguments.FirstOrDefault() ?? typeof(THub);
        var name = hubType.Name.AsSpan();
        hubName = hubType.IsInterface && name[0] == 'I'
            ? new string(name[1..])
            : hubType.Name;
        this.client = client;
        this.logger = logger;
        state = serverStateProvider.GetStore(hubName);
        serverId = hubServerIdProvider.GetHubServerId(hubName);
    }
    private IServerGrain GetServer(string server) => client.GetGrain<IServerGrain>(server);
    private async Task<IEnumerable<IServerGrain>> GetAllServers() =>
        (await state.GetAllServers()).Select(GetServer);
    private async Task<IEnumerable<IServerGrain>> GetGroupServers(string groupName) =>
        (await state.GetGroupServers(groupName)).Select(GetServer);
    private async Task<IEnumerable<IServerGrain>> GetUserServers(string userName) =>
        (await state.GetUserServers(userName)).Select(GetServer);
    private async Task UpdateServers(Func<IServerGrain, Task> update) =>
        await Task.WhenAll((await state.GetAllServers()).Select(GetServer).Select(update));
    private async Task SubscribeToServer()
    {
        await subscriptionLock.WaitAsync();
        try
        {
            var server = GetServer(serverId);
            // subscribed = await server.CheckSubscriber();
            // logger.LogDebug("SubscribeToServer: {Subscribed}", subscribed);
            if (subscribed) return;
            var subscriber = client.CreateObjectReference<IServerObserver>(this);
            var limit = 10;
            while (!await server.Subscribe(subscriber, hubName) && limit-- > 0) await Task.Delay(TimeSpan.FromSeconds(5));
            subscribed = await server.CheckSubscriber();
            logger.LogDebug("SubscribeToServer: {Subscribed}", subscribed);
        }
        finally
        {
            subscriptionLock.Release();
        }
    }
    public override async Task OnConnectedAsync(HubConnectionContext connection)
    {
        logger.LogDebug("OnConnectedAsync: {ConnectionId}", connection.ConnectionId);
        await SubscribeToServer();
        connection.Features.Set<ISignalRGroupFeature>(new SignalRGroupFeature());
        if(!await state.Connections.AddConnectionToUser(connection)) return;
        await UpdateServers(s => s.AddServerToUser(serverId, connection.UserIdentifier!));
    }
    public override async Task OnDisconnectedAsync(HubConnectionContext connection)
    {
        logger.LogDebug("OnDisconnectedAsync: {ConnectionId}", connection.ConnectionId);
        var tasks = new List<Task>();
        var feature = connection.Features.GetRequiredFeature<ISignalRGroupFeature>();
        var groupNames = feature.Groups;
        foreach (var group in groupNames.ToArray())
        {
            tasks.Add(RemoveGroupAsyncCore(connection, group));
        }
        tasks.Add(RemoveUserAsyncCore(connection));
        await Task.WhenAll(tasks);
        state.Connections.Store.Remove(connection);
    }
    private async Task RemoveUserAsyncCore(HubConnectionContext connection)
    {
        if(!await state.Connections.RemoveConnectionFromUser(connection)) return;
        await UpdateServers(s => s.RemoveServerFromUser(serverId, connection.UserIdentifier!));
    }
    public override async Task AddToGroupAsync(string connectionId, string groupName,
        CancellationToken cancellationToken = new CancellationToken())
    {
        if (connectionId == null) throw new ArgumentNullException(nameof(connectionId));
        if (groupName == null) throw new ArgumentNullException(nameof(groupName));
        await AddConnectionToLocalGroup(connectionId, groupName);
        if(state.Connections.Store[connectionId] is null)
        {
            var servers = await GetAllServers();
            var tasks = servers.Select(s => s.AddConnectionToLocalGroup(connectionId, groupName));
            await Task.WhenAll(tasks);
        }
    }
    public override async Task RemoveFromGroupAsync(string connectionId, string groupName,
        CancellationToken cancellationToken = new CancellationToken())
    {
        if (connectionId == null) throw new ArgumentNullException(nameof(connectionId));
        if (groupName == null) throw new ArgumentNullException(nameof(groupName));
        var connection = state.Connections.Store[connectionId];
        if (connection != null) await RemoveGroupAsyncCore(connection, groupName);
        else
        {
            var servers = await GetAllServers();
            var tasks = servers.Select(s => s.RemoveConnectionFromLocalGroup(connectionId, groupName));
            await Task.WhenAll(tasks);
        }
    }
    private async Task RemoveGroupAsyncCore(HubConnectionContext connection, string groupName)
    {
        if(!await state.Connections.RemoveConnectionFromGroup(connection.ConnectionId, groupName)) return;
        await UpdateServers(s => s.RemoveServerFromGroup(serverId, groupName));
    }
    public override async Task SendAllAsync(string methodName, object?[] args, CancellationToken cancellationToken = new CancellationToken())
    {
        var request = new InvocationRequest(methodName, args);
        var localTask = SendToAllConnections(request, null, null, cancellationToken);
        var servers = await GetAllServers();
        var tasks = servers.Select(s => s.SendToLocalAll(request)).Prepend(localTask);
        await Task.WhenAll(tasks);
    }

    public override async Task SendAllExceptAsync(string methodName, object?[] args, IReadOnlyList<string> excludedConnectionIds,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var request = new InvocationRequest(methodName, args);
        await SendToAllConnections(request, (connection, state) => !((IReadOnlyList<string>)state!).Contains(connection.ConnectionId), excludedConnectionIds, cancellationToken);
        var servers = await GetAllServers();
        var tasks = servers.Select(s => s.SendToLocalAll(request, excludedConnectionIds));
        await Task.WhenAll(tasks);
    }

    public override async Task SendConnectionAsync(string connectionId, string methodName, object?[] args,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var request = new InvocationRequest(methodName, args);
        if (state.Connections.Store[connectionId] is not null)
        {
            await SendToAllConnections(request, (connection, state) => ((string)state!).Equals(connection.ConnectionId), connectionId, cancellationToken);
        }
        else
        {
            var servers = await GetAllServers();
            var tasks = servers.Select(s => s.SendToLocalConnections(request, new[] {connectionId}));
            await Task.WhenAll(tasks);
        }
    }
    public override async Task SendConnectionsAsync(IReadOnlyList<string> connectionIds, string methodName, object?[] args,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var request = new InvocationRequest(methodName, args);
        await SendToAllConnections(request, (connection, state) => ((IReadOnlyList<string>)state!).Contains(connection.ConnectionId), connectionIds, cancellationToken);
        var servers = await GetAllServers();
        var tasks = servers.Select(s => s.SendToLocalConnections(request, connectionIds));
        await Task.WhenAll(tasks);
    }
    public override async Task SendGroupAsync(string groupName, string methodName, object?[] args,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var request = new InvocationRequest(methodName, args);
        await SendToLocalGroup(groupName, request, null);
        var servers = await GetGroupServers(groupName);
        var tasks = servers.Select(s => s.SendToLocalGroup(groupName, request));
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
        var connectionIds = state.Connections.GetGroupConnections(groupName);
        connectionIds.RemoveAll(excludedConnectionIds.Contains);
        if (connectionIds.Any())
        {
            await SendToAllConnections(request, (connection, state) => ((IReadOnlyList<string>)state!).Contains(connection.ConnectionId), connectionIds, cancellationToken);
        }
        var servers = await GetGroupServers(groupName);
        var tasks = servers.Select(s => s.SendToLocalGroup(groupName, request, excludedConnectionIds));
        await Task.WhenAll(tasks);
    }
    public override async Task SendUserAsync(string userId, string methodName, object?[] args,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var request = new InvocationRequest(methodName, args);
        await SendToLocalUser(userId, request);
        var servers = await GetUserServers(userId);
        var tasks = servers.Select(s => s.SendToLocalUser(userId, request));
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
        foreach (var connection in this.state.Connections.Store)
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
    public async Task SendToLocalConnections(InvocationRequest request, IReadOnlyList<string> connectionIds)
    {
        await SendToAllConnections(request, (connection, state) => ((IReadOnlyList<string>)state!).Contains(connection.ConnectionId), connectionIds);
    }
    public async Task SendToLocalAll(InvocationRequest request, IReadOnlyList<string>? excludedConnectionIds)
    {
        await SendToAllConnections(request, (connection, state) => !((IReadOnlyList<string>)state!).Contains(connection.ConnectionId), excludedConnectionIds);
    }
    public async Task SendToLocalGroup(string groupName, InvocationRequest request, IReadOnlyList<string>? excludedConnectionIds)
    {
        var connectionIds = state.Connections.GetGroupConnections(groupName);
        if(excludedConnectionIds is not null) connectionIds.RemoveAll(excludedConnectionIds.Contains);
        if (!connectionIds.Any()) return; 
        await SendToAllConnections(request, (connection, state) => ((IReadOnlyList<string>)state!).Contains(connection.ConnectionId), connectionIds);
    }
    public async Task SendToLocalUser(string userName, InvocationRequest request)
    {
        var connectionIds = state.Connections.GetUserConnections(userName);
        if (!connectionIds.Any()) return; 
        await SendToAllConnections(request, (connection, state) => ((IReadOnlyList<string>)state!).Contains(connection.ConnectionId), connectionIds);
    }
    public async Task AddConnectionToLocalGroup(string connectionId, string groupName)
    {
        if (!await state.Connections.AddConnectionToGroup(connectionId, groupName)) return; 
        await UpdateServers(s => s.AddServerToGroup(serverId, groupName));
    }
    public async Task RemoveConnectionFromLocalGroup(string connectionId, string groupName)
    {
        var connection = state.Connections.Store[connectionId];
        if(connection == null) return;
        await RemoveGroupAsyncCore(connection, groupName);
    }
    // public async Task RefreshServer(bool sync)
    // {
    //     logger.LogDebug("RefreshServer: {Sync}",sync);
    //     // await SubscribeToServer();
    //     // await GetHubGrain().AddServer(serverId);
    //     if(!sync) return;
    //     await GetHubGrain().SetServerState(serverId, state.Connections.GetUsers(), state.Connections.GetGroups());
    // }
}