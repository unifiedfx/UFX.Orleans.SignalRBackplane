using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Shared;

var port = Convert.ToInt32(args[0]);
var connectionCount = args.Length == 2 ? Convert.ToInt32(args[1]) : 1;

var connectionTasks = Enumerable.Range(0, connectionCount).Select(_ => StartConnection());
var connections = await Task.WhenAll(connectionTasks);

while (true)
{
    Console.Write("> ");
    var message = Console.ReadLine();

    await connections[0].InvokeAsync(
        "SendToServer", 
        new ExampleInvocation("Sample Client", "Sample Server", message!)
    );
}

async Task<HubConnection> StartConnection()
{
    var connection = new HubConnectionBuilder()
        .WithUrl($"https://localhost:{port}/chat")
        .WithAutomaticReconnect()
        .ConfigureLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Information);
            logging.AddConsole();
        })
        .Build();

    connection.On("ReplyToClient", (ExampleInvocation message) => Console.WriteLine(message));

    await connection.StartAsync();

    return connection;
}