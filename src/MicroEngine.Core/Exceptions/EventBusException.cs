namespace MicroEngine.Core.Exceptions;

/// <summary>
/// Exception thrown when an error occurs in the EventBus.
/// </summary>
public class EventBusException : MicroEngineException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventBusException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public EventBusException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventBusException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public EventBusException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
