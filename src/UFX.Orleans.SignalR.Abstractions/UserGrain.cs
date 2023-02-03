namespace UFX.Orleans.SignalR.Abstractions;

public interface IUserGrain : IGrainWithStringKey
{
    Task SendUserAsync(string methodName, object?[] args);
}