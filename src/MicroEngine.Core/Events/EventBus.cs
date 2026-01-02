using MicroEngine.Core.Exceptions;
using MicroEngine.Core.Resources;

namespace MicroEngine.Core.Events;

/// <summary>
/// Event handler delegate for typed events.
/// </summary>
/// <typeparam name="T">Type of event to handle.</typeparam>
public delegate void EventHandler<T>(T eventData) where T : IEvent;

/// <summary>
/// Delegate for initializing an event instance from the pool.
/// Enables zero-allocation event publishing: SDK rents from pool, user fills, SDK returns.
/// </summary>
/// <typeparam name="T">Type of event to initialize.</typeparam>
public delegate void EventInitializer<T>(T eventData) where T : IEvent;

/// <summary>
/// Central event bus for publishing and subscribing to events.
/// Thread-safe, zero-allocation implementation for event-driven communication.
/// </summary>
/// <remarks>
/// The SDK manages all event lifecycle via object pools to eliminate garbage collection
/// pressure during gameplay. Users provide an EventInitializer delegate instead of creating
/// event instances directly, ensuring SDK control over memory management.
/// </remarks>
public sealed class EventBus : IDisposable
{
    private interface IHandlerWrapper
    {
        void Invoke(IEvent eventData);
        bool Wraps(Delegate handler);
    }

    private sealed class HandlerWrapper<T> : IHandlerWrapper where T : IEvent
    {
        private readonly EventHandler<T> _handler;

        public HandlerWrapper(EventHandler<T> handler)
        {
            _handler = handler;
        }

        public void Invoke(IEvent eventData)
        {
            _handler((T)eventData);
        }
        
        public bool Wraps(Delegate handler)
        {
             return _handler == (Delegate)handler;
        }
    }

    private readonly Dictionary<Type, List<IHandlerWrapper>> _subscribers = new();
    private readonly Queue<IEvent> _eventQueue = new();
    private readonly Dictionary<Type, object> _eventPools = new(); // Type -> ObjectPool<T>
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Gets the number of queued events waiting to be processed.
    /// </summary>
    public int QueuedEventCount
    {
        get
        {
            lock (_lock)
            {
                return _eventQueue.Count;
            }
        }
    }

    /// <summary>
    /// Gets the total number of active subscriptions across all event types.
    /// </summary>
    public int SubscriptionCount
    {
        get
        {
            lock (_lock)
            {
                return _subscribers.Values.Sum(list => list.Count);
            }
        }
    }

    /// <summary>
    /// Subscribes to events of a specific type.
    /// </summary>
    /// <typeparam name="T">Type of event to subscribe to.</typeparam>
    /// <param name="handler">Handler to invoke when event is published.</param>
    public void Subscribe<T>(EventHandler<T> handler) where T : IEvent
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(handler);

