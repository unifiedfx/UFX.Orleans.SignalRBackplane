using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using UFX.Orleans.SignalRBackplane.Abstractions;

namespace UFX.Orleans.SignalRBackplane.Grains;

internal interface IGroupGrainInternal : ISignalrGrain
{
    Task AddToGroupAsync(string connectionId);
    Task RemoveFromGroupAsync(string connectionId);
}

internal class GroupGrain : SignalrBaseGrain, IGroupGrain, IGroupGrainInternal
{
    public GroupGrain(
        [PersistentState(Constants.StateName, Constants.StorageName)] IPersistentState<SubscriptionState> persistedSubs,
        IOptions<SignalrOrleansOptions> options,
        ILogger<GroupGrain> logger
    )
        : base(persistedSubs, options, logger)
    {
    }

    public Task AddToGroupAsync(string connectionId) 
        => GrainFactory
            .GetConnectionGrain(HubName, connectionId)
            .AsReference<IConnectionGrainInternal>()
            .AddToGroupAsync(EntityId);

    public Task RemoveFromGroupAsync(string connectionId)
        => GrainFactory
            .GetConnectionGrain(HubName, connectionId)
            .AsReference<IConnectionGrainInternal>()
            .RemoveFromGroupAsync(EntityId);

    public Task SendGroupAsync(string methodName, object?[] args) 
        => InformObserversAsync(observer => observer.SendGroupAsync(EntityId, methodName, args));

    public Task SendGroupExceptAsync(string methodName, object?[] args, IReadOnlyList<string> excludedConnectionIds) 
        => InformObserversAsync(observer => observer.SendGroupExceptAsync(EntityId, methodName, args, excludedConnectionIds));
}