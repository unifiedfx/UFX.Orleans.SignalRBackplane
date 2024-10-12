using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using UFX.Orleans.SignalRBackplane.Grains;

namespace UFX.Orleans.SignalRBackplane.Tests
{
    public class SignalrBaseGrainTests
    {
        [Fact]
        public async Task Subscribe_AddsObserverToState_IfNotAlreadySubscribed()
        {
            // Arrange
            var persistentState = A.Fake<IPersistentState<SubscriptionState>>();
            var grainContext = A.Fake<IGrainContext>();

            A.CallTo(() => grainContext.GrainId).Returns(GrainId.Create("test", "a"));

            var grain = new TestGrain(persistentState, grainContext);

            // Act
            await grain.SubscribeAsync(A.Fake<IHubLifetimeManagerGrainObserver>());

            // Assert
            persistentState.State.Observers.Should().HaveCount(1);
        }

        [Fact]
        public async Task Subscribe_DoesNotAddObserverToState_IfAlreadySubscribed()
        {
            // Arrange
            var persistentState = A.Fake<IPersistentState<SubscriptionState>>();
            var grainContext = A.Fake<IGrainContext>();

            A.CallTo(() => grainContext.GrainId).Returns(GrainId.Create("test", "a"));

            var grain = new TestGrain(persistentState, grainContext);

            // Act
            var observer = A.Fake<IHubLifetimeManagerGrainObserver>();
            await grain.SubscribeAsync(observer);
            await grain.SubscribeAsync(observer);

            // Assert
            persistentState.State.Observers.Should().HaveCount(1);
        }

        [Fact]
        public async Task Unsubscribe_RemovesObserverFromState_IfAlreadySubscribed()
        {
            // Arrange
            var persistentState = A.Fake<IPersistentState<SubscriptionState>>();
            var grainContext = A.Fake<IGrainContext>();

            A.CallTo(() => grainContext.GrainId).Returns(GrainId.Create("test", "a"));

            var grain = new TestGrain(persistentState, grainContext);

            var observer1 = A.Fake<IHubLifetimeManagerGrainObserver>();
            var observer2 = A.Fake<IHubLifetimeManagerGrainObserver>();
            await grain.SubscribeAsync(observer1);
            await grain.SubscribeAsync(observer2);

            // Act
            await grain.UnsubscribeAsync(observer1);

            // Assert
            persistentState.State.Observers.Should().HaveCount(1);
        }

