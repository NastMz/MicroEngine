using MicroEngine.Core.Graphics;
using MicroEngine.Core.Input;
using MicroEngine.Core.Logging;
using MicroEngine.Core.Resources;
using MicroEngine.Core.Time;

namespace MicroEngine.Core.Scenes;

/// <summary>
/// Provides access to engine services for scenes.
/// This replaces static service locator pattern with explicit dependency injection.
/// </summary>
/// <remarks>
/// SceneContext is passed to scenes during their lifecycle, providing a clean
/// dependency injection approach without tight coupling to static program state.
/// All services are guaranteed to be non-null when passed to scenes.
/// </remarks>
public sealed class SceneContext
{
    /// <summary>
    /// Gets the 2D rendering backend.
    /// </summary>
    public IRenderBackend2D RenderBackend { get; }

    /// <summary>
    /// Gets the input backend for keyboard, mouse, and gamepad input.
    /// </summary>
    public IInputBackend InputBackend { get; }

    /// <summary>
    /// Gets the time service for delta time and frame rate management.
    /// </summary>
    public ITimeService TimeService { get; }

    /// <summary>
    /// Gets the logger for structured logging.
    /// </summary>
    public ILogger Logger { get; }

    /// <summary>
    /// Gets the texture resource cache.
    /// </summary>
    public ResourceCache<ITexture> TextureCache { get; }

    /// <summary>
    /// Gets the scene manager for scene navigation.
    /// </summary>
    public SceneManager SceneManager { get; }

    /// <summary>
    /// Initializes a new instance of the SceneContext with all required services.
    /// </summary>
    /// <param name="renderBackend">The 2D rendering backend.</param>
    /// <param name="inputBackend">The input backend.</param>
    /// <param name="timeService">The time service.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="textureCache">The texture resource cache.</param>
    /// <param name="sceneManager">The scene manager for navigation.</param>
    /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
    public SceneContext(
        IRenderBackend2D renderBackend,
        IInputBackend inputBackend,
        ITimeService timeService,
        ILogger logger,
        ResourceCache<ITexture> textureCache,
        SceneManager sceneManager)
    {
        RenderBackend = renderBackend ?? throw new ArgumentNullException(nameof(renderBackend));
        InputBackend = inputBackend ?? throw new ArgumentNullException(nameof(inputBackend));
        TimeService = timeService ?? throw new ArgumentNullException(nameof(timeService));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        TextureCache = textureCache ?? throw new ArgumentNullException(nameof(textureCache));
        SceneManager = sceneManager ?? throw new ArgumentNullException(nameof(sceneManager));
    }
}
