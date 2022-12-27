namespace UFX.Orleans.SignalR.Abstractions;

public interface IHubServerIdProvider
{
    public string GetServerId(string hubName);
}
