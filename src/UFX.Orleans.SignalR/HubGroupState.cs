using System.Collections.Concurrent;

namespace UFX.Orleans.SignalR;

[Serializable]
public class HubGroupState
{
    public ConcurrentDictionary<string, HashSet<string>> Servers { get; set; } = new();
    public List<string> GetServers(string item) =>
        Servers.TryGetValue(item, out var servers) ? servers.ToList() : new List<string>();
    public void AddServer(string server, string item)
    {
        var servers = Servers.GetOrAdd(item, _ => new());
        lock (servers) servers.Add(server);
    }
    public bool RemoveServer(string server)
    {
        var removed = false;
        foreach (var user in Servers.ToList())
        {
            lock (user.Value)
            {
                removed |= user.Value.Remove(server);
                if(!user.Value.Any()) Servers.TryRemove(user.Key, out _);
            }
        }
        return removed;
    }
    public bool RemoveServer(string server, string item)
    {
        if(!Servers.TryRemove(item, out var servers)) return false;
        lock (servers)
        {
            if(!servers.Remove(server)) return false;
            if(servers.Any()) return true;
            Servers.TryRemove(item, out _);
            return true;
        }
    }
}