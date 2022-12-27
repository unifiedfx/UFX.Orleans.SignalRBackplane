using Microsoft.Extensions.Logging;
using Orleans.Placement;
using UFX.Orleans.SignalR.Abstractions;

namespace UFX.Orleans.SignalR.Grains;

[PreferLocalPlacement]
public class ServerGrain : Grain, IServerGrain
{
    private readonly ILogger<ServerGrain> logger;
    private IServerObserver? observer;

    public ServerGrain(ILogger<ServerGrain> logger) => this.logger = logger;

    public Task<bool> CheckSubscriber()
    {
        if (observer is not null) return Task.FromResult(true);
        DeactivateOnIdle();
        return Task.FromResult(false);
    }
    public Task Subscribe(IServerObserver subscriber)
    {
        logger.LogInformation("Subscribe to server: {Server}",IdentityString);
        observer = subscriber;
        return Task.CompletedTask;
    }
    public async Task AddConnectionToGroup(string connection, string group)
    {
        if (!await CheckSubscriber()) return;
        await observer!.AddConnectionToGroup(connection, group);
    }
    public async Task RemoveConnectionFromGroup(string connection, string group)
    {
        if (!await CheckSubscriber()) return;
        await observer!.RemoveConnectionFromGroup(connection, group);
    }
    public async Task SendToGroup(string group, InvocationRequest request, IReadOnlyList<string>? excludedConnectionIds = default)
    {
        if (!await CheckSubscriber()) return;
        await observer!.SendToGroup(group, request, excludedConnectionIds);
    }
    public async Task SendToUser(string user, InvocationRequest request, IReadOnlyList<string>? excludedConnectionIds = default)
    {
        if (!await CheckSubscriber()) return;
        await observer!.SendToUser(user, request, excludedConnectionIds);
    }
    public async Task SendToAll(InvocationRequest request, IReadOnlyList<string>? excludedConnectionIds = default)
    {
        logger.LogInformation("SendToAll to server {ServerId} calling method: {Target}",IdentityString, request.Target);
        if (!await CheckSubscriber()) return;
        await observer!.SendToAll(request, excludedConnectionIds);
    }
    public async Task SendToConnections(InvocationRequest request, IReadOnlyList<string>? connectionIds)
    {
        if (!await CheckSubscriber()) return;
        await observer!.SendToConnections(request, connectionIds);
    }
}