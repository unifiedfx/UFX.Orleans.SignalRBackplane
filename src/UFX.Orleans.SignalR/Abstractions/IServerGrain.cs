
namespace UFX.Orleans.SignalR.Abstractions;

public interface IServerGrain : IServerObserver, IGrainWithStringKey
{
    Task<bool> CheckSubscriber();
    Task Subscribe(IServerObserver subscriber);
}