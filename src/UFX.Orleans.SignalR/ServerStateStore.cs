namespace UFX.Orleans.SignalR;

public class ServerStateStore
{
    private const string AllServers = "ALL_SERVERS";
    private readonly HubGroupState all = new ();
    private readonly HubGroupState groups = new();
    private readonly HubGroupState users = new();
    public ConnectionStateStore Connections { get; } = new();
    public ValueTask<IReadOnlyList<string>> GetAllServers() => new (all.GetServers(AllServers));
    public ValueTask<IReadOnlyList<string>> GetGroupServers(string groupName) => new (groups.GetServers(groupName));
    public ValueTask<IReadOnlyList<string>> GetUserServers(string userName) => new (users.GetServers(userName));
    public ValueTask AddServerToAll(string server) => all.AddServerAsync(server, AllServers);
    public ValueTask AddServerToGroup(string server, string groupName) => groups.AddServerAsync(server, groupName);
    public ValueTask AddServerToUser(string server, string userName) => users.AddServerAsync(server, userName);
    public async ValueTask RemoveServerFromAll(string server)
    {
        await all.RemoveServerAsync(server, AllServers);
        await groups.RemoveServerAsync(server);
        await users.RemoveServerAsync(server);
    }
    public ValueTask RemoveServerFromGroup(string server, string groupName) => groups.RemoveServerAsync(server, groupName);
    public ValueTask RemoveServerFromUser(string server, string userName) => users.RemoveServerAsync(server, userName);
    public ValueTask SetServerGroupState(string server, HashSet<string> group) => groups.SetStateAsync(server, group);
    public ValueTask SetServerUserState(string server, HashSet<string> user) => users.SetStateAsync(server, user);
}