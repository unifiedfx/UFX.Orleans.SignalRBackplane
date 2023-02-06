using Microsoft.AspNetCore.SignalR;
using Shared;

namespace Server;

public class ChatHub : Hub
{
    public async Task SendToServer(ExampleInvocation message)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "g1");

        await Clients.Caller.SendAsync("ReplyToClient", new ExampleInvocation("Server", "Caller", message.Message));

        await Clients.All.SendAsync("ReplyToClient", new ExampleInvocation("Server", "All Clients", message.Message));

        await Clients.Group("g1").SendAsync("ReplyToClient", new ExampleInvocation("Server", "Group", message.Message));
    }
}