using Microsoft.Extensions.DependencyInjection;

namespace UFX.Orleans.SignalR.Client;

public static class ClientBuilderExtensions
{
    public static IClientBuilder AddSignalRHubContexts(this IClientBuilder clientBuilder)
    {
        clientBuilder.Services
            .AddSingleton<IExternalSignalrHubContextFactory, ExternalSignalrHubContextFactory>()
            .AddSingleton(typeof(IExternalSignalrHubContext<>), typeof(ExternalSignalrHubContext<>));

        return clientBuilder;
    }
}