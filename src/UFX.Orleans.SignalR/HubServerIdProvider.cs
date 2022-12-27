using UFX.Orleans.SignalR.Abstractions;

namespace UFX.Orleans.SignalR;

public class HubServerIdProvider : IHubServerIdProvider
{
    private readonly string localServerId;
    public HubServerIdProvider(string? localServerId = default) => this.localServerId = localServerId ?? Guid.NewGuid().ToString();
    public string GetServerId(string hubName) => $"{hubName}_{localServerId}";
}