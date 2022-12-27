namespace UFX.Orleans.SignalR.Abstractions;

public interface ISignalRGroupFeature
{
    HashSet<string> Groups { get; }
}