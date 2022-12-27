namespace UFX.Orleans.SignalR.Abstractions;

public interface IServerObserver : IGrainObserver
{
    Task SendToAll(InvocationRequest request, IReadOnlyList<string>? excludedConnectionIds = default);
    Task SendToGroup(string group, InvocationRequest request, IReadOnlyList<string>? excludedConnectionIds = default);
    Task SendToUser(string user, InvocationRequest request, IReadOnlyList<string>? excludedConnectionIds = default);
    Task SendToConnections(InvocationRequest request, IReadOnlyList<string>? connectionIds);
    Task AddConnectionToGroup(string connection, string group);
    Task RemoveConnectionFromGroup(string connection, string group);
}