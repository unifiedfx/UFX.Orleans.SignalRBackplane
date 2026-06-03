using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using UFX.Orleans.SignalRBackplane.Grains;

namespace UFX.Orleans.SignalRBackplane.Tests;

public class ConnectionGrainTests
{
    private static ConnectionGrain CreateGrain(
        IPersistentState<SubscriptionState> persistedSubs,
        IGrainContext grainContext,
        IReminderResolver? reminderResolver = null)
    {
        var resolver = reminderResolver ?? A.Fake<IReminderResolver>();
        A.CallTo(() => resolver.GetReminder(A<IGrainBase>.Ignored, A<string>.Ignored))
            .Returns<IGrainReminder?>(null!);
        return new ConnectionGrain(
            persistedSubs,
            grainContext,
            resolver,
            A.Fake<IOptions<SignalrOrleansOptions>>(),
            A.Fake<ILogger<ConnectionGrain>>());
    }

    [Fact]
    public async Task HasObserversAsync_ReturnsFalse_WhenNoObserversSubscribed()
    {
        // Arrange
        var persistentState = A.Fake<IPersistentState<SubscriptionState>>();
        var grainContext = A.Fake<IGrainContext>();
        A.CallTo(() => grainContext.GrainId).Returns(GrainId.Create("test", "hub/conn1"));

        var grain = CreateGrain(persistentState, grainContext);

        // Act
        var result = await grain.HasObserversAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasObserversAsync_ReturnsTrue_WhenObserverIsReachable()
    {
        // Arrange
        var persistentState = A.Fake<IPersistentState<SubscriptionState>>();
        var grainContext = A.Fake<IGrainContext>();
        A.CallTo(() => grainContext.GrainId).Returns(GrainId.Create("test", "hub/conn1"));
        var reminderResolver = A.Fake<IReminderResolver>();
        A.CallTo(() => reminderResolver.GetReminder(A<IGrainBase>.Ignored, A<string>.Ignored))
            .Returns<IGrainReminder?>(null!);

        var grain = CreateGrain(persistentState, grainContext, reminderResolver);

        var observer = A.Fake<IHubLifetimeManagerGrainObserver>();
        // PingAsync succeeds by default — observer is reachable
        await grain.SubscribeAsync(observer);

        // Act
        var result = await grain.HasObserversAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasObserversAsync_ReturnsFalse_AndPrunesObserver_WhenAllObserversAreDefunct()
    {
        // Arrange
        var persistentState = A.Fake<IPersistentState<SubscriptionState>>();
        var grainContext = A.Fake<IGrainContext>();
        A.CallTo(() => grainContext.GrainId).Returns(GrainId.Create("test", "hub/conn1"));
        var reminderResolver = A.Fake<IReminderResolver>();
        A.CallTo(() => reminderResolver.GetReminder(A<IGrainBase>.Ignored, A<string>.Ignored))
            .Returns<IGrainReminder?>(null!);

        var grain = CreateGrain(persistentState, grainContext, reminderResolver);

        var observer = A.Fake<IHubLifetimeManagerGrainObserver>();
        await grain.SubscribeAsync(observer);

        // Simulate a defunct observer — PingAsync throws
        A.CallTo(() => observer.PingAsync()).Throws(new Exception("Observer unreachable"));

        // Act
        var result = await grain.HasObserversAsync();

        // Assert
        result.Should().BeFalse();
        persistentState.State.Observers.Should().BeEmpty();
    }
}
