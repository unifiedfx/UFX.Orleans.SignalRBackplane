using Orleans.Serialization.Buffers.Adaptors;

namespace UFX.Orleans.SignalR.Abstractions;

public interface IServerStateProvider
{
    ServerStateStore GetStore(string name);
} 