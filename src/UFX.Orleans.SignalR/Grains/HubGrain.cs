using Microsoft.Extensions.Logging;
using Orleans.Concurrency;
using Orleans.Runtime;
using UFX.Orleans.SignalR.Abstractions;

namespace UFX.Orleans.SignalR.Grains;

[Reentrant]
public class HubGrain : Grain, IHubGrain
{
    private readonly IPersistentState<HubAllState> all;
    private readonly IPersistentState<HubGroupState> groups;
    private readonly IPersistentState<HubGroupState> users;
    private readonly ILogger<HubGrain> logger;

    public HubGrain(
        [PersistentState("hubAll")]IPersistentState<HubAllState> all,
        [PersistentState("hubGroup")]IPersistentState<HubGroupState> groups,
        [PersistentState("hubUser")]IPersistentState<HubGroupState> users,
        ILogger<HubGrain> logger)
    {
        this.all = all;
        this.groups = groups;
        this.users = users;
        this.logger = logger;
    }
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        RegisterTimer(_ => CheckSubscribers(), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        return base.OnActivateAsync(cancellationToken);
    }
    private async Task CheckSubscribers()
    {
        logger.LogInformation("Checking {Count} Subscribers",all.State.Servers.Count);
        var servers = all.State.GetServers().ToDictionary(k => k, s => GetServer(s).CheckSubscriber());
        await Task.WhenAll(servers.Values);
        foreach (var server in servers)
        {
            if(!server.Value.Result) await RemoveServer(server.Key);
        }
    }
    private IServerGrain GetServer(string server) => GrainFactory.GetGrain<IServerGrain>(server);
    public Task<IReadOnlyList<string>> GetAllServers() =>
        Task.FromResult<IReadOnlyList<string>>(all.State.GetServers());
    public Task<IReadOnlyList<string>> GetUserServers(string userName) =>
        Task.FromResult<IReadOnlyList<string>>(users.State.GetServers(userName).ToList());
    public Task<IReadOnlyList<string>> GetGroupServers(string groupName) =>
        Task.FromResult<IReadOnlyList<string>>(groups.State.GetServers(groupName).ToList());
    
    public async Task AddServerToGroup(string server, string group)
    {
        logger.LogInformation("Adding server {Server} to group: {Group}",server,group);
        groups.State.AddServer(server, group);
        await groups.WriteStateAsync();
    }
    public async Task AddConnectionToGroup(string connection, string group)
    {
        var servers = all.State.GetServers().Select(GetServer);
        foreach (var server in servers)
        {
            await server.AddConnectionToGroup(connection, group);
        }
    }
    public async Task AddServerToUser(string server, string user)
    {
        users.State.AddServer(server,user);
        await users.WriteStateAsync();
    } 
    public async Task RemoveServerFromGroup(string server, string group)
    {
        groups.State.RemoveServer(server,group);
        await groups.WriteStateAsync();
    }
    public async Task RemoveConnectionFromGroup(string connection, string group)
    {
        var servers = all.State.GetServers().Select(GetServer);
        foreach (var server in servers)
        {
            await server.RemoveConnectionFromGroup(connection, group);
        }
    }
    public async Task RemoveServerFromUser(string server, string user)
    {
        users.State.RemoveServer(server,user);
        await users.WriteStateAsync();
    }
    public async Task AddServer(string server)
    {
        all.State.AddServer(server);
        await all.WriteStateAsync();
    }
    public async Task RemoveServer(string server)
    {
        logger.LogInformation("Removing Server {Server}", server);
        if(all.State.RemoveServer(server)) await all.WriteStateAsync();
        if(groups.State.RemoveServer(server)) await groups.WriteStateAsync();
        if(users.State.RemoveServer(server)) await users.WriteStateAsync();
    }
}