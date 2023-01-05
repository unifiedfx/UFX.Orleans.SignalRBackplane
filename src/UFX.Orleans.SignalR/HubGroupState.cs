using System.Collections.Concurrent;

namespace UFX.Orleans.SignalR;

// [Serializable]
public class HubGroupState
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> locks = new ();
    private readonly ConcurrentDictionary<string, HashSet<string>> state = new();
    public IReadOnlyList<string> GetServers(string item) =>
        state.TryGetValue(item, out var servers) ? servers.ToList() : new List<string>();

    public async ValueTask AddServerAsync(string server, string item)
    {
        var @lock = GetLock(server);
        await @lock.WaitAsync();
        var servers = state.GetOrAdd(item, _ => new());
        servers.Add(server);
        @lock.Release();
    }
    public async ValueTask RemoveServerAsync(string server)
    {
        var @lock = GetLock(server);
        await @lock.WaitAsync();
        foreach (var item in state.ToList())
        {
            item.Value.Remove(server);
            if(!item.Value.Any()) state.TryRemove(item.Key, out _);
        }
        @lock.Release();
    }
    // public async ValueTask<HashSet<string>> GetServerItems(string server)
    // {
    //     var @lock = GetLock(server);
    //     var result = new HashSet<string>();
    //     await @lock.WaitAsync();
    //     foreach (var item in state.ToList())
    //     {
    //         if(!item.Value.Contains(server)) continue;
    //         result.Add(item.Key);
    //     }
    //     @lock.Release();
    //     return result;
    // }
    public async ValueTask RemoveServerAsync(string server, string item)
    {
        var @lock = GetLock(server);
        await @lock.WaitAsync();
        try
        {
            if(!state.TryRemove(item, out var servers)) return;
            if(!servers.Remove(server)) return;
            if(servers.Any()) return;
            state.TryRemove(item, out _);
        }
        finally
        {
            @lock.Release();
        }
    }
    
    public async ValueTask SetStateAsync(string server, HashSet<string> items)
    {
        var @lock = GetLock(server);
        await @lock.WaitAsync();
        foreach (var item in items) state.GetOrAdd(item, _ => new()).Add(server);
        @lock.Release();
    }
    private SemaphoreSlim GetLock(string server) => locks.GetOrAdd(server, _ => new SemaphoreSlim(1,1));
}