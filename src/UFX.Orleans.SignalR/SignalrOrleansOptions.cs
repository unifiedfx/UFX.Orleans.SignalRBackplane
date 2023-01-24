namespace UFX.Orleans.SignalR;

public class SignalrOrleansOptions
{
    public TimeSpan GrainCleanupPeriod { get; set; } = TimeSpan.FromDays(1);
}