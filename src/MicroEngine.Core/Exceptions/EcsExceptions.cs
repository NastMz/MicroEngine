namespace MicroEngine.Core.Exceptions;

/// <summary>
/// Base exception for all ECS-related errors.
/// Error codes: ECS-xxx
/// </summary>
public class EcsException : MicroEngineException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EcsException"/> class.
    /// </summary>
    public EcsException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EcsException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public EcsException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EcsException"/> class with a specified error message and error code.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="errorCode">The error code.</param>
    public EcsException(string message, string errorCode) : base(message, errorCode) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EcsException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public EcsException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EcsException"/> class with a specified error message, error code, and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public EcsException(string message, string errorCode, Exception innerException) : base(message, errorCode, innerException) { }
}

/// <summary>
/// Exception thrown when an entity is not found.
/// Error code: ECS-404
/// </summary>
public sealed class EntityNotFoundException : EcsException
{
    /// <summary>
    /// Gets the entity ID that was not found.
    /// </summary>
    public uint EntityId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityNotFoundException"/> class.
    /// </summary>
    /// <param name="entityId">The entity ID that was not found.</param>
    public EntityNotFoundException(uint entityId)
        : base($"Entity with ID {entityId} not found.", "ECS-404")
    {
        EntityId = entityId;
        WithContext("entityId", entityId);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityNotFoundException"/> class with an inner exception.
    /// </summary>
    /// <param name="entityId">The entity ID that was not found.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public EntityNotFoundException(uint entityId, Exception innerException)
        : base($"Entity with ID {entityId} not found.", "ECS-404", innerException)
    {
        EntityId = entityId;
        WithContext("entityId", entityId);
    }
}

/// <summary>
/// Exception thrown when a component is not found on an entity.
/// Error code: ECS-405
/// </summary>
public sealed class ComponentNotFoundException : EcsException
{
    /// <summary>
    /// Gets the entity ID.
    /// </summary>
    public uint EntityId { get; }

    /// <summary>
    /// Gets the component type that was not found.
    /// </summary>
    public Type ComponentType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentNotFoundException"/> class.
    /// </summary>
    /// <param name="entityId">The entity ID.</param>
    /// <param name="componentType">The component type that was not found.</param>
    public ComponentNotFoundException(uint entityId, Type componentType)
        : base($"Component '{componentType.Name}' not found on entity {entityId}.", "ECS-405")
    {
        EntityId = entityId;
        ComponentType = componentType;
        WithContext("entityId", entityId);
        WithContext("componentType", componentType.Name);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentNotFoundException"/> class.
    /// </summary>
    /// <param name="entityId">The entity ID.</param>
    /// <param name="componentTypeName">The component type name that was not found.</param>
    public ComponentNotFoundException(uint entityId, string componentTypeName)
        : base($"Component '{componentTypeName}' not found on entity {entityId}.", "ECS-405")
    {
        EntityId = entityId;
        ComponentType = null!;
        WithContext("entityId", entityId);
        WithContext("componentType", componentTypeName);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentNotFoundException"/> class with an inner exception.
    /// </summary>
    /// <param name="entityId">The entity ID.</param>
    /// <param name="componentType">The component type that was not found.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ComponentNotFoundException(uint entityId, Type componentType, Exception innerException)
        : base($"Component '{componentType.Name}' not found on entity {entityId}.", "ECS-405", innerException)
    {
        EntityId = entityId;
        ComponentType = componentType;
        WithContext("entityId", entityId);
        WithContext("componentType", componentType.Name);
    }
}

/// <summary>
/// Exception thrown when attempting to add a duplicate component to an entity.
/// Error code: ECS-409
/// </summary>
public sealed class DuplicateComponentException : EcsException
{
    /// <summary>
    /// Gets the entity ID.
    /// </summary>
    public uint EntityId { get; }

    /// <summary>
    /// Gets the component type that already exists.
    /// </summary>
    public Type ComponentType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateComponentException"/> class.
    /// </summary>
    /// <param name="entityId">The entity ID.</param>
    /// <param name="componentType">The component type that already exists.</param>
    public DuplicateComponentException(uint entityId, Type componentType)
        : base($"Entity {entityId} already has a component of type '{componentType.Name}'.", "ECS-409")
    {
        EntityId = entityId;
        ComponentType = componentType;
        WithContext("entityId", entityId);
        WithContext("componentType", componentType.Name);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateComponentException"/> class.
    /// </summary>
    /// <param name="entityId">The entity ID.</param>
    /// <param name="componentTypeName">The component type name that already exists.</param>
    public DuplicateComponentException(uint entityId, string componentTypeName)
        : base($"Entity {entityId} already has a component of type '{componentTypeName}'.", "ECS-409")
    {
        EntityId = entityId;
        ComponentType = null!;
        WithContext("entityId", entityId);
        WithContext("componentType", componentTypeName);
    }
}

/// <summary>
/// Exception thrown when an invalid entity operation is attempted.
/// Error code: ECS-400
/// </summary>
public sealed class InvalidEntityOperationException : EcsException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidEntityOperationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public InvalidEntityOperationException(string message)
        : base(message, "ECS-400")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidEntityOperationException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public InvalidEntityOperationException(string message, Exception innerException)
        : base(message, "ECS-400", innerException)
    {
    }
}

/// <summary>
/// Exception thrown when a world operation fails.
/// Error code: ECS-500
/// </summary>
public sealed class WorldException : EcsException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorldException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public WorldException(string message)
        : base(message, "ECS-500")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorldException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public WorldException(string message, Exception innerException)
        : base(message, "ECS-500", innerException)
    {
    }
}
