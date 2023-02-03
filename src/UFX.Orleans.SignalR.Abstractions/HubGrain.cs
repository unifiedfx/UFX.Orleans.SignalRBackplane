namespace UFX.Orleans.SignalR.Abstractions;

public interface IHubGrain : IGrainWithStringKey
{
    Task SendAllAsync(string methodName, object?[] args);
    Task SendAllExceptAsync(string methodName, object?[] args, IReadOnlyList<string> excludedConnectionIds);
}