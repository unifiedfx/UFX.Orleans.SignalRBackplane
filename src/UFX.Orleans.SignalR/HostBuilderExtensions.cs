using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace UFX.Orleans.SignalR;

public static class SiloHostBuilderExtensions
{
    public static ISiloBuilder UseSignalR(this ISiloBuilder siloBuilder, Action<SignalrOrleansOptions>? optionsAction = null)
    {
        var services = siloBuilder.Services;

        if (optionsAction is not null)
        {
            services.Configure(optionsAction);
        }
        else
        {
            services.ConfigureOptions<SignalrOrleansOptions>();
        }

        services.AddSingleton(typeof(HubLifetimeManager<>), typeof(OrleansHubLifetimeManager<>));

        siloBuilder.AddReminders();

        // If reminder persistence has not yet been registered, add the in-memory provider
        if (services.All(x => x.ServiceType != typeof(IReminderTable)))
        {
            siloBuilder
                .UseInMemoryReminderService();
        }

        return siloBuilder;
    }
}