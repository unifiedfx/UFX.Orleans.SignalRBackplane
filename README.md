# UFX.Orleans.SignalR

# Overview

[![CI](https://github.com/unifiedfx/UFX.Orleans.SignalR/actions/workflows/ci.yml/badge.svg)](https://github.com/unifiedfx/UFX.Orleans.SignalR/actions/workflows/ci.yml)

[Orleans](https://github.com/dotnet/orleans) is a framework that provides a straightforward approach to building distributed high-scale computing applications without the need to learn and apply complex concurrency or other scaling patterns.

[ASP.NET Core SignalR](https://github.com/aspnet/SignalR) is a library for ASP.NET Core that makes it incredibly simple to add real-time web functionality to your applications.
The ability to have your server-side code push content to the connected clients as it happens, in real-time with support for the following clients:

* [.Net](https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client?view=aspnetcore-7.0)
* [Java](https://learn.microsoft.com/en-us/aspnet/core/signalr/java-client?view=aspnetcore-7.0)
* [JavaScript](https://learn.microsoft.com/en-us/aspnet/core/signalr/javascript-client?view=aspnetcore-7.0)

This library is inspired by [SignalR.Orleans](https://github.com/OrleansContrib/SignalR.Orleans) and [Microsoft.AspNetCore.SignalR.StackExchangeRedis](https://www.nuget.org/packages/Microsoft.AspNetCore.SignalR.StackExchangeRedis/) and provides a [SignalR backplane](https://learn.microsoft.com/en-us/aspnet/signalr/overview/performance/scaleout-in-signalr) on top of Orleans, allowing scale-out to multiple servers with optimal performance and minimal dependencies.
This library supports Orleans V7 and uses Grain Observers as a PubSub mechanism.

# Benefits

* Ideal redundancy and scale-out compared to Redis due to co-hosting SignalR hubs with Orleans Silos.
* Eliminates the requirement for additional 3rd-party components to learn/scale and manage (i.e., Redis).
* Works with any supported Orleans storage provider: [ADO.NET](https://learn.microsoft.com/en-us/dotnet/orleans/grains/grain-persistence/relational-storage), [Azure Storage](https://learn.microsoft.com/en-us/dotnet/orleans/grains/grain-persistence/azure-storage), [Amazon DynamoDB](https://learn.microsoft.com/en-us/dotnet/orleans/grains/grain-persistence/dynamodb-storage) and [MongoDb](https://github.com/OrleansContrib/Orleans.Providers.MongoDB), among others.
* Other than configuring the Orleans Silo, there is no requirement to interact with Orleans directly. You can use the SignalR `IHubContext` directly, and messages will be sent across multiple servers if required.
* Minimal latency due to the direct server-to-server messaging using Grain Observers as a PubSub mechanism, as opposed to Orleans streams that work on a Store & Forward Queue.
* It can be used instead of [Azure SignalR Service](https://azure.microsoft.com/en-gb/products/signalr-service/#overview) scale-out, potentially saving thousands of dollars.

# Usage
## Adding the Backplane
This is the minimum setup required to use this backplane:

```cs
builder
    .Host
    .UseOrleans(siloBuilder => siloBuilder
        .AddMemoryGrainStorageAsDefault()
        .AddMemoryGrainStorage(UFX.Orleans.SignalR.Constants.StorageName)
        .UseInMemoryReminderService()
        .AddSignalRBackplane()
    );
```

`AddSignalRBackplane` will register reminder support on the silo if not already registered. You must provide reminder persistence using the `UseInMemoryReminderService()` extension (unsuitable for production), or a [persisted reminder storage provider](https://learn.microsoft.com/en-us/dotnet/orleans/grains/timers-and-reminders#configuration). 

You must also provide a named storage provider for the grains. The name you must use is stored in the constant `UFX.Orleans.SignalR.Constants.StorageName`. This allows you to register a storage provider specific to the SignalR backplane, which can be a different storage provider to the rest of your application if preferred. You can see more detail on the persistence API [here](https://learn.microsoft.com/en-us/dotnet/orleans/grains/grain-persistence/?pivots=orleans-7-0#api). All of our grains have the state name of `orleans-signalr-grains` stored in the constant `Orleans.SignalR.Constants.StateName`.

Due to a [regression in Orleans v7](https://github.com/dotnet/orleans/issues/8283), you must register both a named storage provider and a default storage provider (as shown above). The SignalR backplane uses only the named storage provider.

## Adding SignalR
Adding this backplane does not register the SignalR services that are required to make real-time client-to-server and server-to-client possible. We leave this to you as there are a number of configurations you may want to make when doing this. For out-of-the-box configuration, you can call the `AddSignalR` extension on the `IServiceCollection`:

```cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
```

You can then [further configure SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/configuration?view=aspnetcore-7.0&tabs=dotnet) as required.

## Sending messages to clients
Once the services have been registered by following the section above, you can access an instance of `IHubContext` via dependency injection. You can inject an instance of IHubContext into a controller, middleware, or other DI service. Use the instance to send messages to clients. You can see more detail on the API [here](https://docs.microsoft.com/en-us/aspnet/core/signalr/hubcontext?view=aspnetcore-7.0).

```cs
public class MyService
{
    private readonly IHubContext<MyHub> _hubContext;

    public MyService(IHubContext<MyHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendMessage(string message)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveMessage", message);
    }
}
```

## Logging
All grains implement the `IIncomingGrainCallFilter` interface, which allows us to log all incoming calls to the grains. This is useful for debugging, as we log the grain type, method name called, address and id of the grain. You can enable this by making sure the debug log level is active for the `UFX.Orleans.SignalR` namespace. One way to do this is to add the following to your `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "UFX.Orleans.SignalR": "Debug"
    }
  }
}
``` 

You can see other ways to configure the log level [here](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-7.0#set-log-level-by-command-line-environment-variables-and-other-configuration).

# Sample Client and Server
A sample client and server are supplied in the `samples` folder. The server is a simple ASP.NET Core app that uses the SignalR backplane and provides a single SignalR hub. The client is a simple console application that connects to the server and sends messages to it. The server will add the caller to a group, echo the message to the sender, to the group, and all connected clients.

## Server

The server runs on a random port each time to allow you to run multiple instances locally for multi-silo testing. The port can be found in the console output of the server. 

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://127.0.0.1:62351
```

If you wish to run on the same port each time, you can change the `applicationUrl` in `Properties\launchSettings.json` to a fixed port rather than `:0`. You can then run the server via

```bash
\samples\Server> dotnet run
```

## Client
To run the client, use the following command. You must provide the port number that the server is running on. You can also give an optional number of SignalR connections to create from the client, which defaults to 1.

```bash
\samples\Client> dotnet run <server port number> [connection count]
```

# Design

Each connection, user, and group is represented by their own grain. When a new SignalR connection is made to a hub on a specific server, a connection grain is created, which can live on any silo. The hub then subscribes to the connection grain, which acts as a point of pub-sub communication. When a message is sent to a connection via the `IHubContext` on any server, the connection grain is called, which notifies the hub on the correct server that it should send the message to its local connection.

If the connection has a user identifier associated with it, then the hub will also subscribe to the user grain. When a message is sent to a user via the `IHubContext`, the user grain is called, which in turn notifies all of the hubs that have connections for that user, and they send out the message. The same mechanism is used for groups.

There is also a single HubGrain per hub type, that all hubs of that type subscribe to. This allows the sending of messages to all connections on all hubs of that type. 

This design reduces the number of network requests between silos, using the Orleans grain directory to locate grains and the observer pattern to notify the correct hub of messages.

## Graceful Disconnection
Hubs track their connections and which users and groups these connections are members of. When a connection is disconnected, the hub unsubscribes from the connection grain. All grains have an in-built mechanism whereby they delete their state and deactivate when their last observer unsubscribes. This allows group and user subscriptions to be removed when the last connection for that user or group is removed.

When a silo is shutdown gracefully, all of the connections on that silo are deactivated, and the same mechanism is used to remove the subscriptions.

## Silo Crash
If a silo is stopped before it has a chance to gracefully shutdown, then the grains will remain active. In this scenario, there may be grains that will never be invoked again, because they represent connections/groups/users that no longer exist. To cater for this, every grain will periodically ping its subscribers to check they are still alive. Any defunct observers are removed from the grain's state, and the grain will clear all its state and deactivate if it has no more observers.

This ping period is configurable, and is one day by default. There is a trade-off here between persistence storage and cpu/network/memory usage. A short period will mean grains clean up their state quickly, reducing the amount of persistent storage used bvy defunct grains. However, it will also mean that more network requests are made to check the status of observers and more cpu and memory used to reactivate the grains. A longer period will mean fewer network requests made and grains reactivated less frequently, but more persistent storage may be used by defunct grains.

If you would like to customise the cleanup period, use the `GrainCleanupPeriod` property on the `SignalrOrleansOptions` class.
```cs
.AddSignalRBackplane(x => x.GrainCleanupPeriod = TimeSpan.FromHours(1))
```

# Architecture

![Diagram](assets/Orleans.SignalR.png)