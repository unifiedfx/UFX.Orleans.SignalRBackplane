namespace UFX.Orleans.SignalRBackplane.Client;

public interface IExternalSignalrHubContextFactory
{
    IExternalSignalrHubContext CreateHubContext(string hubName);
    IExternalSignalrHubContext CreateHubContext<THub>();
}

public class ExternalSignalrHubContextFactory : IExternalSignalrHubContextFactory
{
    private readonly Dictionary<string, ExternalSignalrHubContext> _contexts = new(StringComparer.Ordinal);
    private readonly object _sync = new();

    private readonly IClusterClient _clusterClient;

    public ExternalSignalrHubContextFactory(IClusterClient clusterClient) 
        => _clusterClient = clusterClient;

    public IExternalSignalrHubContext CreateHubContext(string hubName)
    {
        lock (_sync)
        {
            if (!_contexts.TryGetValue(hubName, out var hubContext))
            {
                hubContext = new ExternalSignalrHubContext(_clusterClient, hubName);
                _contexts[hubName] = hubContext;
            }

            return hubContext;
        }
    }

    public IExternalSignalrHubContext CreateHubContext<THub>() 
        => CreateHubContext(ExternalSignalrHubContextHelper.GetHubTypeName<THub>());
}