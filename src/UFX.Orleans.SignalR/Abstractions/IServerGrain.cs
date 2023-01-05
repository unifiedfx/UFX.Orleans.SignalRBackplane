
namespace UFX.Orleans.SignalR.Abstractions;

public interface IServerGrain : IGrainWithStringKey
{
    ValueTask<bool> CheckSubscriber();
    Task<bool> Subscribe(IServerObserver subscriber, string hub);
    Task AddServerToAll(string server);
    Task AddServerToGroup(string server, string groupName);
    Task AddServerToUser(string server, string userName);
    Task RemoveServerFromAll(string server);
    Task RemoveServerFromGroup(string server, string groupName);
    Task RemoveServerFromUser(string server, string userName);
    Task<HashSet<string>> GetServerGroupState();
    Task<HashSet<string>> GetServerUserState();
    Task SendToLocalAll(InvocationRequest request, IReadOnlyList<string>? excludedConnectionIds = default);
    Task SendToLocalGroup(string group, InvocationRequest request, IReadOnlyList<string>? excludedConnectionIds = default);
    Task SendToLocalUser(string user, InvocationRequest request);
    Task SendToLocalConnections(InvocationRequest request, IReadOnlyList<string> connectionIds);
    Task AddConnectionToLocalGroup(string connection, string groupName);
    Task RemoveConnectionFromLocalGroup(string connection, string groupName);
}