using MicroEngine.Core.Audio;
using MicroEngine.Core.Graphics;
using MicroEngine.Core.Input;
using MicroEngine.Core.Logging;
using MicroEngine.Core.Math;
using MicroEngine.Core.Resources;
using MicroEngine.Core.Scenes;

namespace MicroEngine.Game.Scenes.Demos;

/// <summary>
/// Demonstrates spatial audio with distance attenuation.
/// Shows how sound volume changes based on listener position.
/// </summary>
public sealed class SpatialAudioDemo : Scene
{
    private IInputBackend _inputBackend = null!;
    private IRenderer2D _renderer = null!;
    private IAudioDevice _audioDevice = null!;
    private ISoundPlayer _soundPlayer = null!;
    private ILogger _logger = null!;
    private ResourceCache<IAudioClip> _audioCache = null!;

    private const string SCENE_NAME = "SpatialAudioDemo";
    private const float MAX_DISTANCE = 250f;
    private const float LISTENER_SPEED = 200f;

    private Vector2 _listenerPosition;
    private readonly List<Vector2> _soundSources = new();
    private readonly List<IAudioClip?> _soundClips = new();
    private readonly List<string> _soundPaths = new();
    private readonly float[] _loopTimers = new float[3];
    // Different loop intervals to make it sound less repetitive/synced
    private readonly float[] _loopIntervals = { 0.8f, 1.2f, 1.5f }; 

    /// <summary>
    /// Initializes a new instance of the <see cref="SpatialAudioDemo"/> class.
    /// </summary>
    public SpatialAudioDemo()
        : base(SCENE_NAME)
    {
    }

    /// <inheritdoc/>
    public override void OnLoad(SceneContext context)
    {
        base.OnLoad(context);
        _inputBackend = context.InputBackend;
        _renderer = context.Renderer;
        _audioDevice = context.AudioDevice;
        _soundPlayer = context.SoundPlayer;
        _logger = context.Logger;
        _audioCache = context.AudioCache;
        _logger.Info(SCENE_NAME, "Spatial Audio demo loaded - demonstrating distance attenuation");

        // Initialize listener at center
        _listenerPosition = new Vector2(400, 300);
        _audioDevice.SetListenerPosition(_listenerPosition);

        // Create 3 sound sources at different positions
        _soundSources.Add(new Vector2(200, 200));
        _soundSources.Add(new Vector2(600, 200));
        _soundSources.Add(new Vector2(400, 450));

        // Define sound paths
        _soundPaths.Add("assets/audio/sfx/mixkit-air-in-a-hit-2161.wav");
        _soundPaths.Add("assets/audio/sfx/mixkit-player-jumping-in-a-video-game-2043.wav");
        _soundPaths.Add("assets/audio/sfx/mixkit-winning-a-coin-video-game-2069.wav");

        // Load sounds
        foreach (var path in _soundPaths)
        {
            try
            {
                _soundClips.Add(_audioCache.Load(path));
            }
            catch
            {
                _logger.Warn(SCENE_NAME, $"Could not load {path}");
                _soundClips.Add(null);
            }
        }
    }

