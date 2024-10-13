using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Metadata;
using Orleans.Serialization;
using UFX.Orleans.SignalRBackplane.Grains;

namespace UFX.Orleans.SignalRBackplane;

public static class SiloBuilderExtensions
{
    public static ISiloBuilder AddSignalRBackplane(this ISiloBuilder siloBuilder, Action<SignalrOrleansOptions>? optionsAction = null)
    {
        var services = siloBuilder.Services;
        var options = new SignalrOrleansOptions();
        optionsAction?.Invoke(options);
        services.AddOptions<SignalrOrleansOptions>().Configure(o =>
        {
            o.GrainCleanupPeriod = options.GrainCleanupPeriod;
            o.UseFullyQualifiedGrainTypes = options.UseFullyQualifiedGrainTypes;
            o.JsonSerializerFallback = options.JsonSerializerFallback;
        });
        if (options.JsonSerializerFallback)
        {
            services.AddSerializer(serializerBuilder =>
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                var types = new HashSet<Type>(assemblies.
                    SelectMany(a => a.GetTypes()).
                    Where(t => t.CustomAttributes.Any(a =>a.AttributeType == typeof(GenerateSerializerAttribute))));
                serializerBuilder.AddJsonSerializer(type => !types.Contains(type));
            });
        }
        services
            .AddSingleton<IGrainTypeProvider, GrainTypeProvider>()
            .AddSingleton<IGrainInterfaceTypeProvider, GrainInterfaceTypeProvider>()
            .AddSingleton(typeof(HubLifetimeManager<>), typeof(OrleansHubLifetimeManager<>))
            .AddSingleton<IReminderResolver, ReminderResolver>();

        siloBuilder.AddReminders();

        return siloBuilder;
    }
}