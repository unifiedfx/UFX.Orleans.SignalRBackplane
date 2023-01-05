using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Orleans.Concurrency;
using Orleans.Runtime;
using UFX.Orleans.SignalR.Abstractions;

namespace UFX.Orleans.SignalR.Grains;

[Reentrant]
public class HubGrain : Grain, IHubGrain
{
    private readonly IPersistentState<ConcurrentDictionary<string, DateTimeOffset>> servers;
    private readonly ILogger<HubGrain> logger;
    public HubGrain(ILogger<HubGrain> logger, [PersistentState("hubServers")]IPersistentState<ConcurrentDictionary<string, DateTimeOffset>> servers)
    {
        this.logger = logger;
        this.servers = servers;
    }
    private IServerGrain GetServer(string server) => GrainFactory.GetGrain<IServerGrain>(server);
    public ValueTask<IReadOnlyList<string>> GetAllServers() => new(servers.State.Keys.ToList());
    public async Task AddServerToAll(string server)
    {
        logger.LogDebug("AddServer: {Server}", server);
        servers.State.AddOrUpdate(server, _ => DateTimeOffset.UtcNow, (_,_) => DateTimeOffset.UtcNow);
        var expiry = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromSeconds(30));
        var expired = servers.State.Where(s => s.Value < expiry).Select(s => s.Key).ToList();
        if (!expired.Any())
        {
            await servers.WriteStateAsync();
            return;
        }
        logger.LogDebug("Expired: {Expired}", string.Join(", ", expired));
        foreach (var remove in expired) servers.State.Remove(remove, out _);
        await servers.WriteStateAsync();
        var update = servers.State.Select(s => s.Key).Select(GetServer);
        var tasks = new List<Task>();
        foreach (var serverGrain in update)
        {
            tasks.AddRange(expired.Select(e => serverGrain.RemoveServerFromAll(e)));
        }
        await Task.WhenAll(tasks);
    }
}