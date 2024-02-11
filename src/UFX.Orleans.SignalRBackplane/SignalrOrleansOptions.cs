namespace UFX.Orleans.SignalRBackplane;

public class SignalrOrleansOptions
{
    public TimeSpan GrainCleanupPeriod { get; set; } = TimeSpan.FromDays(1);
    public bool UseFullyQualifiedGrainTypes { get; set; } = true;
}