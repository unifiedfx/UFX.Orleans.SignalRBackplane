using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;
using UFX.Orleans.SignalR.Abstractions;

namespace UFX.Orleans.SignalR;

public static class SiloHostBuilderExtensions
{
    public static ISiloBuilder UseSignalR(this ISiloBuilder builder,
        Action<SignalrOrleansConfig>? configure = null)
    {
        var cfg = new SignalrOrleansConfig();
        configure?.Invoke(cfg);

        builder.Services.AddSerializer(serializerBuilder =>
        {
            serializerBuilder.AddNewtonsoftJsonSerializer(isSupported: cfg.NewtonsoftJsonSerializer);
        });
        try
        {
            builder.AddMemoryGrainStorageAsDefault();
        }
        catch
        {
            /** Grain storage provider was already added. Do nothing. **/
        }
        if(string.IsNullOrWhiteSpace(cfg.ServerId))
            builder.Services.AddSingleton<IHubServerIdProvider, HubServerIdProvider>();
        else
            builder.Services.AddSingleton<IHubServerIdProvider>(new HubServerIdProvider(cfg.ServerId));
        builder.Services.AddSingleton(typeof(HubLifetimeManager<>), typeof(OrleansHubLifetimeManager<>));
        return builder;
    }
}

public class SignalrOrleansConfig
{
    public Func<Type, bool> NewtonsoftJsonSerializer { get; set; } = _ => true;
    public string? ServerId { get; set; }
}
