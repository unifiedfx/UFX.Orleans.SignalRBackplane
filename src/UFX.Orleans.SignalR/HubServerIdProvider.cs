using UFX.Orleans.SignalR.Abstractions;

namespace UFX.Orleans.SignalR;

public class HubServerIdProvider : IHubServerIdProvider
{
    private readonly string localServerId;
    public HubServerIdProvider(string? localServerId = default) => this.localServerId = localServerId ?? Guid.NewGuid().ToString();
    public string GetHubServerId(string hubName) => $"{hubName}_{localServerId}";
    public bool IsLocal(string hubServerId) => hubServerId.EndsWith($"_{localServerId}");
}