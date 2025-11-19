namespace MicroEngine.Core.Exceptions;

/// <summary>
/// Base exception for all physics-related errors.
/// Error codes: PHYS-xxx
/// </summary>
public class PhysicsException : MicroEngineException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PhysicsException"/> class.
    /// </summary>
    public PhysicsException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="PhysicsException"/> class with a specified error message.
    /// </summary>
    public PhysicsException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="PhysicsException"/> class with a specified error message and error code.
    /// </summary>
    public PhysicsException(string message, string errorCode) : base(message, errorCode) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="PhysicsException"/> class with a specified error message and inner exception.
    /// </summary>
    public PhysicsException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="PhysicsException"/> class with a specified error message, error code, and inner exception.
    /// </summary>
    public PhysicsException(string message, string errorCode, Exception innerException) : base(message, errorCode, innerException) { }
}

/// <summary>
/// Exception thrown when an invalid collision configuration is detected.
/// Error code: PHYS-400
/// </summary>
public sealed class InvalidCollisionConfigurationException : PhysicsException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidCollisionConfigurationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public InvalidCollisionConfigurationException(string message)
        : base(message, "PHYS-400")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidCollisionConfigurationException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public InvalidCollisionConfigurationException(string message, Exception innerException)
        : base(message, "PHYS-400", innerException)
    {
    }
}

/// <summary>
/// Exception thrown when a physics simulation error occurs.
/// Error code: PHYS-500
/// </summary>
public sealed class PhysicsSimulationException : PhysicsException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PhysicsSimulationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public PhysicsSimulationException(string message)
        : base(message, "PHYS-500")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PhysicsSimulationException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public PhysicsSimulationException(string message, Exception innerException)
        : base(message, "PHYS-500", innerException)
    {
    }
}
