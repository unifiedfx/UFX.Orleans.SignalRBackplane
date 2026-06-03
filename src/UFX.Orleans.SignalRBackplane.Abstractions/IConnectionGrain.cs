namespace UFX.Orleans.SignalRBackplane.Abstractions;

public interface IConnectionGrain : IGrainWithStringKey
{
    Task SendConnectionAsync(string methodName, object?[] args);

    /// <summary>
    /// Returns <see langword="true"/> if this grain has at least one live observer (i.e. the
    /// underlying SignalR connection is still active on some silo). Returns <see langword="false"/>
    /// when the connection has disconnected or its silo has crashed and the observer set is empty.
    /// </summary>
    Task<bool> HasObserversAsync();
}
