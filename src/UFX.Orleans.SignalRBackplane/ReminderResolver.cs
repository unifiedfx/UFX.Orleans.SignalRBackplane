using Orleans.Runtime;

namespace UFX.Orleans.SignalRBackplane;

internal interface IReminderResolver
{
    Task<IGrainReminder> GetReminder(IGrainBase grainBase, string reminderName);
}

/// <summary>
/// An implementation of <see cref="IReminderResolver"/> that follows the same code path that calling this.GetReminder() would use in a grain.
/// This exists so that we can mock it out in tests.
/// </summary>
internal class ReminderResolver : IReminderResolver
{
    public Task<IGrainReminder> GetReminder(IGrainBase grainBase, string reminderName) 
        => grainBase.GetReminder(reminderName);
}