using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

var portArg = args.First();
var port = Convert.ToInt32(portArg);

var connectionCountArg = args.LastOrDefault();
var connectionCount = String.IsNullOrWhiteSpace(connectionCountArg) ? 1 : Convert.ToInt32(connectionCountArg);


var connectionTasks = Enumerable.Range(0, connectionCount).Select(_ => StartConnection());
var connections = await Task.WhenAll(connectionTasks);

while (true)
{
    Console.Write("> ");
    var message = Console.ReadLine();

    await connections[0].InvokeAsync("SendToServer", message);
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

    connection.On("ReplyToClient", (string message) => Console.WriteLine($"Server: {message}"));

    await connection.StartAsync();

    return connection;
}