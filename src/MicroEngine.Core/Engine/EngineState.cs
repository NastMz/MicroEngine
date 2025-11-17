namespace MicroEngine.Core.Engine;

/// <summary>
/// Represents the current state of the engine.
/// </summary>
public enum EngineState
{
    /// <summary>
    /// Engine has not been initialized yet.
    /// </summary>
    NotInitialized,

    /// <summary>
    /// Engine is initializing.
    /// </summary>
    Initializing,

    /// <summary>
    /// Engine is running normally.
    /// </summary>
    Running,

    /// <summary>
    /// Engine is paused.
    /// </summary>
    Paused,

    /// <summary>
    /// Engine is shutting down.
    /// </summary>
    ShuttingDown,

    /// <summary>
    /// Engine has stopped.
    /// </summary>
    Stopped
}
