using MicroEngine.Core.Exceptions;
using MicroEngine.Core.Resources;

namespace MicroEngine.Core.Tests.Exceptions;

/// <summary>
/// Tests for the base MicroEngineException class.
/// </summary>
public sealed class MicroEngineExceptionTests
{
    [Fact]
    public void Constructor_Default_CreatesException()
    {
        var exception = new MicroEngineException();

        Assert.NotNull(exception);
        Assert.Null(exception.ErrorCode);
        Assert.Empty(exception.Context);
    }

    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        var exception = new MicroEngineException("Test error");

        Assert.Equal("Test error", exception.Message);
        Assert.Null(exception.ErrorCode);
    }

    [Fact]
    public void Constructor_WithMessageAndErrorCode_SetsBoth()
    {
        var exception = new MicroEngineException("Test error", "TEST-001");

        Assert.Equal("Test error", exception.Message);
        Assert.Equal("TEST-001", exception.ErrorCode);
    }

    [Fact]
    public void Constructor_WithInnerException_SetsInnerException()
    {
        var innerException = new InvalidOperationException("Inner error");
        var exception = new MicroEngineException("Test error", innerException);

        Assert.Equal("Test error", exception.Message);
        Assert.Same(innerException, exception.InnerException);
    }

    [Fact]
    public void Constructor_WithAllParameters_SetsAllProperties()
    {
        var innerException = new InvalidOperationException("Inner error");
        var exception = new MicroEngineException("Test error", "TEST-001", innerException);

        Assert.Equal("Test error", exception.Message);
        Assert.Equal("TEST-001", exception.ErrorCode);
        Assert.Same(innerException, exception.InnerException);
    }

    [Fact]
    public void WithContext_AddsContextEntry()
    {
        var exception = new MicroEngineException("Test error");

        var result = exception.WithContext("key1", "value1");

        Assert.Same(exception, result); // Fluent interface
        Assert.Single(exception.Context);
        Assert.Equal("value1", exception.Context["key1"]);
    }

    [Fact]
    public void WithContext_MultipleEntries_AddsAllEntries()
    {
        var exception = new MicroEngineException("Test error")
            .WithContext("key1", "value1")
            .WithContext("key2", 42)
            .WithContext("key3", true);

        Assert.Equal(3, exception.Context.Count);
        Assert.Equal("value1", exception.Context["key1"]);
        Assert.Equal(42, exception.Context["key2"]);
        Assert.Equal(true, exception.Context["key3"]);
    }

    [Fact]
    public void ToString_WithErrorCode_IncludesErrorCodeInMessage()
    {
        var exception = new MicroEngineException("Test error", "TEST-001");

        var result = exception.ToString();

        Assert.Contains("[TEST-001]", result);
        Assert.Contains("Test error", result);
    }

    [Fact]
    public void ToString_WithContext_IncludesContextInMessage()
    {
        var exception = new MicroEngineException("Test error")
            .WithContext("userId", 123)
            .WithContext("action", "load");

        var result = exception.ToString();

        Assert.Contains("Context:", result);
        Assert.Contains("userId=123", result);
        Assert.Contains("action=load", result);
    }

    [Fact]
    public void ToString_WithInnerException_IncludesInnerException()
    {
        var innerException = new InvalidOperationException("Inner error");
        var exception = new MicroEngineException("Test error", innerException);

        var result = exception.ToString();

        Assert.Contains("Inner error", result);
        Assert.Contains("--->", result);
    }

    [Fact]
    public void ToString_Complete_IncludesAllInformation()
    {
        var innerException = new InvalidOperationException("Inner error");
        var exception = new MicroEngineException("Test error", "TEST-001", innerException)
            .WithContext("key", "value");

        var result = exception.ToString();

        Assert.Contains("[TEST-001]", result);
        Assert.Contains("Test error", result);
        Assert.Contains("Context:", result);
        Assert.Contains("key=value", result);
        Assert.Contains("Inner error", result);
    }
}

