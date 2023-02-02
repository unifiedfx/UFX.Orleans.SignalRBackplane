using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Server;
using UFX.Orleans.SignalR;

var builder = WebApplication.CreateBuilder(args);

builder
    .Host
    .UseOrleans(siloBuilder => siloBuilder
        .UseLocalhostClustering()
        .AddAzureBlobGrainStorage(Constants.StorageName, c => c.ConfigureBlobServiceClient("UseDevelopmentStorage=true"))
        .UseInMemoryReminderService()
        .AddSignalRBackplane(x => x.GrainCleanupPeriod = TimeSpan.FromMinutes(1))
    );

builder.Services.AddSignalR();

var app = builder.Build();

app.MapHub<ChatHub>("/chat");
app.MapHub<NotificationHub>("/notifications");

app.MapPost("chat", async ([FromServices] IHubContext<ChatHub> hub, [FromQuery]string message) =>
{
    await hub.Clients.All.SendAsync("ReplyToClient", $"From HTTP POST To All: {message}");

    return Results.NoContent();
});

app.Run();