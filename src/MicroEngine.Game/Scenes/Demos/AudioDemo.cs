using MicroEngine.Core.Graphics;
using MicroEngine.Core.Input;
using MicroEngine.Core.Logging;
using MicroEngine.Core.Math;
using MicroEngine.Core.Scenes;

namespace MicroEngine.Game.Scenes.Demos;

/// <summary>
/// Demonstrates audio system concepts with simulated playback.
/// Shows volume control, playback state, and audio feedback UI.
/// </summary>
public sealed class AudioDemo : Scene
{
    private IInputBackend _inputBackend = null!;
    private IRenderBackend2D _renderBackend = null!;
    private ILogger _logger = null!;

    private float _musicVolume;
    private float _sfxVolume;
    private bool _isMusicPlaying;
    private string _lastSoundPlayed = "None";
    private float _soundFeedbackTimer;
    private const float FEEDBACK_DURATION = 0.5f;

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioDemo"/> class.
    /// </summary>
    public AudioDemo()
        : base("AudioDemo")
    {
        _musicVolume = 0.75f;
        _sfxVolume = 0.9f;
    }

    /// <inheritdoc/>
    public override void OnLoad(SceneContext context)
    {
        base.OnLoad(context);
        _inputBackend = context.InputBackend;
        _renderBackend = context.RenderBackend;
        _logger = context.Logger;
        _logger.Info("AudioDemo", "Audio demo loaded (simulated)");
    }

    /// <inheritdoc/>
    public override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);

        // Early exit if not loaded yet (can happen during scene preloading)
        if (_inputBackend == null)
        {
            return;
        }

        if (_inputBackend.IsKeyPressed(Key.Escape))
        {
            _isMusicPlaying = false;
            PopScene();
            return;
        }

        // Music controls
        if (_inputBackend.IsKeyPressed(Key.Space))
        {
            ToggleMusic();
        }

        if (_inputBackend.IsKeyPressed(Key.Up))
        {
            _musicVolume = System.Math.Min(1.0f, _musicVolume + 0.1f);
            UpdateMusicVolume();
        }

        if (_inputBackend.IsKeyPressed(Key.Down))
        {
            _musicVolume = System.Math.Max(0.0f, _musicVolume - 0.1f);
            UpdateMusicVolume();
        }

        // Sound effect controls
        if (_inputBackend.IsKeyPressed(Key.J))
        {
            PlaySound("Jump");
        }

        if (_inputBackend.IsKeyPressed(Key.C))
        {
            PlaySound("Collect");
        }

        if (_inputBackend.IsKeyPressed(Key.H))
        {
            PlaySound("Hit");
        }

        // SFX volume controls
        if (_inputBackend.IsKeyPressed(Key.Right))
        {
            _sfxVolume = System.Math.Min(1.0f, _sfxVolume + 0.1f);
            _logger.Info("AudioDemo", $"SFX Volume: {_sfxVolume:P0}");
        }

        if (_inputBackend.IsKeyPressed(Key.Left))
        {
            _sfxVolume = System.Math.Max(0.0f, _sfxVolume - 0.1f);
            _logger.Info("AudioDemo", $"SFX Volume: {_sfxVolume:P0}");
        }

        // Update sound feedback timer
        if (_soundFeedbackTimer > 0)
        {
            _soundFeedbackTimer -= deltaTime;
        }
    }

    /// <inheritdoc/>
    public override void OnRender()
    {
        // Early exit if not loaded yet (can happen during scene preloading)
        if (_renderBackend == null)
        {
            return;
        }

        _renderBackend.Clear(new Color(25, 30, 40, 255));

        // Title
        _renderBackend.DrawText("Audio Demo", new Vector2(300, 30), 24, Color.White);

        // Music controls
        _renderBackend.DrawText("Music Controls:", new Vector2(50, 100), 18, new Color(180, 220, 255, 255));
        _renderBackend.DrawText("[SPACE] Play/Pause Music", new Vector2(70, 130), 16, Color.White);
        _renderBackend.DrawText($"[↑/↓] Music Volume: {_musicVolume:P0}", new Vector2(70, 155), 16, Color.White);
        
        var musicStatus = _isMusicPlaying ? "Playing" : "Stopped";
        var musicColor = _isMusicPlaying ? new Color(100, 255, 100, 255) : new Color(255, 100, 100, 255);
        _renderBackend.DrawText($"Status: {musicStatus}", new Vector2(70, 180), 16, musicColor);

        // Music volume bar
        DrawVolumeBar(new Vector2(70, 210), _musicVolume, new Color(100, 150, 255, 255));

        // Sound effects controls
        _renderBackend.DrawText("Sound Effects:", new Vector2(50, 270), 18, new Color(255, 220, 180, 255));
        _renderBackend.DrawText("[J] Jump Sound", new Vector2(70, 300), 16, Color.White);
        _renderBackend.DrawText("[C] Collect Sound", new Vector2(70, 325), 16, Color.White);
        _renderBackend.DrawText("[H] Hit Sound", new Vector2(70, 350), 16, Color.White);
        _renderBackend.DrawText($"[←/→] SFX Volume: {_sfxVolume:P0}", new Vector2(70, 375), 16, Color.White);

        // SFX volume bar
        DrawVolumeBar(new Vector2(70, 405), _sfxVolume, new Color(255, 180, 100, 255));

        // Last sound played feedback
        if (_soundFeedbackTimer > 0)
        {
            var alpha = (byte)(255 * (_soundFeedbackTimer / FEEDBACK_DURATION));
            _renderBackend.DrawText($"Played: {_lastSoundPlayed}", new Vector2(70, 440), 18, new Color(100, 255, 100, alpha));
        }

        // Instructions
        _renderBackend.DrawText("Note: Audio backend integration required for actual playback", 
            new Vector2(50, 510), 12, new Color(150, 150, 150, 255));
        _renderBackend.DrawText("[ESC] Back to Menu", new Vector2(10, 580), 14, new Color(150, 150, 150, 255));
    }

    /// <inheritdoc/>
    public override void OnUnload()
    {
        base.OnUnload();
        _logger?.Info("AudioDemo", "Audio demo unloaded");
    }

    private void ToggleMusic()
    {
        _isMusicPlaying = !_isMusicPlaying;
        _logger.Info("AudioDemo", $"Music {(_isMusicPlaying ? "started" : "stopped")} (simulated)");
    }

    private void UpdateMusicVolume()
    {
        _logger.Info("AudioDemo", $"Music Volume: {_musicVolume:P0}");
    }

    private void PlaySound(string soundName)
    {
        _lastSoundPlayed = soundName;
        _soundFeedbackTimer = FEEDBACK_DURATION;
        _logger.Info("AudioDemo", $"Played {soundName} sound at {_sfxVolume:P0} volume (simulated)");
    }

    private void DrawVolumeBar(Vector2 position, float volume, Color color)
    {
        const float barWidth = 300f;
        const float barHeight = 20f;

        // Background
        _renderBackend.DrawRectangle(position, new Vector2(barWidth, barHeight), new Color(50, 50, 50, 255));

        // Filled portion
        if (volume > 0)
        {
            _renderBackend.DrawRectangle(position, new Vector2(barWidth * volume, barHeight), color);
        }

        // Border
        _renderBackend.DrawRectangle(position, new Vector2(barWidth, barHeight), Color.White);
    }
}
