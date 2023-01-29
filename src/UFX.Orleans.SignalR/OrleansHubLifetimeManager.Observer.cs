namespace UFX.Orleans.SignalR;

internal interface IHubLifetimeManagerGrainObserver : IGrainObserver
{
    Task SendAllAsync(string methodName, object?[] args);
    Task SendAllExceptAsync(string methodName, object?[] args, IReadOnlyList<string> excludedConnectionIds);

    Task SendConnectionAsync(string connectionId, string methodName, object?[] args);

    Task AddToGroupAsync(string connectionId, string groupName);
    Task RemoveFromGroupAsync(string connectionId, string groupName);
    Task SendGroupAsync(string groupName, string methodName, object?[] args);
    Task SendGroupExceptAsync(string groupName, string methodName, object?[] args, IReadOnlyList<string> excludedConnectionIds);
    
    Task SendUserAsync(string userId, string methodName, object?[] args);
    Task PingAsync();
}

internal partial class OrleansHubLifetimeManager<THub>
{
    private IHubLifetimeManagerGrainObserver? _observer;

    private readonly SemaphoreSlim _initialLock = new(1, 1);

    Task IHubLifetimeManagerGrainObserver.SendAllAsync(string methodName, object?[] args) 
        => _hubManager.SendAllAsync(methodName, args);

    Task IHubLifetimeManagerGrainObserver.SendAllExceptAsync(string methodName, object?[] args, IReadOnlyList<string> excludedConnectionIds) 
        => _hubManager.SendAllExceptAsync(methodName, args, excludedConnectionIds);

    Task IHubLifetimeManagerGrainObserver.SendConnectionAsync(string connectionId, string methodName, object?[] args) 
        => _hubManager.SendConnectionAsync(connectionId, methodName, args);

    Task IHubLifetimeManagerGrainObserver.AddToGroupAsync(string connectionId, string groupName) 
        => _hubManager.AddToGroupAsync(connectionId, groupName);

    Task IHubLifetimeManagerGrainObserver.RemoveFromGroupAsync(string connectionId, string groupName) 
        => _hubManager.RemoveFromGroupAsync(connectionId, groupName);

    Task IHubLifetimeManagerGrainObserver.SendGroupAsync(string groupName, string methodName, object?[] args) 
        => _hubManager.SendGroupAsync(groupName, methodName, args);

    Task IHubLifetimeManagerGrainObserver.SendGroupExceptAsync(string groupName, string methodName, object?[] args, IReadOnlyList<string> excludedConnectionIds) 
        => _hubManager.SendGroupExceptAsync(groupName, methodName, args, excludedConnectionIds);

    Task IHubLifetimeManagerGrainObserver.SendUserAsync(string userId, string methodName, object?[] args) 
        => _hubManager.SendUserAsync(userId, methodName, args);

    Task IHubLifetimeManagerGrainObserver.PingAsync()
        => Task.CompletedTask;

    private async Task EnsureObserverAsync()
    {
        if (_observer is null)
        {
            await _initialLock.WaitAsync();

            try
            {
                if (_observer is not null)
                {
                    // Somebody else set the observer
                    return;
                }

                _observer = _grainFactory.CreateObjectReference<IHubLifetimeManagerGrainObserver>(this);

                await _hubGrain.SubscribeAsync(_observer);
            }
            finally
            {
                _initialLock.Release();
            }
        }
    }
}