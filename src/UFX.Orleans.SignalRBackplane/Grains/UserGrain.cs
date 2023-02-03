using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using UFX.Orleans.SignalRBackplane.Abstractions;

namespace UFX.Orleans.SignalRBackplane.Grains;

internal class UserGrain : SignalrBaseGrain, IUserGrain
{
    public UserGrain(
        [PersistentState(Constants.StateName, Constants.StorageName)] IPersistentState<SubscriptionState> persistedSubs,
        IOptions<SignalrOrleansOptions> options,
        ILogger<UserGrain> logger)
        : base(persistedSubs, options, logger)
    {
    }

    public Task SendUserAsync(string methodName, object?[] args) 
        => InformObserversAsync(observer => observer.SendUserAsync(EntityId, methodName, args));
}