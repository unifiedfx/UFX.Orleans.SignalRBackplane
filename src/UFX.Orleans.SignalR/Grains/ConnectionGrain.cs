using Microsoft.Extensions.Options;
using Orleans.Runtime;

namespace UFX.Orleans.SignalR.Grains;

internal interface IConnectionGrain : ISignalrGrain
{
    Task SendConnectionAsync(string methodName, object?[] args);
    Task AddToGroupAsync(string groupName);
    Task RemoveFromGroupAsync(string groupName);
}

internal class ConnectionGrain : SignalrBaseGrain, IConnectionGrain
{
    public ConnectionGrain(
        [PersistentState(Constants.StateName, Constants.StorageName)] IPersistentState<SubscriptionState> persistedSubs,
        IOptions<SignalrOrleansOptions> options
    )
        : base(persistedSubs, options)
    {
    }

    public Task SendConnectionAsync(string methodName, object?[] args)
        => InformObserversAsync(observer => observer.SendConnectionAsync(this.GetPrimaryKeyString(), methodName, args));

    public Task AddToGroupAsync(string groupName)
        => InformObserversAsync(observer => observer.AddToGroupAsync(this.GetPrimaryKeyString(), groupName));

    public Task RemoveFromGroupAsync(string groupName)
        => InformObserversAsync(observer => observer.RemoveFromGroupAsync(this.GetPrimaryKeyString(), groupName));
}