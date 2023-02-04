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
    private readonly IGrainFactory _grainFactory;

    public GroupGrain(
        [PersistentState(Constants.StateName, Constants.StorageName)] IPersistentState<SubscriptionState> persistedSubs,
        IGrainContext grainContext,
        IReminderResolver reminderResolver,
        IOptions<SignalrOrleansOptions> options,
        ILogger<GroupGrain> logger,
        IGrainFactory grainFactory
    )
        : base(persistedSubs, grainContext, reminderResolver, options, logger)
    {
        _grainFactory = grainFactory;
    }

    public Task AddToGroupAsync(string connectionId) 
        => _grainFactory
            .GetConnectionGrain(HubName, connectionId)
            .AsReference<IConnectionGrainInternal>()
            .AddToGroupAsync(EntityId);

    public Task RemoveFromGroupAsync(string connectionId)
        => _grainFactory
            .GetConnectionGrain(HubName, connectionId)
            .AsReference<IConnectionGrainInternal>()
            .RemoveFromGroupAsync(EntityId);

    public Task SendGroupAsync(string methodName, object?[] args) 
        => InformObserversAsync(observer => observer.SendGroupAsync(EntityId, methodName, args));

    public Task SendGroupExceptAsync(string methodName, object?[] args, IReadOnlyList<string> excludedConnectionIds) 
        => InformObserversAsync(observer => observer.SendGroupExceptAsync(EntityId, methodName, args, excludedConnectionIds));
}