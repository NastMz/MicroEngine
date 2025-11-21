using MicroEngine.Core.Audio;
using MicroEngine.Core.DependencyInjection;
using MicroEngine.Core.Graphics;
using MicroEngine.Core.Input;
using MicroEngine.Core.Logging;
using MicroEngine.Core.Resources;
using MicroEngine.Core.State;
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
    /// Gets the window manager.
    /// </summary>
    public IWindow Window { get; }

    /// <summary>
    /// Gets the 2D renderer.
    /// </summary>
    public IRenderer2D Renderer { get; }

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
    /// Gets the audio resource cache.
    /// </summary>
    public ResourceCache<IAudioClip> AudioCache { get; }

    /// <summary>
    /// Gets the audio device manager.
    /// </summary>
    public IAudioDevice AudioDevice { get; }

    /// <summary>
    /// Gets the sound player for sound effects.
    /// </summary>
    public ISoundPlayer SoundPlayer { get; }

    /// <summary>
    /// Gets the music player for streaming audio.
    /// </summary>
    public IMusicPlayer MusicPlayer { get; }

    /// <summary>
    /// Gets the global game state for persistent data across scenes.
    /// </summary>
    public IGameState GameState { get; }

    /// <summary>
    /// Gets the service container for resolving scoped services.
    /// </summary>
    public IServiceContainer Services { get; }

    /// <summary>
    /// Initializes a new instance of the SceneContext with all required services.
    /// </summary>
    public SceneContext(
        IWindow window,
        IRenderer2D renderer,
        IInputBackend inputBackend,
        ITimeService timeService,
        ILogger logger,
        ResourceCache<ITexture> textureCache,
        ResourceCache<IAudioClip> audioCache,
        IAudioDevice audioDevice,
        ISoundPlayer soundPlayer,
        IMusicPlayer musicPlayer,
        IGameState gameState,
        IServiceContainer services)
    {
        Window = window ?? throw new ArgumentNullException(nameof(window));
        Renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        InputBackend = inputBackend ?? throw new ArgumentNullException(nameof(inputBackend));
        TimeService = timeService ?? throw new ArgumentNullException(nameof(timeService));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        TextureCache = textureCache ?? throw new ArgumentNullException(nameof(textureCache));
        AudioCache = audioCache ?? throw new ArgumentNullException(nameof(audioCache));
        AudioDevice = audioDevice ?? throw new ArgumentNullException(nameof(audioDevice));
        SoundPlayer = soundPlayer ?? throw new ArgumentNullException(nameof(soundPlayer));
        MusicPlayer = musicPlayer ?? throw new ArgumentNullException(nameof(musicPlayer));
        GameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }
}
