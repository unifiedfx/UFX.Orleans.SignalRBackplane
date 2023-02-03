namespace UFX.Orleans.SignalRBackplane.Abstractions;

public interface IUserGrain : IGrainWithStringKey
{
    Task SendUserAsync(string methodName, object?[] args);
}