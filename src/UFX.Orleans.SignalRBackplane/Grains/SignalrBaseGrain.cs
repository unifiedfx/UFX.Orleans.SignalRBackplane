using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;

namespace UFX.Orleans.SignalRBackplane.Grains;

internal interface ISignalrGrain : IGrainWithStringKey
{
    Task SubscribeAsync(IHubLifetimeManagerGrainObserver observer);
    Task UnsubscribeAsync(IHubLifetimeManagerGrainObserver observer);
}

internal abstract class SignalrBaseGrain : IGrainBase, ISignalrGrain, IRemindable, IIncomingGrainCallFilter
{
    public IGrainContext GrainContext { get; }

    /// <summary>
    /// The name of the hub type this grain is connected to.
    /// </summary>
    protected readonly string HubName;

    /// <summary>
    /// The EntityId of the grain. This is the connectionId for a connection grain, the userId for a user grain, the group name for a group grain and the hub name for a hub grain.
    /// </summary>
    protected readonly string EntityId;
    
    private const string PingReminderName = nameof(PingReminderName);

    private readonly IPersistentState<SubscriptionState> _persistedSubs;
    private readonly IReminderResolver _reminderResolver;
    private readonly ILogger<SignalrBaseGrain> _logger;
    private readonly TimeSpan _grainCleanupPeriod;
    
    private HashSet<IHubLifetimeManagerGrainObserver> _observers = new();

    protected SignalrBaseGrain(
        IPersistentState<SubscriptionState> persistedSubs, 
        IGrainContext grainContext,
        IReminderResolver reminderResolver,
        IOptions<SignalrOrleansOptions> options,
        ILogger<SignalrBaseGrain> logger)
    {
        GrainContext = grainContext;
        _persistedSubs = persistedSubs;
        _reminderResolver = reminderResolver;
        _logger = logger;
        _grainCleanupPeriod = options.Value.GrainCleanupPeriod;

        // A grain key is in the form of "HubName/EntityId" or "HubName" for hub grains
        // For example a connection to ChatHub with connectionId 123 will have a grain key of "chathub/123"
        // A HubGrain does not have an EntityId and will therefore have both HubName and EntityId set to chathub
        var grainKeyParts = this.GetPrimaryKeyString().Split("/", 2);
        HubName = grainKeyParts[0];
        EntityId = grainKeyParts.Length == 2 ? grainKeyParts[1] : HubName;
    }

    public async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await this.RegisterOrUpdateReminder(PingReminderName, _grainCleanupPeriod, _grainCleanupPeriod);

        _observers = _persistedSubs.State.Observers;
    }

    public Task SubscribeAsync(IHubLifetimeManagerGrainObserver observer) 
        => RunActionAndUpdateStateAsync(() => _observers.Add(observer));

    public Task UnsubscribeAsync(IHubLifetimeManagerGrainObserver observer)
        => RunActionAndUpdateStateAsync(() => _observers.Remove(observer));

    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        if (reminderName == PingReminderName)
        {
            await RunActionAndUpdateStateAsync(() => NotifyAllObserversAsync(observer => observer.PingAsync()));
        }
    }

    protected Task InformObserversAsync(Func<IHubLifetimeManagerGrainObserver, Task> notificationCallback) 
        => RunActionAndUpdateStateAsync(() => NotifyAllObserversAsync(notificationCallback));

    async Task NotifyAllObserversAsync(Func<IHubLifetimeManagerGrainObserver, Task> notification)
    {
        var notifyTasks = _observers.Select(NotifyObserver);

        await Task.WhenAll(notifyTasks);

        async Task NotifyObserver(IHubLifetimeManagerGrainObserver observer)
        {
            try
            {
                await notification(observer);
            }
            catch (Exception)
            {
                // Failing observers are considered defunct and will be removed
                _observers.Remove(observer);
            }
        }
    }

    Task RunActionAndUpdateStateAsync(Action action)
    {
        var countBeforeAction = _observers.Count;
        action();
        var countAfterAction = _observers.Count;

        return UpdateStateAsync(countBeforeAction, countAfterAction);
    }

    async Task RunActionAndUpdateStateAsync(Func<Task> func)
    {
        var countBeforeAction = _observers.Count;
        await func();
        var countAfterAction = _observers.Count;

        await UpdateStateAsync(countBeforeAction, countAfterAction);
    }

    async Task UpdateStateAsync(int countBeforeAction, int countAfterAction)
    {
        if (countAfterAction == 0)
        {
            await _persistedSubs.ClearStateAsync();

            var grainReminder = await _reminderResolver.GetReminder(this, PingReminderName);

            if (grainReminder is not null)
            {
                await this.UnregisterReminder(grainReminder);
            }

            this.DeactivateOnIdle();
        }
        else if (countBeforeAction != countAfterAction)
        {
            _persistedSubs.State.Observers = _observers;
            await _persistedSubs.WriteStateAsync();
        }
    }

    Task IIncomingGrainCallFilter.Invoke(IIncomingGrainCallContext context)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Method {MethodName} called on grain {Grain}", context.ImplementationMethod.Name, context.TargetContext.Address);
        }

        return context.Invoke();
    }
}

[GenerateSerializer]
internal class SubscriptionState
{
    [Id(0)]
    public HashSet<IHubLifetimeManagerGrainObserver> Observers { get; set; } = new();
}