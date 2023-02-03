using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UFX.Orleans.SignalRBackplane.Abstractions;
using UFX.Orleans.SignalRBackplane.Grains;

namespace UFX.Orleans.SignalRBackplane;

internal partial class OrleansHubLifetimeManager<THub> : HubLifetimeManager<THub>, IHubLifetimeManagerGrainObserver where THub : Hub
{
    private static readonly string HubName = typeof(THub).FullName!.ToLower();

    private readonly IGrainFactory _grainFactory;
    private readonly DefaultHubLifetimeManager<THub> _hubManager;
    private readonly IHubGrain _hubGrain;

    private readonly ConcurrentDictionary<string, (string? UserIdentifier, string[] GroupNames)> _trackedConnections = new();

    public OrleansHubLifetimeManager(IGrainFactory grainFactory, ILogger<DefaultHubLifetimeManager<THub>> logger, IHostApplicationLifetime hostLifetime)
    {
        _grainFactory = grainFactory;
        _hubManager = new DefaultHubLifetimeManager<THub>(logger);

        _hubGrain = _grainFactory.GetGrain<IHubGrain>(HubName);

        hostLifetime.ApplicationStopping.Register(OnApplicationStopping);
    }

    public override async Task OnConnectedAsync(HubConnectionContext connection)
    {
        await EnsureObserverAsync();

        _trackedConnections.TryAdd(connection.ConnectionId, (connection.UserIdentifier, Array.Empty<string>()));

        await GetConnectionGrain(connection.ConnectionId)
            .AsReference<ISignalrGrain>()
            .SubscribeAsync(_observer!);

        if (connection.UserIdentifier is not null)
        {
            await GetUserGrain(connection.UserIdentifier)
                .AsReference<ISignalrGrain>()
                .SubscribeAsync(_observer!);
        }

        await _hubManager.OnConnectedAsync(connection);
    }

    public override async Task OnDisconnectedAsync(HubConnectionContext connection)
    {
        _trackedConnections.Remove(connection.ConnectionId, out var removedConnection);

        // If this was the last connection for the user on this hub, unsubscribe from the user grain
        if (connection.UserIdentifier is not null && _trackedConnections.All(conn => conn.Value.UserIdentifier != connection.UserIdentifier))
        {
            await GetUserGrain(connection.UserIdentifier)
                .AsReference<ISignalrGrain>()
                .UnsubscribeAsync(_observer!);
        }

        // If this was the last connection for this group on this hub, unsubscribe from the group grain 
        var groupUnsubTasks = removedConnection.GroupNames.Select(
            groupName =>
                _trackedConnections.All(conn => !conn.Value.GroupNames.Contains(groupName))
                    ? GetGroupGrain(groupName)
                        .AsReference<ISignalrGrain>()
                        .UnsubscribeAsync(_observer!)
                    : Task.CompletedTask
        );
        await Task.WhenAll(groupUnsubTasks);

        await GetConnectionGrain(connection.ConnectionId)
            .AsReference<ISignalrGrain>()
            .UnsubscribeAsync(_observer!);

        await _hubManager.OnDisconnectedAsync(connection);
    }

    public override async Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        var updated = false;
        var remainingAttempts = 5;

        do
        {
            if (_trackedConnections.TryGetValue(connectionId, out var existingEntry))
            {
                if (existingEntry.GroupNames.Contains(groupName))
                {
                    break;
                }

                updated = _trackedConnections.TryUpdate(
                    connectionId,
                    (existingEntry.UserIdentifier, existingEntry.GroupNames.Append(groupName).ToArray()),
                    existingEntry
                );
            }
        } while (!updated && remainingAttempts-- > 0);

        var group = GetGroupGrain(groupName)
            .AsReference<IGroupGrainInternal>();

        await group.SubscribeAsync(_observer!);

        await group.AddToGroupAsync(connectionId);
    }

    public override async Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        var updated = false;
        var remainingAttempts = 5;

        do
        {
            if (_trackedConnections.TryGetValue(connectionId, out var existingEntry))
            {
                updated = _trackedConnections.TryUpdate(
                    connectionId,
                    (existingEntry.UserIdentifier, existingEntry.GroupNames.Where(name => name != groupName).ToArray()),
                    existingEntry
                );
            }
        } while (!updated && remainingAttempts-- > 0);

        var groupGrain = GetGroupGrain(groupName)
            .AsReference<IGroupGrainInternal>();

        await groupGrain.RemoveFromGroupAsync(connectionId);

        // If this was the last connection for this group on this hub, unsubscribe from the group grain 
        if (_trackedConnections.All(conn => !conn.Value.GroupNames.Contains(groupName)))
        {
            await groupGrain.UnsubscribeAsync(_observer!);
        }
    }

    public override Task SendAllAsync(string methodName, object?[] args, CancellationToken cancellationToken = default)
        => _hubGrain.SendAllAsync(methodName, args);

    public override Task SendAllExceptAsync(string methodName, object?[] args, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default)
        => _hubGrain.SendAllExceptAsync(methodName, args, excludedConnectionIds);

    public override Task SendConnectionAsync(string connectionId, string methodName, object?[] args, CancellationToken cancellationToken = default)
        => GetConnectionGrain(connectionId)
            .SendConnectionAsync(methodName, args);

    public override Task SendConnectionsAsync(IReadOnlyList<string> connectionIds, string methodName, object?[] args, CancellationToken cancellationToken = default)
        => Task.WhenAll(connectionIds.Select(connectionId => SendConnectionAsync(connectionId, methodName, args, cancellationToken)));

    public override Task SendGroupAsync(string groupName, string methodName, object?[] args, CancellationToken cancellationToken = default)
        => GetGroupGrain(groupName)
            .SendGroupAsync(methodName, args);

    public override Task SendGroupExceptAsync(string groupName, string methodName, object?[] args, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default)
        => GetGroupGrain(groupName)
            .SendGroupExceptAsync(methodName, args, excludedConnectionIds);

    public override Task SendGroupsAsync(IReadOnlyList<string> groupNames, string methodName, object?[] args, CancellationToken cancellationToken = default)
        => Task.WhenAll(groupNames.Select(groupName => SendGroupAsync(groupName, methodName, args, cancellationToken)));

    public override Task SendUserAsync(string userId, string methodName, object?[] args, CancellationToken cancellationToken = default)
        => GetUserGrain(userId)
            .SendUserAsync(methodName, args);

    public override Task SendUsersAsync(IReadOnlyList<string> userIds, string methodName, object?[] args, CancellationToken cancellationToken = default)
        => Task.WhenAll(userIds.Select(userId => SendUserAsync(userId, methodName, args, cancellationToken)));

    private IConnectionGrain GetConnectionGrain(string connectionId) => _grainFactory.GetConnectionGrain(HubName, connectionId);
    private IGroupGrain GetGroupGrain(string groupName) => _grainFactory.GetGroupGrain(HubName, groupName);
    private IUserGrain GetUserGrain(string userId) => _grainFactory.GetUserGrain(HubName, userId);

    private void OnApplicationStopping()
    {
        if (_observer is not null)
        {
            try
            {
                _hubGrain
                    .AsReference<ISignalrGrain>()
                    .UnsubscribeAsync(_observer).GetAwaiter().GetResult();
            }
            catch
            {
                // We tried our best to cleanup gracefully, this will be cleaned up by the grain reminder later
            }
        }
    }
}