namespace MicroEngine.Core.Events;

/// <summary>
/// Event handler delegate for typed events.
/// </summary>
/// <typeparam name="T">Type of event to handle.</typeparam>
public delegate void EventHandler<T>(T eventData) where T : IEvent;

/// <summary>
/// Central event bus for publishing and subscribing to events.
/// Thread-safe implementation for event-driven communication.
/// </summary>
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
    /// Queues an event for deferred processing.
    /// Event will be processed when ProcessEvents() is called.
    /// </summary>
    /// <typeparam name="T">Type of event to queue.</typeparam>
    /// <param name="eventData">Event data to queue.</param>
    public void Queue<T>(T eventData) where T : IEvent
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(eventData);

        lock (_lock)
        {
            _eventQueue.Enqueue(eventData);
        }
    }

    /// <summary>
    /// Processes all queued events.
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
                // CRITICAL: Previously swallowed. We MUST rethrow to expose bugs.
                // In a production engine, you might wrap this in a custom EngineException
                // or ensure it's logged to a file before rethrowing.
                throw new InvalidOperationException($"Error handling event {eventData.GetType().Name}", ex);
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
    /// Disposes the event bus and clears all state.
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
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }
}
