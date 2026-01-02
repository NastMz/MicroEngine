using MicroEngine.Core.Resources;

namespace MicroEngine.Core.Events;

/// <summary>
/// Base interface for all events in the engine.
/// Events are used for decoupled communication between systems and components.
/// All events must implement IPoolable to participate in the zero-allocation event system.
/// </summary>
public interface IEvent : IPoolable
{
    /// <summary>
    /// Gets the timestamp when the event was created.
    /// </summary>
    DateTime Timestamp { get; }

    /// <summary>
    /// Gets whether this event has been handled.
    /// </summary>
    bool IsHandled { get; set; }
}