        [Fact]
        public async Task Unsubscribe_DeactivatesGrain_AfterReminderWhenLastObserverUnsubscribes()
        {
            // Arrange
            var persistentState = A.Fake<IPersistentState<SubscriptionState>>();
            var grainContext = A.Fake<IGrainContext>();
            var reminderResolver = A.Fake<IReminderResolver>();
            
            A.CallTo(() => grainContext.GrainId).Returns(GrainId.Create("test", "a"));
            A.CallTo(() => reminderResolver.GetReminder(A<IGrainBase>.Ignored, A<string>.Ignored)).Returns<IGrainReminder?>(null!);

            var grain = new TestGrain(
                persistentState,
                grainContext,
                reminderResolver
            );
            
            var observer = A.Fake<IHubLifetimeManagerGrainObserver>();
            await grain.SubscribeAsync(observer);
            
            // Act
            await grain.UnsubscribeAsync(observer);

            A.CallTo(() => grainContext.Deactivate(new(DeactivationReasonCode.ApplicationRequested, "DeactivateOnIdle was called."), null)).MustNotHaveHappened();

            await grain.ReceiveReminder("PingReminderName", new TickStatus());

            // Assert
            A.CallTo(() => grainContext.Deactivate(new(DeactivationReasonCode.ApplicationRequested, "DeactivateOnIdle was called."), null)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task Unsubscribe_DoesNotDeactivateGrain_WhenSecondLastObserverUnsubscribes()
        {
            // Arrange
            var persistentState = A.Fake<IPersistentState<SubscriptionState>>();
            var grainContext = A.Fake<IGrainContext>();
            var reminderResolver = A.Fake<IReminderResolver>();

            A.CallTo(() => grainContext.GrainId).Returns(GrainId.Create("test", "a"));
            A.CallTo(() => reminderResolver.GetReminder(A<IGrainBase>.Ignored, A<string>.Ignored)).Returns<IGrainReminder?>(null!);

            var grain = new TestGrain(
                persistentState,
                grainContext,
                reminderResolver
            );

            var observer1 = A.Fake<IHubLifetimeManagerGrainObserver>();
            var observer2 = A.Fake<IHubLifetimeManagerGrainObserver>();
            await grain.SubscribeAsync(observer1);
            await grain.SubscribeAsync(observer2);

            // Act
            await grain.UnsubscribeAsync(observer1);

            // Assert
            A.CallTo(() => grainContext.Deactivate(A<DeactivationReason>.Ignored, A<CancellationToken>.Ignored)).MustNotHaveHappened();
        }

        [Fact]
        public async Task NotifyAllObserversAsync_RemovesObserver_WhenNotificationFails()
        {
            // Arrange
            var persistentState = A.Fake<IPersistentState<SubscriptionState>>();
            var grainContext = A.Fake<IGrainContext>();
            var reminderResolver = A.Fake<IReminderResolver>();

            A.CallTo(() => grainContext.GrainId).Returns(GrainId.Create("test", "a"));
            A.CallTo(() => reminderResolver.GetReminder(A<IGrainBase>.Ignored, A<string>.Ignored)).Returns<IGrainReminder?>(null!);

            var grain = new TestGrain(
                persistentState,
                grainContext,
                reminderResolver
            );

            var observer = A.Fake<IHubLifetimeManagerGrainObserver>();
            await grain.SubscribeAsync(observer);

            // Act
            A.CallTo(() => observer.PingAsync()).Throws(new Exception());
            await grain.ReceiveReminder("PingReminderName", new TickStatus());

            // Assert
            persistentState.State.Observers.Should().BeEmpty();
        }

        [Fact]
        public async Task NotifyAllObserversAsync_DoesNotRemoveObserver_WhenNotificationSucceeds()
        {
            // Arrange
            var persistentState = A.Fake<IPersistentState<SubscriptionState>>();
            var grainContext = A.Fake<IGrainContext>();
            var reminderResolver = A.Fake<IReminderResolver>();

            A.CallTo(() => grainContext.GrainId).Returns(GrainId.Create("test", "a"));
            A.CallTo(() => reminderResolver.GetReminder(A<IGrainBase>.Ignored, A<string>.Ignored)).Returns<IGrainReminder?>(null!);

            var grain = new TestGrain(
                persistentState,
                grainContext,
                reminderResolver
            );

            var observer = A.Fake<IHubLifetimeManagerGrainObserver>();
            await grain.SubscribeAsync(observer);

            // Act
            await grain.ReceiveReminder("PingReminderName", new TickStatus());

            // Assert
            persistentState.State.Observers.Should().HaveCount(1).And.Contain(observer);
        }

        [Fact]
        public void HubAndEntityNames_AreCorrect_WhenGrainHasNoEntityIdInKey()
        {
            // Arrange
            var persistentState = A.Fake<IPersistentState<SubscriptionState>>();
            var grainContext = A.Fake<IGrainContext>();
            var reminderResolver = A.Fake<IReminderResolver>();

            const string hubName = "hub.name";
            A.CallTo(() => grainContext.GrainId).Returns(GrainId.Create("test", hubName));

            // Act
            var grain = new TestGrain(
                persistentState,
                grainContext,
                reminderResolver
            );

            // Assert
            grain.TestGrainHubName.Should().Be(hubName);
            grain.TestGrainEntityId.Should().Be(hubName);
        }

        [Fact]
        public void HubAndEntityNames_AreCorrect_WhenGrainHasEntityIdInKey()
        {
            // Arrange
            var persistentState = A.Fake<IPersistentState<SubscriptionState>>();
            var grainContext = A.Fake<IGrainContext>();
            var reminderResolver = A.Fake<IReminderResolver>();

            const string hubName = "hub.name";
            const string entityId = "entity.id";
            A.CallTo(() => grainContext.GrainId).Returns(GrainId.Create("test", $"{hubName}/{entityId}"));

            // Act
            var grain = new TestGrain(
                persistentState,
                grainContext,
                reminderResolver
            );

            // Assert
            grain.TestGrainHubName.Should().Be(hubName);
            grain.TestGrainEntityId.Should().Be(entityId);
        }

        private class TestGrain : SignalrBaseGrain
        {
            public string TestGrainHubName => HubName;
            public string TestGrainEntityId => EntityId;

            public TestGrain(IPersistentState<SubscriptionState> persistedSubs, IGrainContext grainContext)
                : base(persistedSubs, grainContext, A.Fake<IReminderResolver>(), A.Fake<IOptions<SignalrOrleansOptions>>(), A.Fake<ILogger<SignalrBaseGrain>>())
            {
            }
            
            public TestGrain(
                IPersistentState<SubscriptionState> persistedSubs, 
                IGrainContext grainContext,
                IReminderResolver reminderResolver) 
                : base(persistedSubs, grainContext, reminderResolver, A.Fake<IOptions<SignalrOrleansOptions>>(), A.Fake<ILogger<SignalrBaseGrain>>())
            {
            }
        }
    }
}