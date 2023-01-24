using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;

namespace UFX.Orleans.SignalR.Grains;

internal interface IUserGrain : ISignalrGrain
{
    Task SendUserAsync(string methodName, object?[] args);
}

internal class UserGrain : SignalrBaseGrain, IUserGrain
{
    public UserGrain(
        [PersistentState(Constants.StateName, Constants.StorageName)] IPersistentState<SubscriptionState> persistedSubs,
        IOptions<SignalrOrleansOptions> options)
        : base(persistedSubs, options)
    {
    }

    public Task SendUserAsync(string methodName, object?[] args) 
        => InformObserversAsync(observer => observer.SendUserAsync(this.GetPrimaryKeyString(), methodName, args));
}