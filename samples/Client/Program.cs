using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

var firstArg = args.FirstOrDefault();
var connectionCount = String.IsNullOrWhiteSpace(firstArg) ? 1 : Convert.ToInt32(firstArg);

var connectionTasks = Enumerable.Range(0, connectionCount).Select(_ => StartConnection());
var connections = await Task.WhenAll(connectionTasks);

while (true)
{
    Console.Write("> ");
    var message = Console.ReadLine();

    await connections[0].InvokeAsync("SendToServer", message);
}

static async Task<HubConnection> StartConnection()
{
    var connection = new HubConnectionBuilder()
        .WithUrl("https://localhost:7181/chat")
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