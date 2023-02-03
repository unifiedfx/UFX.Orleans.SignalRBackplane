namespace UFX.Orleans.SignalRBackplane.Client;

public static class ExternalSignalrHubContextHelper
{
    public static string GetHubTypeName<T>() => typeof(T).FullName!.ToLower();
}