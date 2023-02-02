namespace UFX.Orleans.SignalR.Client;

public static class ExternalSignalrHubContextHelper
{
    public static string GetHubTypeName<T>() => typeof(T).FullName!.ToLower();
}