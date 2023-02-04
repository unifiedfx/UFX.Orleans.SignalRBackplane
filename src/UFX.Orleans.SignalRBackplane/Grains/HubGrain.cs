using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using UFX.Orleans.SignalRBackplane.Abstractions;

namespace UFX.Orleans.SignalRBackplane.Grains;

internal class HubGrain : SignalrBaseGrain, IHubGrain
{
    public HubGrain(
        [PersistentState(Constants.StateName, Constants.StorageName)] IPersistentState<SubscriptionState> persistedSubs,
        IGrainContext grainContext,
        IReminderResolver reminderResolver,
        IOptions<SignalrOrleansOptions> options,
        ILogger<HubGrain> logger
    )
        : base(persistedSubs, grainContext, reminderResolver, options, logger)
    {
    }

    public Task SendAllAsync(string methodName, object?[] args) 
        => InformObserversAsync(observer => observer.SendAllAsync(methodName, args));

    public Task SendAllExceptAsync(string methodName, object?[] args, IReadOnlyList<string> excludedConnectionIds) 
        => InformObserversAsync(observer => observer.SendAllExceptAsync(methodName, args, excludedConnectionIds));
}