        lock (_lock)
        {
            var eventType = typeof(T);
            if (!_subscribers.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<IHandlerWrapper>();
                _subscribers[eventType] = handlers;
            }

            // Avoid duplicate subscriptions
            if (!handlers.Any(h => h.Wraps(handler)))
            {
                 handlers.Add(new HandlerWrapper<T>(handler));
            }
        }
    }

    /// <summary>
    /// Unsubscribes from events of a specific type.
    /// </summary>
    /// <typeparam name="T">Type of event to unsubscribe from.</typeparam>
    /// <param name="handler">Handler to remove.</param>
    public void Unsubscribe<T>(EventHandler<T> handler) where T : IEvent
    {
        if (_disposed)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(handler);

        lock (_lock)
        {
            var eventType = typeof(T);
            if (_subscribers.TryGetValue(eventType, out var handlers))
            {
                handlers.RemoveAll(h => h.Wraps(handler));
                if (handlers.Count == 0)
                {
                    _subscribers.Remove(eventType);
                }
            }
        }
    }

    /// <summary>
    /// Publishes an event immediately to all subscribers.
    /// Event is processed synchronously.
    /// </summary>
    /// <typeparam name="T">Type of event to publish.</typeparam>
    /// <param name="eventData">Event data to publish.</param>
    public void Publish<T>(T eventData) where T : IEvent
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(eventData);

        List<IHandlerWrapper>? handlersCopy = null;

        lock (_lock)
        {
             if (_subscribers.TryGetValue(typeof(T), out var handlers))
             {
                 handlersCopy = new List<IHandlerWrapper>(handlers);
             }
        }

        if (handlersCopy != null)
        {
            DispatchEvents(handlersCopy, eventData);
        }
    }

    /// <summary>
    /// Queues an event for deferred processing using the object pool.
    /// The SDK rents an instance from the pool, invokes the initializer to fill it,
    /// then enqueues it for processing.
    /// </summary>
    /// <typeparam name="T">Type of event to queue.</typeparam>
    /// <param name="initializer">Delegate to initialize the event instance.</param>
    public void Queue<T>(EventInitializer<T> initializer) where T : class, IEvent, IPoolable, new()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(initializer);

        // Get or create pool for this event type
        var poolType = typeof(ObjectPool<>).MakeGenericType(typeof(T));
        var eventType = typeof(T);

        lock (_lock)
        {
            if (!_eventPools.TryGetValue(eventType, out var poolObj))
            {
                poolObj = Activator.CreateInstance(poolType, 16, 256)!;
                _eventPools[eventType] = poolObj;
            }

            // Rent from pool and initialize
            var rentMethod = poolType.GetMethod("Rent")!;
            var eventInstance = (T)rentMethod.Invoke(poolObj, new object?[] { null })!;
            
            initializer(eventInstance);
            _eventQueue.Enqueue(eventInstance);
        }
    }

    /// <summary>
    /// Alternative Queue method that takes the event type dynamically.
    /// Useful for reflection-based event sending.
    /// </summary>
    public void Queue(Type eventType, Action<IEvent> initializer)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(eventType);
        ArgumentNullException.ThrowIfNull(initializer);

        if (!typeof(IEvent).IsAssignableFrom(eventType))
        {
            throw new ArgumentException($"Type {eventType.Name} must implement IEvent", nameof(eventType));
        }

        lock (_lock)
        {
            if (!_eventPools.TryGetValue(eventType, out var poolObj))
            {
                var poolType = typeof(ObjectPool<>).MakeGenericType(eventType);
                poolObj = Activator.CreateInstance(poolType, 16, 256)!;
                _eventPools[eventType] = poolObj;
            }

            var eventPoolType = typeof(ObjectPool<>).MakeGenericType(eventType);
            var rentMethod = eventPoolType.GetMethod("Rent")!;
            var eventInstance = (IEvent)rentMethod.Invoke(poolObj, new object?[] { null })!;
            
            initializer(eventInstance);
            _eventQueue.Enqueue(eventInstance);
        }
    }

    /// <summary>
    /// Processes all queued events and returns them to their respective pools.
    /// Should be called once per frame or update cycle.
    /// </summary>
    public void ProcessEvents()
    {
        if (_disposed)
        {
            return;
        }

        while (true)
        {
            IEvent? eventData;

            lock (_lock)
            {
                if (_eventQueue.Count == 0)
                {
                    break;
                }
                eventData = _eventQueue.Dequeue();
            }

            var eventType = eventData.GetType();
            List<IHandlerWrapper>? handlersCopy = null;

            lock (_lock)
            {
                 if (_subscribers.TryGetValue(eventType, out var handlers))
                 {
                     handlersCopy = new List<IHandlerWrapper>(handlers);
                 }
            }
            
            if (handlersCopy != null)
            {
                 DispatchEvents(handlersCopy, eventData);
            }

            // Return event to pool after processing
            ReturnToPool(eventData);
        }
    }

    /// <summary>
    /// Returns an event instance to its object pool for reuse.
    /// </summary>
    private void ReturnToPool(IEvent eventData)
    {
        var eventType = eventData.GetType();

        lock (_lock)
        {
            if (_eventPools.TryGetValue(eventType, out var poolObj) && eventData is IPoolable poolable)
            {
                var poolType = typeof(ObjectPool<>).MakeGenericType(eventType);
                var returnMethod = poolType.GetMethod("Return")!;
                returnMethod.Invoke(poolObj, new object[] { eventData });
            }
        }
    }
    
    private void DispatchEvents(List<IHandlerWrapper> handlers, IEvent eventData)
    {
        foreach (var handler in handlers)
        {
            if (eventData.IsHandled)
            {
                break;
            }

            try
            {
                handler.Invoke(eventData);
            }
            catch (Exception ex)
            {
                // Rethrow with context to ensure bugs are not swallowed.
                // This ensures issues in event handlers are visible immediately.
                throw new EventBusException($"Error handling event {eventData.GetType().Name}", ex);
            }
        }
    }

    /// <summary>
    /// Clears all queued events without processing them.
    /// </summary>
    public void ClearQueue()
    {
        lock (_lock)
        {
            _eventQueue.Clear();
        }
    }

    /// <summary>
    /// Clears all subscriptions.
    /// </summary>
    public void ClearSubscriptions()
    {
        lock (_lock)
        {
            _subscribers.Clear();
        }
    }

    /// <summary>
    /// Disposes the event bus and all pooled resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        lock (_lock)
        {
            _eventQueue.Clear();
            _subscribers.Clear();

            // Dispose all object pools
            foreach (var pool in _eventPools.Values)
            {
                var disposable = pool as IDisposable;
                disposable?.Dispose();
            }
            _eventPools.Clear();

            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }
}
