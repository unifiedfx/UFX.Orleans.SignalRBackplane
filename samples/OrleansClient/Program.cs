using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
    await hubContext.SendAllAsync("ReplyToClient", new object?[] { "To the group: Hello from the orleans client" });
    await Task.Delay(TimeSpan.FromSeconds(3));
}