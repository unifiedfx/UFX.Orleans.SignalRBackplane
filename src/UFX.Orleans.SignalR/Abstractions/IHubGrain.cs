namespace UFX.Orleans.SignalR.Abstractions;

public interface IHubGrain : IGrainWithStringKey
{
    ValueTask<IReadOnlyList<string>> GetAllServers();
    Task AddServerToAll(string server);
}