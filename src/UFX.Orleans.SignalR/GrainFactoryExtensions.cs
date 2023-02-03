using UFX.Orleans.SignalR.Abstractions;

namespace UFX.Orleans.SignalR;

internal static class GrainFactoryExtensions
{
    internal static IConnectionGrain GetConnectionGrain(this IGrainFactory grainFactory, string hubName, string connectionId) 
        => grainFactory.GetGrain<IConnectionGrain>($"{hubName}/{connectionId}");
        
    internal static IGroupGrain GetGroupGrain(this IGrainFactory grainFactory, string hubName, string groupName) 
        => grainFactory.GetGrain<IGroupGrain>($"{hubName}/{groupName}");
        
    internal static IUserGrain GetUserGrain(this IGrainFactory grainFactory, string hubName, string userId) 
        => grainFactory.GetGrain<IUserGrain>($"{hubName}/{userId}");
}