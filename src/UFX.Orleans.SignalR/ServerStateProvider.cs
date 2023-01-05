using System.Collections.Concurrent;
using UFX.Orleans.SignalR.Abstractions;

namespace UFX.Orleans.SignalR;

public class ServerStateProvider : IServerStateProvider
{
    private readonly ConcurrentDictionary<string, ServerStateStore> store = new ();
    public ServerStateStore GetStore(string name) => store.GetOrAdd(name, new ServerStateStore());
}