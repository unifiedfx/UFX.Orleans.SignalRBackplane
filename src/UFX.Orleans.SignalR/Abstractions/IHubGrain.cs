namespace UFX.Orleans.SignalR.Abstractions;

public interface IHubGrain : IGrainWithStringKey
{
    Task<IReadOnlyList<string>> GetAllServers();
    Task<IReadOnlyList<string>> GetUserServers(string userName);
    Task<IReadOnlyList<string>> GetGroupServers(string groupName);
    Task AddServer(string server);
    Task AddServerToGroup(string server, string group);
    Task AddServerToUser(string server, string user);
    Task RemoveServerFromGroup(string server, string group);
    Task RemoveServerFromUser(string server, string user);
}