using Microsoft.AspNetCore.SignalR;

namespace Server;

public class ChatHub : Hub
{
    public async Task SendToServer(string message)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "g1");

        await Clients.Caller.SendAsync("ReplyToClient", $"To the Caller: {message}");

        await Clients.All.SendAsync("ReplyToClient", $"To All: {message}");

        await Clients.Group("g1").SendAsync("ReplyToClient", $"To the group: {message}");
    }
}