    /// <inheritdoc/>
    public override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);

        if (_inputBackend.IsKeyPressed(Key.Escape))
        {
            PopScene();
            return;
        }

        // Move listener with arrow keys
        var movementX = 0f;
        var movementY = 0f;
        if (_inputBackend.IsKeyDown(Key.Up)) movementY -= LISTENER_SPEED * deltaTime;
        if (_inputBackend.IsKeyDown(Key.Down)) movementY += LISTENER_SPEED * deltaTime;
        if (_inputBackend.IsKeyDown(Key.Left)) movementX -= LISTENER_SPEED * deltaTime;
        if (_inputBackend.IsKeyDown(Key.Right)) movementX += LISTENER_SPEED * deltaTime;

        _listenerPosition = new Vector2(
            _listenerPosition.X + movementX,
            _listenerPosition.Y + movementY
        );
        
        // Clamp listener to screen bounds
        _listenerPosition = new Vector2(
            MathF.Max(50f, MathF.Min(750f, _listenerPosition.X)),
            MathF.Max(50f, MathF.Min(550f, _listenerPosition.Y))
        );

        _audioDevice.SetListenerPosition(_listenerPosition);

        // Continuous loop for each source
        for (int i = 0; i < 3; i++)
        {
            if (_soundClips[i] == null) continue;

            _loopTimers[i] -= deltaTime;
            if (_loopTimers[i] <= 0)
            {
                _loopTimers[i] = _loopIntervals[i];
                _soundPlayer.PlaySoundAtPosition(_soundClips[i]!, _soundSources[i], MAX_DISTANCE);
            }
        }
    }

    /// <inheritdoc/>
    public override void OnRender()
    {
        _renderer.Clear(new Color(10, 15, 20, 255));

        RenderSoundSources();
        RenderListener();
        RenderUI();
    }

    /// <inheritdoc/>
    public override void OnUnload()
    {
        base.OnUnload();
        
        foreach (var path in _soundPaths)
        {
            _audioCache.Unload(path);
        }
        
        _logger?.Info(SCENE_NAME, "Spatial Audio demo unloaded");
    }

    private void RenderSoundSources()
    {
        for (int i = 0; i < _soundSources.Count; i++)
        {
            var source = _soundSources[i];
            
            // Calculate distance and attenuation
            var dx = source.X - _listenerPosition.X;
            var dy = source.Y - _listenerPosition.Y;
            var distance = MathF.Sqrt(dx * dx + dy * dy);
            var attenuation = 1.0f - MathF.Min(distance / MAX_DISTANCE, 1f);

            // Draw attenuation radius
            _renderer.DrawCircle(source, MAX_DISTANCE, new Color(100, 100, 150, 20));

            // Draw distance line
            _renderer.DrawLine(_listenerPosition, source, new Color(150, 150, 150, 100));

            // Draw sound source
            var sourceColor = new Color(255, 200, 100, 255);
            _renderer.DrawCircle(source, 20, sourceColor);
            _renderer.DrawText($"S{i + 1}", new Vector2(source.X - 8, source.Y - 8), 14, Color.White);

            // Draw label and info
            var labelPos = new Vector2(source.X - 30, source.Y - 40);
            _renderer.DrawText($"Source {i + 1}", labelPos, 12, Color.White);
            
            var infoPos = new Vector2(source.X - 30, source.Y + 25);
            _renderer.DrawText($"Dist: {distance:F0}px", infoPos, 10, new Color(200, 200, 200, 255));
            _renderer.DrawText($"Vol: {attenuation * 100:F0}%", new Vector2(infoPos.X, infoPos.Y + 12), 10, new Color(200, 200, 200, 255));
        }
    }

    private void RenderListener()
    {
        // Draw listener
        _renderer.DrawCircle(_listenerPosition, 25, new Color(100, 150, 255, 255));
        _renderer.DrawText("L", new Vector2(_listenerPosition.X - 8, _listenerPosition.Y - 10), 20, Color.White);
        
        var labelPos = new Vector2(_listenerPosition.X - 25, _listenerPosition.Y - 45);
        _renderer.DrawText("Listener", labelPos, 12, Color.White);
    }

    private void RenderUI()
    {
        var layout = new TextLayoutHelper(startX: 10, startY: 10, defaultLineHeight: 20);
        var infoColor = new Color(200, 200, 200, 255);
        var dimColor = new Color(150, 150, 150, 255);

        layout.DrawText(_renderer, "Spatial Audio Demo", 20, Color.White)
              .AddSpacing(5)
              .DrawText(_renderer, $"Max Distance: {MAX_DISTANCE}px", 14, dimColor)
              .AddSpacing(10)
              .DrawText(_renderer, "How it works:", 16, Color.White)
              .DrawText(_renderer, "- Each source plays a DIFFERENT sound", 12, dimColor)
              .DrawText(_renderer, "- Sounds loop continuously", 12, dimColor)
              .DrawText(_renderer, "- Volume decreases with distance", 12, dimColor)
              .DrawText(_renderer, "- Move listener (blue) with arrow keys", 12, dimColor)
              .DrawText(_renderer, "- Get close to hear each sound clearly", 12, dimColor);

        var soundsLoaded = _soundClips.Count(c => c != null);
        if (soundsLoaded == 0)
        {
            layout.AddSpacing(10)
                  .DrawText(_renderer, "[!] No audio files loaded", 14, new Color(255, 200, 100, 255))
                  .DrawText(_renderer, "Demo works without audio", 12, dimColor);
        }
        else
        {
            layout.AddSpacing(10)
                  .DrawText(_renderer, $"[OK] {soundsLoaded}/3 sounds loaded", 14, new Color(100, 255, 100, 255))
                  .DrawText(_renderer, "Sounds playing in loop", 12, dimColor);
        }

        // Legend
        layout.AddSpacing(10)
              .DrawText(_renderer, "Legend:", 16, Color.White)
              .DrawText(_renderer, "[O] Listener (Blue) - Move with arrows", 12, new Color(100, 150, 255, 255))
              .DrawText(_renderer, "[O] Sound Source (Orange)", 12, new Color(255, 200, 100, 255))
              .DrawText(_renderer, "[o] Attenuation Radius", 12, new Color(100, 100, 150, 255))
              .DrawText(_renderer, "[-] Distance Line", 12, new Color(150, 150, 150, 255));

        // Controls
        layout.SetY(500)
              .DrawText(_renderer, "Controls:", 16, Color.White)
              .DrawText(_renderer, "[Arrow Keys] Move Listener", 14, dimColor)
              .DrawText(_renderer, "[ESC] Menu", 14, dimColor);
    }
}

