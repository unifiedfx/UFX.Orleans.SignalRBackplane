﻿using Microsoft.Extensions.Logging;
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
        IOptions<SignalrOrleansOptions> options,
        ILogger<ConnectionGrain> logger
    )
        : base(persistedSubs, options, logger)
    {
    }

    public Task SendConnectionAsync(string methodName, object?[] args) 
        => InformObserversAsync(observer => observer.SendConnectionAsync(EntityId, methodName, args));

    public Task AddToGroupAsync(string groupName)
        => InformObserversAsync(observer => observer.AddToGroupAsync(EntityId, groupName));

    public Task RemoveFromGroupAsync(string groupName)
        => InformObserversAsync(observer => observer.RemoveFromGroupAsync(EntityId, groupName));
}