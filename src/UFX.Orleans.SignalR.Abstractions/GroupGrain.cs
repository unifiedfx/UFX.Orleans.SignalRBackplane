namespace UFX.Orleans.SignalR.Abstractions;

public interface IGroupGrain : IGrainWithStringKey
{
    Task SendGroupAsync(string methodName, object?[] args);
    Task SendGroupExceptAsync(string methodName, object?[] args, IReadOnlyList<string> excludedConnectionIds);
}