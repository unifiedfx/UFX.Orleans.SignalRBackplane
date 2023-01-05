using Microsoft.Extensions.Logging;
using Orleans.Concurrency;
using Orleans.Placement;
using UFX.Orleans.SignalR.Abstractions;

namespace UFX.Orleans.SignalR.Grains;

[Reentrant]
[PreferLocalPlacement]
public class ServerGrain : Grain, IServerGrain
{
    private readonly ILogger<ServerGrain> logger;
    private readonly IHubServerIdProvider hubServerIdProvider;
    private readonly IServerStateProvider serverStateProvider;
    private ServerStateStore? store;
    private IServerObserver? observer;
    private string? hubName;
    private IDisposable? timer;

    public ServerGrain(ILogger<ServerGrain> logger, IHubServerIdProvider hubServerIdProvider, IServerStateProvider serverStateProvider)
    {
        this.logger = logger;
        this.hubServerIdProvider = hubServerIdProvider;
        this.serverStateProvider = serverStateProvider;
    }
    public ValueTask<bool> CheckSubscriber()
    {
        logger.LogDebug("CheckSubscriber: {Server}, Subscribed: {Observer}",IdentityString, observer != null);
        if (hubServerIdProvider.IsLocal(IdentityString)) return new (observer != null);
        logger.LogDebug("CheckSubscriber: Subscriber not local {Server}", IdentityString);
        DeactivateOnIdle();
        return new(false);
    }
    public async Task<bool> Subscribe(IServerObserver subscriber, string hub)
    {
        if (!hubServerIdProvider.IsLocal(IdentityString))
        {
            logger.LogDebug("Subscribe: Subscriber not local {Server}", IdentityString);
            DeactivateOnIdle();
            return false;
        }
        logger.LogDebug("Subscribe to server: {Server}",IdentityString);
        hubName = hub;
        store ??= serverStateProvider.GetStore(hubName);
        observer = subscriber;
        var servers = await GetServers();
        var tasks = servers.Select(GetServerState);
        await Task.WhenAll(tasks);
        timer ??= RegisterTimer(_ => Heartbeat(), null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
        return true;
    }
    private async Task GetServerState(IServerGrain server)
    {
        var id = server.GetPrimaryKeyString();
        if(string.IsNullOrEmpty(id) || store is null) return;
        await server.AddServerToAll(IdentityString);
        await store.SetServerGroupState(id, await server.GetServerGroupState());
        await store.SetServerUserState(id, await server.GetServerUserState());
        
    }
    private async Task<List<IServerGrain>> GetServers()
    {
        var hub = GrainFactory.GetGrain<IHubGrain>(hubName);
        await hub.AddServerToAll(IdentityString);
        var servers = await hub.GetAllServers();
        return servers.Except(new[]{IdentityString}).Select(s => GrainFactory.GetGrain<IServerGrain>(s)).ToList();
    }
    private async Task Heartbeat()
    {
        logger.LogDebug("Heartbeat: {Server}", IdentityString);
        var hub = GrainFactory.GetGrain<IHubGrain>(hubName);
        // var servers = await hub.GetAllServers();
        await hub.AddServerToAll(IdentityString);
        DelayDeactivation(TimeSpan.FromHours(24));
        // if(servers.Contains(IdentityString)) return;
        // store.GetAllServers()
    }
    public async Task AddServerToAll(string server)
    {
        if (!await CheckSubscriber() || store is null) return;
        await store.AddServerToAll(server);
    }
    public async Task AddServerToGroup(string server, string groupName)
    {
        if (!await CheckSubscriber() || store is null) return;
        await store.AddServerToGroup(server, groupName);
    }
    public async Task AddServerToUser(string server, string userName)
    {
        if (!await CheckSubscriber() || store is null) return;
        await store.AddServerToUser(server, userName);
    }
    public async Task RemoveServerFromAll(string server)
    {
        if (!await CheckSubscriber() || store is null) return;
        await store.RemoveServerFromAll(server);
    }
    public async Task RemoveServerFromGroup(string server, string groupName)
    {
        if (!await CheckSubscriber() || store is null) return;
        await store.RemoveServerFromGroup(server, groupName);
    }
    public async Task RemoveServerFromUser(string server, string userName)
    {
        if (!await CheckSubscriber() || store is null) return;
        await store.RemoveServerFromUser(server, userName);
    }
    public async Task<HashSet<string>> GetServerGroupState()
    {
        if (!await CheckSubscriber() || store is null) throw new TaskCanceledException($"Server ({IdentityString}) does not have a subscription");
        return store.Connections.GetGroups();
    }
    public async Task<HashSet<string>> GetServerUserState()
    {
        if (!await CheckSubscriber() || store is null) throw new TaskCanceledException($"Server ({IdentityString}) does not have a subscription");
        return store.Connections.GetUsers();
    }
    public async Task AddConnectionToLocalGroup(string connection, string group)
    {
        if (!await CheckSubscriber()) return;
        await observer!.AddConnectionToLocalGroup(connection, group);
    }
    public async Task RemoveConnectionFromLocalGroup(string connection, string group)
    {
        if (!await CheckSubscriber()) return;
        await observer!.RemoveConnectionFromLocalGroup(connection, group);
    }
    public async Task SendToLocalGroup(string group, InvocationRequest request, IReadOnlyList<string>? excludedConnectionIds = default)
    {
        if (!await CheckSubscriber()) return;
        await observer!.SendToLocalGroup(group, request, excludedConnectionIds);
    }
    public async Task SendToLocalUser(string user, InvocationRequest request)
    {
        if (!await CheckSubscriber()) return;
        await observer!.SendToLocalUser(user, request);
    }
    public async Task SendToLocalAll(InvocationRequest request, IReadOnlyList<string>? excludedConnectionIds = default)
    {
        logger.LogInformation("SendToAll to server {ServerId} calling method: {Target}",IdentityString, request.Target);
        if (!await CheckSubscriber()) return;
        await observer!.SendToLocalAll(request, excludedConnectionIds);
    }
    public async Task SendToLocalConnections(InvocationRequest request, IReadOnlyList<string> connectionIds)
    {
        if (!await CheckSubscriber()) return;
        await observer!.SendToLocalConnections(request, connectionIds);
    }
}