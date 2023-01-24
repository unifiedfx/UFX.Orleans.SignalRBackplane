using Microsoft.Extensions.Options;
using Orleans.Runtime;

namespace UFX.Orleans.SignalR.Grains;

internal interface ISignalrGrain : IGrainWithStringKey
{
    Task SubscribeAsync(IHubLifetimeManagerGrainObserver observer);
    Task UnsubscribeAsync(IHubLifetimeManagerGrainObserver observer);
}

internal abstract class SignalrBaseGrain : Grain<SubscriptionState>, ISignalrGrain, IRemindable
{
    private const string PingReminderName = nameof(PingReminderName);

    private readonly IPersistentState<SubscriptionState> _persistedSubs;
    private readonly TimeSpan _grainCleanupPeriod;
    private HashSet<IHubLifetimeManagerGrainObserver> _observers = new();

    protected SignalrBaseGrain(IPersistentState<SubscriptionState> persistedSubs, IOptions<SignalrOrleansOptions> options)
    {
        _persistedSubs = persistedSubs;
        _grainCleanupPeriod = options.Value.GrainCleanupPeriod;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await this.RegisterOrUpdateReminder(PingReminderName, _grainCleanupPeriod, _grainCleanupPeriod);

        _observers = _persistedSubs.State.Observers;

        await base.OnActivateAsync(cancellationToken);
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

            var grainReminder = await this.GetReminder(PingReminderName);
            if (grainReminder is not null)
            {
                await this.UnregisterReminder(grainReminder);
            }

            DeactivateOnIdle();
        }
        else if (countBeforeAction != countAfterAction)
        {
            _persistedSubs.State.Observers = _observers;
            await _persistedSubs.WriteStateAsync();
        }
    }
}

[GenerateSerializer]
internal class SubscriptionState
{
    [Id(0)]
    public HashSet<IHubLifetimeManagerGrainObserver> Observers { get; set; } = new();
}