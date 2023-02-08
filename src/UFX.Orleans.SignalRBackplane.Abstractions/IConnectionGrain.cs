namespace UFX.Orleans.SignalRBackplane.Abstractions;

public interface IConnectionGrain : IGrainWithStringKey
{
    Task SendConnectionAsync(string methodName, object?[] args);
}