namespace UFX.Orleans.SignalR;

[Serializable]
public class HubAllState
{
    public HashSet<string> Servers = new();
    public List<string> GetServers()
    {
        lock (Servers) return Servers.ToList();
    }
    public bool AddServer(string server)
    {
        lock (Servers) return Servers.Add(server);
    }
    public bool RemoveServer(string server)
    {
        lock (Servers) return Servers.Remove(server);
    }
}