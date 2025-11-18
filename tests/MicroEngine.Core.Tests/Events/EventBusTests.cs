using MicroEngine.Core.Events;

namespace MicroEngine.Core.Tests.Events;

public sealed class EventBusTests
{
    private sealed class TestEvent : IEvent
    {
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        public bool IsHandled { get; set; }
        public string Message { get; }

        public TestEvent(string message)
        {
            Message = message;
        }
    }

    private sealed class AnotherEvent : IEvent
    {
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        public bool IsHandled { get; set; }
    }

    [Fact]
    public void Subscribe_AddsHandler()
    {
        using var bus = new EventBus();
        var called = false;

        bus.Subscribe<TestEvent>(e => called = true);

        Assert.Equal(1, bus.SubscriptionCount);
        Assert.False(called);
    }

    [Fact]
    public void Subscribe_SameHandlerTwice_OnlyAddsOnce()
    {
        using var bus = new EventBus();
        static void Handler(TestEvent e) {
            // No-op
         }

        bus.Subscribe<TestEvent>(Handler);
        bus.Subscribe<TestEvent>(Handler);

        Assert.Equal(1, bus.SubscriptionCount);
    }

    [Fact]
    public void Unsubscribe_RemovesHandler()
    {
        using var bus = new EventBus();
        static void Handler(TestEvent e) {
            // No-op
         }

        bus.Subscribe<TestEvent>(Handler);
        bus.Unsubscribe<TestEvent>(Handler);

        Assert.Equal(0, bus.SubscriptionCount);
    }

    [Fact]
    public void Publish_InvokesSubscribers()
    {
        using var bus = new EventBus();
        var called = false;
        string? receivedMessage = null;

        bus.Subscribe<TestEvent>(e =>
        {
            called = true;
            receivedMessage = e.Message;
        });

        bus.Publish(new TestEvent("test"));

        Assert.True(called);
        Assert.Equal("test", receivedMessage);
    }

    [Fact]
    public void Publish_MultipleSubscribers_InvokesAll()
    {
        using var bus = new EventBus();
        var callCount = 0;

        bus.Subscribe<TestEvent>(e => callCount++);
        bus.Subscribe<TestEvent>(e => callCount++);
        bus.Subscribe<TestEvent>(e => callCount++);

        bus.Publish(new TestEvent("test"));

        Assert.Equal(3, callCount);
    }

    [Fact]
    public void Publish_HandledEvent_StopsInvocation()
    {
        using var bus = new EventBus();
        var firstCalled = false;
        var secondCalled = false;

        bus.Subscribe<TestEvent>(e =>
        {
            firstCalled = true;
            e.IsHandled = true;
        });

        bus.Subscribe<TestEvent>(e => secondCalled = true);

        bus.Publish(new TestEvent("test"));

        Assert.True(firstCalled);
        Assert.False(secondCalled);
    }

    [Fact]
    public void Publish_DifferentEventTypes_OnlyInvokesMatchingSubscribers()
    {
        using var bus = new EventBus();
        var testEventCalled = false;
        var anotherEventCalled = false;

        bus.Subscribe<TestEvent>(e => testEventCalled = true);
        bus.Subscribe<AnotherEvent>(e => anotherEventCalled = true);

        bus.Publish(new TestEvent("test"));

        Assert.True(testEventCalled);
        Assert.False(anotherEventCalled);
    }

    [Fact]
    public void Queue_AddsEventToQueue()
    {
        using var bus = new EventBus();

        bus.Queue(new TestEvent("test"));

        Assert.Equal(1, bus.QueuedEventCount);
    }

    [Fact]
    public void ProcessEvents_InvokesQueuedEvents()
    {
        using var bus = new EventBus();
        var callCount = 0;

        bus.Subscribe<TestEvent>(e => callCount++);

        bus.Queue(new TestEvent("test1"));
        bus.Queue(new TestEvent("test2"));
        bus.Queue(new TestEvent("test3"));

        Assert.Equal(3, bus.QueuedEventCount);

        bus.ProcessEvents();

        Assert.Equal(3, callCount);
        Assert.Equal(0, bus.QueuedEventCount);
    }

    [Fact]
    public void ProcessEvents_WithHandledEvent_StopsProcessing()
    {
        using var bus = new EventBus();
        var firstCalled = false;
        var secondCalled = false;

        bus.Subscribe<TestEvent>(e =>
        {
            firstCalled = true;
            e.IsHandled = true;
        });

        bus.Subscribe<TestEvent>(e => secondCalled = true);

        bus.Queue(new TestEvent("test"));
        bus.ProcessEvents();

        Assert.True(firstCalled);
        Assert.False(secondCalled);
    }

    [Fact]
    public void ClearQueue_RemovesAllQueuedEvents()
    {
        using var bus = new EventBus();

        bus.Queue(new TestEvent("test1"));
        bus.Queue(new TestEvent("test2"));
        bus.Queue(new TestEvent("test3"));

        Assert.Equal(3, bus.QueuedEventCount);

        bus.ClearQueue();

        Assert.Equal(0, bus.QueuedEventCount);
    }

    [Fact]
    public void ClearSubscriptions_RemovesAllHandlers()
    {
        using var bus = new EventBus();

        bus.Subscribe<TestEvent>(e => { });
        bus.Subscribe<AnotherEvent>(e => { });

        Assert.Equal(2, bus.SubscriptionCount);

        bus.ClearSubscriptions();

        Assert.Equal(0, bus.SubscriptionCount);
    }

    [Fact]
    public void Dispose_ClearsAllState()
    {
        var bus = new EventBus();

        bus.Subscribe<TestEvent>(e => { });
        bus.Queue(new TestEvent("test"));

        bus.Dispose();

        Assert.Equal(0, bus.SubscriptionCount);
        Assert.Equal(0, bus.QueuedEventCount);
    }

    [Fact]
    public void Publish_AfterDispose_ThrowsObjectDisposedException()
    {
        var bus = new EventBus();
        bus.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
            bus.Publish(new TestEvent("test")));
    }

    [Fact]
    public void Subscribe_WithNullHandler_ThrowsArgumentNullException()
    {
        using var bus = new EventBus();

        Assert.Throws<ArgumentNullException>(() =>
            bus.Subscribe<TestEvent>(null!));
    }

    [Fact]
    public void Publish_WithNullEvent_ThrowsArgumentNullException()
    {
        using var bus = new EventBus();

        Assert.Throws<ArgumentNullException>(() =>
            bus.Publish<TestEvent>(null!));
    }
}
