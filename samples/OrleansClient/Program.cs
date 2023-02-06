using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared;
using UFX.Orleans.SignalRBackplane.Client;

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging => logging.AddConsole())
    .UseOrleansClient(clientBuilder => clientBuilder
        .UseLocalhostClustering()
        .AddSignalRHubContexts())
    .Build();

await host.StartAsync();

var hubContextFactory = host.Services.GetRequiredService<IExternalSignalrHubContextFactory>();
var hubContext = hubContextFactory.CreateHubContext("server.chathub");

while (true)
{
    await hubContext.SendAllAsync("ReplyToClient", new object?[] { new ExampleInvocation("OrleansClient", "All Clients", "Hello from the Orleans Client") });
    await Task.Delay(TimeSpan.FromSeconds(3));
}