/// <summary>
/// Tests for ECS exceptions.
/// </summary>
public sealed class EcsExceptionTests
{
    [Fact]
    public void EntityNotFoundException_SetsProperties()
    {
        var exception = new EntityNotFoundException(123);

        Assert.Equal(123u, exception.EntityId);
        Assert.Equal("ECS-404", exception.ErrorCode);
        Assert.Contains("123", exception.Message);
        Assert.Equal(123u, exception.Context["entityId"]);
    }

    [Fact]
    public void ComponentNotFoundException_SetsProperties()
    {
        var exception = new ComponentNotFoundException(456, typeof(string));

        Assert.Equal(456u, exception.EntityId);
        Assert.Equal(typeof(string), exception.ComponentType);
        Assert.Equal("ECS-405", exception.ErrorCode);
        Assert.Contains("456", exception.Message);
        Assert.Contains("String", exception.Message);
    }

    [Fact]
    public void DuplicateComponentException_SetsProperties()
    {
        var exception = new DuplicateComponentException(789, typeof(int));

        Assert.Equal(789u, exception.EntityId);
        Assert.Equal(typeof(int), exception.ComponentType);
        Assert.Equal("ECS-409", exception.ErrorCode);
        Assert.Contains("789", exception.Message);
        Assert.Contains("Int32", exception.Message);
    }

    [Fact]
    public void InvalidEntityOperationException_SetsErrorCode()
    {
        var exception = new InvalidEntityOperationException("Invalid operation");

        Assert.Equal("ECS-400", exception.ErrorCode);
        Assert.Contains("Invalid operation", exception.Message);
    }

    [Fact]
    public void WorldException_SetsErrorCode()
    {
        var exception = new WorldException("World error");

        Assert.Equal("ECS-500", exception.ErrorCode);
        Assert.Contains("World error", exception.Message);
    }
}

/// <summary>
/// Tests for Resource exceptions.
/// </summary>
public sealed class ResourceExceptionTests
{
    [Fact]
    public void ResourceNotFoundException_SetsProperties()
    {
        var exception = new ResourceNotFoundException("textures/player.png");

        Assert.Equal("textures/player.png", exception.ResourcePath);
        Assert.Equal("RES-404", exception.ErrorCode);
        Assert.Contains("textures/player.png", exception.Message);
        Assert.Equal("textures/player.png", exception.Context["path"]);
    }

    [Fact]
    public void ResourceLoadException_SetsProperties()
    {
        var exception = new ResourceLoadException("audio/music.ogg", "corrupted file");

        Assert.Equal("audio/music.ogg", exception.ResourcePath);
        Assert.Equal("RES-500", exception.ErrorCode);
        Assert.Contains("audio/music.ogg", exception.Message);
        Assert.Contains("corrupted file", exception.Message);
        Assert.Equal("corrupted file", exception.Context["reason"]);
    }

    [Fact]
    public void InvalidResourceFormatException_SetsProperties()
    {
        var exception = new InvalidResourceFormatException("data/level.json", "JSON");

        Assert.Equal("data/level.json", exception.ResourcePath);
        Assert.Equal("JSON", exception.ExpectedFormat);
        Assert.Equal("RES-400", exception.ErrorCode);
        Assert.Contains("data/level.json", exception.Message);
        Assert.Contains("JSON", exception.Message);
    }

    [Fact]
    public void ResourceValidationException_SetsProperties()
    {
        var exception = new ResourceValidationException(
            "fonts/arial.ttf",
            ResourceValidationError.FileTooLarge,
            "File exceeds 10MB limit");

        Assert.Equal("fonts/arial.ttf", exception.ResourcePath);
        Assert.Equal(ResourceValidationError.FileTooLarge, exception.ValidationError);
        Assert.Equal("RES-422", exception.ErrorCode);
        Assert.Contains("fonts/arial.ttf", exception.Message);
        Assert.Contains("File exceeds 10MB limit", exception.Message);
        Assert.Equal("FileTooLarge", exception.Context["validationError"]);
    }
}

