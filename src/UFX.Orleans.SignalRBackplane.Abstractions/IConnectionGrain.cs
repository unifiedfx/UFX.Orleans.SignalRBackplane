namespace UFX.Orleans.SignalRBackplane.Abstractions;

public interface IConnectionGrain : IGrainWithStringKey
{
    Task SendConnectionAsync(string methodName, object?[] args);

    /// <summary>
    /// Actively pings all registered observers, removes any that fail to respond, then returns
    /// <see langword="true"/> if at least one reachable observer remains, or <see langword="false"/>
    /// if all observers were defunct (or there were none to begin with).
    /// When <see langword="false"/> is returned the grain also deactivates itself.
    /// </summary>
    /// <remarks>
    /// Because this method contacts remote observer references it has similar cost to a regular
    /// grain-to-grain call for each registered observer. Avoid calling it in tight loops.
    /// </remarks>
    Task<bool> HasObserversAsync();
}
