namespace Shared;

[GenerateSerializer]
public record ExampleInvocation(string From, string To, string Message);