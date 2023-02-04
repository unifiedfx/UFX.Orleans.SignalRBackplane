using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using UFX.Orleans.SignalRBackplane.Abstractions;

namespace UFX.Orleans.SignalRBackplane.Grains;

internal interface IConnectionGrainInternal : ISignalrGrain
{
    Task AddToGroupAsync(string groupName);
    Task RemoveFromGroupAsync(string groupName);
}

internal class ConnectionGrain : SignalrBaseGrain, IConnectionGrain, IConnectionGrainInternal
{
    public ConnectionGrain(
        [PersistentState(Constants.StateName, Constants.StorageName)] IPersistentState<SubscriptionState> persistedSubs,
        IGrainContext grainContext,
        IReminderResolver reminderResolver,
        IOptions<SignalrOrleansOptions> options,
        ILogger<ConnectionGrain> logger
    )
        : base(persistedSubs, grainContext, reminderResolver, options, logger)
    {
    }

    public Task SendConnectionAsync(string methodName, object?[] args) 
        => InformObserversAsync(observer => observer.SendConnectionAsync(EntityId, methodName, args));

    public Task AddToGroupAsync(string groupName)
        => InformObserversAsync(observer => observer.AddToGroupAsync(EntityId, groupName));

    public Task RemoveFromGroupAsync(string groupName)
        => InformObserversAsync(observer => observer.RemoveFromGroupAsync(EntityId, groupName));
}