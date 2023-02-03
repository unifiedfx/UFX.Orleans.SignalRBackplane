namespace UFX.Orleans.SignalR.Abstractions;

public interface IConnectionGrain : IGrainWithStringKey
{
    Task SendConnectionAsync(string methodName, object?[] args);
}