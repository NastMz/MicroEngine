namespace MicroEngine.Core.Events;

/// <summary>
/// Base interface for all events in the engine.
/// Events are used for decoupled communication between systems and components.
/// </summary>
public interface IEvent
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
