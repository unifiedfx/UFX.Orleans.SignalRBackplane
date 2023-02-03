using UFX.Orleans.SignalRBackplane.Abstractions;

namespace UFX.Orleans.SignalRBackplane.Client;

/// <summary>
/// A generic interface for interacting with SignalR connections on a remote Orleans silo cluster where the hub name is derived from the specified <typeparamref name="THub"/> type name.
/// Generally used to enable activation of a named <see cref="IExternalSignalrHubContext"/> from dependency injection.
/// </summary>
/// <typeparam name="THub">The type whose name is used for the remote hub.</typeparam>
public interface IExternalSignalrHubContext<out THub> : IExternalSignalrHubContext
{
}

/// <summary>
/// Represents a type used for interacting with SignalR connections on a remote Orleans silo cluster.
/// </summary>
public interface IExternalSignalrHubContext
{
    Task SendAllAsync(string methodName, object?[] args);
    Task SendAllExceptAsync(string methodName, object?[] args, IReadOnlyList<string> excludedConnectionIds);
    Task SendConnectionAsync(string connectionId, string methodName, object?[] args);
    Task SendConnectionsAsync(IReadOnlyList<string> connectionIds, string methodName, object?[] args);
    Task SendGroupAsync(string groupName, string methodName, object?[] args);
    Task SendGroupExceptAsync(string groupName, string methodName, object?[] args, IReadOnlyList<string> excludedConnectionIds);
    Task SendGroupsAsync(IReadOnlyList<string> groupNames, string methodName, object?[] args);
    Task SendUserAsync(string userId, string methodName, object?[] args);
    Task SendUsersAsync(IReadOnlyList<string> userIds, string methodName, object?[] args);
}

public class ExternalSignalrHubContext<THub> : IExternalSignalrHubContext<THub>
{
    private readonly IExternalSignalrHubContext _hubContext;

    public ExternalSignalrHubContext(IExternalSignalrHubContextFactory factory)
        => _hubContext = factory.CreateHubContext<THub>();

    public Task SendAllAsync(string methodName, object?[] args) 
        => _hubContext.SendAllAsync(methodName, args);

    public Task SendAllExceptAsync(string methodName, object?[] args, IReadOnlyList<string> excludedConnectionIds) 
        => _hubContext.SendAllExceptAsync(methodName, args, excludedConnectionIds);

    public Task SendConnectionAsync(string connectionId, string methodName, object?[] args) 
        => _hubContext.SendConnectionAsync(connectionId, methodName, args);

    public Task SendConnectionsAsync(IReadOnlyList<string> connectionIds, string methodName, object?[] args) 
        => _hubContext.SendConnectionsAsync(connectionIds, methodName, args);

    public Task SendGroupAsync(string groupName, string methodName, object?[] args) 
        => _hubContext.SendGroupAsync(groupName, methodName, args);

    public Task SendGroupExceptAsync(string groupName, string methodName, object?[] args, IReadOnlyList<string> excludedConnectionIds) 
        => _hubContext.SendGroupExceptAsync(groupName, methodName, args, excludedConnectionIds);

    public Task SendGroupsAsync(IReadOnlyList<string> groupNames, string methodName, object?[] args) 
        => _hubContext.SendGroupsAsync(groupNames, methodName, args);

    public Task SendUserAsync(string userId, string methodName, object?[] args) 
        => _hubContext.SendUserAsync(userId, methodName, args);

    public Task SendUsersAsync(IReadOnlyList<string> userIds, string methodName, object?[] args) 
        => _hubContext.SendUsersAsync(userIds, methodName, args);
}

public class ExternalSignalrHubContext : IExternalSignalrHubContext
{
    private readonly string _hubName;

    private readonly IClusterClient _clusterClient;
    private readonly IHubGrain _hubGrain;

    public ExternalSignalrHubContext(IClusterClient clusterClient, string hubName)
    {
        _clusterClient = clusterClient;
        _hubName = hubName;
        _hubGrain = clusterClient.GetGrain<IHubGrain>(_hubName);
    }

    public Task SendAllAsync(string methodName, object?[] args)
        => _hubGrain.SendAllAsync(methodName, args);

    public Task SendAllExceptAsync(string methodName, object?[] args, IReadOnlyList<string> excludedConnectionIds)
        => _hubGrain.SendAllExceptAsync(methodName, args, excludedConnectionIds);

    public Task SendConnectionAsync(string connectionId, string methodName, object?[] args)
        => GetConnectionGrain(connectionId)
            .SendConnectionAsync(methodName, args);

    public Task SendConnectionsAsync(IReadOnlyList<string> connectionIds, string methodName, object?[] args)
        => Task.WhenAll(connectionIds.Select(connectionId => SendConnectionAsync(connectionId, methodName, args)));

    public Task SendGroupAsync(string groupName, string methodName, object?[] args)
        => GetGroupGrain(groupName)
            .SendGroupAsync(methodName, args);

    public Task SendGroupExceptAsync(string groupName, string methodName, object?[] args, IReadOnlyList<string> excludedConnectionIds)
        => GetGroupGrain(groupName)
            .SendGroupExceptAsync(methodName, args, excludedConnectionIds);

    public Task SendGroupsAsync(IReadOnlyList<string> groupNames, string methodName, object?[] args)
        => Task.WhenAll(groupNames.Select(groupName => SendGroupAsync(groupName, methodName, args)));

    public Task SendUserAsync(string userId, string methodName, object?[] args)
        => GetUserGrain(userId)
            .SendUserAsync(methodName, args);

    public Task SendUsersAsync(IReadOnlyList<string> userIds, string methodName, object?[] args)
        => Task.WhenAll(userIds.Select(userId => SendUserAsync(userId, methodName, args)));

    private IConnectionGrain GetConnectionGrain(string connectionId)
        => _clusterClient.GetGrain<IConnectionGrain>($"{_hubName}/{connectionId}");

    private IGroupGrain GetGroupGrain(string groupName)
        => _clusterClient.GetGrain<IGroupGrain>($"{_hubName}/{groupName}");

    private IUserGrain GetUserGrain(string userId)
        => _clusterClient.GetGrain<IUserGrain>($"{_hubName}/{userId}");
}