/// <summary>
/// Tests for Scene exceptions.
/// </summary>
public sealed class SceneExceptionTests
{
    [Fact]
    public void SceneTransitionException_WithMessage_SetsErrorCode()
    {
        var exception = new SceneTransitionException("Transition failed");

        Assert.Equal("SCENE-500", exception.ErrorCode);
        Assert.Contains("Transition failed", exception.Message);
    }

    [Fact]
    public void SceneTransitionException_WithTypes_SetsProperties()
    {
        var exception = new SceneTransitionException(typeof(string), typeof(int), "state corrupted");

        Assert.Equal(typeof(string), exception.SourceSceneType);
        Assert.Equal(typeof(int), exception.TargetSceneType);
        Assert.Equal("SCENE-500", exception.ErrorCode);
        Assert.Contains("String", exception.Message);
        Assert.Contains("Int32", exception.Message);
        Assert.Contains("state corrupted", exception.Message);
    }

    [Fact]
    public void SceneLifecycleException_SetsProperties()
    {
        var exception = new SceneLifecycleException(typeof(object), "OnLoad", "null reference");

        Assert.Equal(typeof(object), exception.SceneType);
        Assert.Equal("SCENE-400", exception.ErrorCode);
        Assert.Contains("Object", exception.Message);
        Assert.Contains("OnLoad", exception.Message);
        Assert.Contains("null reference", exception.Message);
        Assert.Equal("OnLoad", exception.Context["operation"]);
    }

    [Fact]
    public void InvalidSceneOperationException_SetsErrorCode()
    {
        var exception = new InvalidSceneOperationException("Cannot pop empty stack");

        Assert.Equal("SCENE-409", exception.ErrorCode);
        Assert.Contains("Cannot pop empty stack", exception.Message);
    }
}

/// <summary>
/// Tests for Physics exceptions.
/// </summary>
public sealed class PhysicsExceptionTests
{
    [Fact]
    public void InvalidCollisionConfigurationException_SetsErrorCode()
    {
        var exception = new InvalidCollisionConfigurationException("Invalid layer configuration");

        Assert.Equal("PHYS-400", exception.ErrorCode);
        Assert.Contains("Invalid layer configuration", exception.Message);
    }

    [Fact]
    public void PhysicsSimulationException_SetsErrorCode()
    {
        var exception = new PhysicsSimulationException("NaN detected in velocity");

        Assert.Equal("PHYS-500", exception.ErrorCode);
        Assert.Contains("NaN detected", exception.Message);
    }
}

/// <summary>
/// Tests for Backend exceptions.
/// </summary>
public sealed class BackendExceptionTests
{
    [Fact]
    public void BackendInitializationException_SetsProperties()
    {
        var exception = new BackendInitializationException("Raylib", "Window creation failed");

        Assert.Equal("Raylib", exception.BackendType);
        Assert.Equal("BACKEND-500", exception.ErrorCode);
        Assert.Contains("Raylib", exception.Message);
        Assert.Contains("Window creation failed", exception.Message);
        Assert.Equal("Window creation failed", exception.Context["reason"]);
    }

    [Fact]
    public void BackendOperationException_SetsProperties()
    {
        var exception = new BackendOperationException("OpenGL", "DrawTexture", "Invalid handle");

        Assert.Equal("OpenGL", exception.BackendType);
        Assert.Equal("DrawTexture", exception.Operation);
        Assert.Equal("BACKEND-400", exception.ErrorCode);
        Assert.Contains("OpenGL", exception.Message);
        Assert.Contains("DrawTexture", exception.Message);
        Assert.Contains("Invalid handle", exception.Message);
    }
}
