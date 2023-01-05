namespace UFX.Orleans.SignalR.Abstractions;

public interface IHubServerIdProvider
{
    string GetHubServerId(string hubName);
    bool IsLocal(string hubServerId);
}
