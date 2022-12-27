using Microsoft.AspNetCore.SignalR.Protocol;
namespace UFX.Orleans.SignalR;

[Immutable]
[GenerateSerializer]
public class InvocationRequest
{
    [Id(0)]
    public string Target { get; set; }
    [Id(1)]
    public object?[] Arguments { get; set; }
    public InvocationRequest(string target, object?[] arguments)
    {
        Target = target;
        Arguments = arguments;
    }
    public InvocationMessage ToMessage() => new InvocationMessage(Target, Arguments);
}