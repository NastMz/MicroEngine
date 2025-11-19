namespace MicroEngine.Core.Exceptions;

/// <summary>
/// Base exception for all scene-related errors.
/// Error codes: SCENE-xxx
/// </summary>
public class SceneException : MicroEngineException
{
    /// <summary>
    /// Gets the scene type that caused the error.
    /// </summary>
    public Type? SceneType { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SceneException"/> class.
    /// </summary>
    public SceneException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SceneException"/> class with a specified error message.
    /// </summary>
    public SceneException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SceneException"/> class with a specified error message and error code.
    /// </summary>
    public SceneException(string message, string errorCode) : base(message, errorCode) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SceneException"/> class with a specified error message and inner exception.
    /// </summary>
    public SceneException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SceneException"/> class with a specified error message, error code, and inner exception.
    /// </summary>
    public SceneException(string message, string errorCode, Exception innerException) : base(message, errorCode, innerException) { }
}

/// <summary>
/// Exception thrown when a scene transition fails.
/// Error code: SCENE-500
/// </summary>
public sealed class SceneTransitionException : SceneException
{
    /// <summary>
    /// Gets the source scene type.
    /// </summary>
    public Type? SourceSceneType { get; }

    /// <summary>
    /// Gets the target scene type.
    /// </summary>
    public Type? TargetSceneType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SceneTransitionException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public SceneTransitionException(string message)
        : base(message, "SCENE-500")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SceneTransitionException"/> class.
    /// </summary>
    /// <param name="sourceType">The source scene type.</param>
    /// <param name="targetType">The target scene type.</param>
    /// <param name="reason">The reason for the failure.</param>
    public SceneTransitionException(Type sourceType, Type targetType, string reason)
        : base($"Failed to transition from {sourceType.Name} to {targetType.Name}: {reason}", "SCENE-500")
    {
        SourceSceneType = sourceType;
        TargetSceneType = targetType;
        WithContext("sourceScene", sourceType.Name);
        WithContext("targetScene", targetType.Name);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SceneTransitionException"/> class with an inner exception.
    /// </summary>
    /// <param name="sourceType">The source scene type.</param>
    /// <param name="targetType">The target scene type.</param>
    /// <param name="reason">The reason for the failure.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public SceneTransitionException(Type sourceType, Type targetType, string reason, Exception innerException)
        : base($"Failed to transition from {sourceType.Name} to {targetType.Name}: {reason}", "SCENE-500", innerException)
    {
        SourceSceneType = sourceType;
        TargetSceneType = targetType;
        WithContext("sourceScene", sourceType.Name);
        WithContext("targetScene", targetType.Name);
    }
}

/// <summary>
/// Exception thrown when a scene lifecycle error occurs.
/// Error code: SCENE-400
/// </summary>
public sealed class SceneLifecycleException : SceneException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SceneLifecycleException"/> class.
    /// </summary>
    /// <param name="sceneType">The scene type.</param>
    /// <param name="operation">The lifecycle operation that failed (e.g., "OnLoad", "OnUnload").</param>
    /// <param name="reason">The reason for the failure.</param>
    public SceneLifecycleException(Type sceneType, string operation, string reason)
        : base($"Scene lifecycle error in {sceneType.Name}.{operation}: {reason}", "SCENE-400")
    {
        SceneType = sceneType;
        WithContext("sceneType", sceneType.Name);
        WithContext("operation", operation);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SceneLifecycleException"/> class with an inner exception.
    /// </summary>
    /// <param name="sceneType">The scene type.</param>
    /// <param name="operation">The lifecycle operation that failed.</param>
    /// <param name="reason">The reason for the failure.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public SceneLifecycleException(Type sceneType, string operation, string reason, Exception innerException)
        : base($"Scene lifecycle error in {sceneType.Name}.{operation}: {reason}", "SCENE-400", innerException)
    {
        SceneType = sceneType;
        WithContext("sceneType", sceneType.Name);
        WithContext("operation", operation);
    }
}

/// <summary>
/// Exception thrown when attempting an invalid scene operation.
/// Error code: SCENE-409
/// </summary>
public sealed class InvalidSceneOperationException : SceneException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidSceneOperationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public InvalidSceneOperationException(string message)
        : base(message, "SCENE-409")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidSceneOperationException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public InvalidSceneOperationException(string message, Exception innerException)
        : base(message, "SCENE-409", innerException)
    {
    }
}
