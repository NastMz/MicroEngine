namespace MicroEngine.Core.Exceptions;

/// <summary>
/// Exception thrown when an error occurs in the EventBus.
/// </summary>
public class EventBusException : MicroEngineException
{
    public EventBusException(string message) : base(message)
    {
    }

    public EventBusException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
