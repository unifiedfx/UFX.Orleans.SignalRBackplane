namespace UFX.Orleans.SignalR.Abstractions;

public interface IServerObserver : IGrainObserver
{
    Task SendToLocalAll(InvocationRequest request, IReadOnlyList<string>? excludedConnectionIds = default);
    Task SendToLocalGroup(string group, InvocationRequest request, IReadOnlyList<string>? excludedConnectionIds = default);
    Task SendToLocalUser(string user, InvocationRequest request);
    Task SendToLocalConnections(InvocationRequest request, IReadOnlyList<string> connectionIds);
    Task AddConnectionToLocalGroup(string connection, string groupName);
    Task RemoveConnectionFromLocalGroup(string connection, string groupName);
}