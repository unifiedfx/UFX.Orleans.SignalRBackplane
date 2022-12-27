using UFX.Orleans.SignalR.Abstractions;

namespace UFX.Orleans.SignalR;

public sealed class SignalRGroupFeature : ISignalRGroupFeature
{
    public HashSet<string> Groups { get; } = new (StringComparer.OrdinalIgnoreCase);
}