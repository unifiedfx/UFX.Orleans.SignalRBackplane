using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace UFX.Orleans.SignalRBackplane;

public static class SiloBuilderExtensions
{
    public static ISiloBuilder AddSignalRBackplane(this ISiloBuilder siloBuilder, Action<SignalrOrleansOptions>? optionsAction = null)
    {
        var services = siloBuilder.Services;

        if (optionsAction is not null)
        {
            services.Configure(optionsAction);
        }

        services
            .AddSingleton(typeof(HubLifetimeManager<>), typeof(OrleansHubLifetimeManager<>))
            .AddSingleton<IReminderResolver, ReminderResolver>();

        siloBuilder.AddReminders();

        return siloBuilder;
    }
}