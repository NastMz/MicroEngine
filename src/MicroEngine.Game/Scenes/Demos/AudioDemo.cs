using MicroEngine.Core.Audio;
using MicroEngine.Core.Graphics;
using MicroEngine.Core.Input;
using MicroEngine.Core.Logging;
using MicroEngine.Core.Math;
using MicroEngine.Core.Resources;
using MicroEngine.Core.Scenes;

namespace MicroEngine.Game.Scenes.Demos;

/// <summary>
/// Demonstrates audio system with real playback.
/// Shows volume control, playback state, and audio feedback UI.
/// </summary>
public sealed class AudioDemo : Scene
{
    private IInputBackend _inputBackend = null!;
    private IRenderer2D _renderer = null!;
    private ILogger _logger = null!;
    private ISoundPlayer _soundPlayer = null!;
    private IMusicPlayer _musicPlayer = null!;
    private ResourceCache<IAudioClip> _audioCache = null!;

    private IAudioClip? _backgroundMusic;
    private IAudioClip? _jumpSound;
    private IAudioClip? _collectSound;
    private IAudioClip? _hitSound;

    private float _musicVolume;
    private float _sfxVolume;
    private bool _isMusicPlaying;
    private string _lastSoundPlayed = "None";
    private float _soundFeedbackTimer;
    private const float FEEDBACK_DURATION = 0.5f;

    // Audio file paths
    private const string MUSIC_PATH = "assets/audio/music/mixkit-sonor-2-570.mp3";
    private const string JUMP_SFX_PATH = "assets/audio/sfx/mixkit-player-jumping-in-a-video-game-2043.wav";
    private const string COLLECT_SFX_PATH = "assets/audio/sfx/mixkit-winning-a-coin-video-game-2069.wav";
    private const string HIT_SFX_PATH = "assets/audio/sfx/mixkit-air-in-a-hit-2161.wav";

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
        _renderer = context.Renderer;
        _logger = context.Logger;
        _soundPlayer = context.SoundPlayer;
        _musicPlayer = context.MusicPlayer;
        _audioCache = context.AudioCache;

        // Load audio resources
        try
        {
            _backgroundMusic = _audioCache.Load(MUSIC_PATH);
            _jumpSound = _audioCache.Load(JUMP_SFX_PATH);
            _collectSound = _audioCache.Load(COLLECT_SFX_PATH);
            _hitSound = _audioCache.Load(HIT_SFX_PATH);
            _logger.Info("AudioDemo", "Audio demo loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.Error("AudioDemo", $"Failed to load audio resources: {ex.Message}");
        }
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
            if (_isMusicPlaying && _backgroundMusic != null)
            {
                _musicPlayer.StopMusic(_backgroundMusic);
            }
            _isMusicPlaying = false;
            PopScene();
            return;
        }

        // Update music stream (required for streaming audio)
        if (_isMusicPlaying && _backgroundMusic != null)
        {
            _musicPlayer.UpdateMusic(_backgroundMusic);
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
            PlaySound("Jump", _jumpSound);
        }

        if (_inputBackend.IsKeyPressed(Key.C))
        {
            PlaySound("Collect", _collectSound);
        }

        if (_inputBackend.IsKeyPressed(Key.H))
        {
            PlaySound("Hit", _hitSound);
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
        if (_renderer == null)
        {
            return;
        }

        _renderer.Clear(new Color(25, 30, 40, 255));

        var layout = new TextLayoutHelper(_renderer, startX: 50, startY: 30, defaultLineHeight: 25);
        var titleColor = new Color(180, 220, 255, 255);
        var sfxTitleColor = new Color(255, 220, 180, 255);
        var dimColor = new Color(150, 150, 150, 255);

        // Title (centered)
        layout.SetX(300)
              .DrawText("Audio Demo", 24, Color.White)
              .SetX(50)
              .AddSpacing(45);

        // Music controls
        layout.DrawSection("Music Controls:", 18, titleColor, spacingAfter: 5)
              .SetX(70)
              .DrawText("[SPACE] Play/Pause Music", 16, Color.White)
              .DrawText($"[ArrowUp/ArrowDown] Music Volume: {_musicVolume:P0}", 16, Color.White);
        
        var musicStatus = _isMusicPlaying ? "Playing" : "Stopped";
        var musicColor = _isMusicPlaying ? new Color(100, 255, 100, 255) : new Color(255, 100, 100, 255);
        layout.DrawText($"Status: {musicStatus}", 16, musicColor);

        // Music volume bar
        DrawVolumeBar(new Vector2(70, layout.CurrentY), _musicVolume, new Color(100, 150, 255, 255));
        layout.AddSpacing(35);

        // Sound effects controls
        layout.SetX(50)
              .DrawSection("Sound Effects:", 18, sfxTitleColor, spacingAfter: 5)
              .SetX(70)
              .DrawText("[J] Jump Sound", 16, Color.White)
              .DrawText("[C] Collect Sound", 16, Color.White)
              .DrawText("[H] Hit Sound", 16, Color.White)
              .DrawText($"[ArrowLeft/ArrowRight] SFX Volume: {_sfxVolume:P0}", 16, Color.White);

        // SFX volume bar
        DrawVolumeBar(new Vector2(70, layout.CurrentY), _sfxVolume, new Color(255, 180, 100, 255));
        layout.AddSpacing(35);

        // Last sound played feedback
        if (_soundFeedbackTimer > 0)
        {
            var alpha = (byte)(255 * (_soundFeedbackTimer / FEEDBACK_DURATION));
            layout.SetX(70)
                  .DrawText($"Played: {_lastSoundPlayed}", 18, new Color(100, 255, 100, alpha));
        }

        // Instructions
        layout.SetX(10)
              .SetY(580)
              .DrawText("[ESC] Back to Menu", 14, dimColor);
    }

    /// <inheritdoc/>
    public override void OnUnload()
    {
        base.OnUnload();

        // Stop music if playing
        if (_isMusicPlaying && _backgroundMusic != null)
        {
            _musicPlayer.StopMusic(_backgroundMusic);
        }

        // Unload audio resources (cache handles disposal)
        if (_backgroundMusic != null)
        {
            _audioCache.Unload(MUSIC_PATH);
        }
        if (_jumpSound != null)
        {
            _audioCache.Unload(JUMP_SFX_PATH);
        }
        if (_collectSound != null)
        {
            _audioCache.Unload(COLLECT_SFX_PATH);
        }
        if (_hitSound != null)
        {
            _audioCache.Unload(HIT_SFX_PATH);
        }

        _logger?.Info("AudioDemo", "Audio demo unloaded");
    }

    private void ToggleMusic()
    {
        if (_backgroundMusic == null)
        {
            _logger.Warn("AudioDemo", "Background music not loaded");
            return;
        }

        _isMusicPlaying = !_isMusicPlaying;

        if (_isMusicPlaying)
        {
            // Check if music is already playing (from previous session)
            if (_musicPlayer.IsMusicPlaying(_backgroundMusic))
            {
                _musicPlayer.ResumeMusic(_backgroundMusic);
            }
            else
            {
                _musicPlayer.PlayMusic(_backgroundMusic);
                _musicPlayer.SetMusicVolume(_backgroundMusic, _musicVolume);
            }
            _logger.Info("AudioDemo", "Music started");
        }
        else
        {
            _musicPlayer.PauseMusic(_backgroundMusic);
            _logger.Info("AudioDemo", "Music paused");
        }
    }

    private void UpdateMusicVolume()
    {
        if (_backgroundMusic != null)
        {
            _musicPlayer.SetMusicVolume(_backgroundMusic, _musicVolume);
        }
        _logger.Info("AudioDemo", $"Music Volume: {_musicVolume:P0}");
    }

    private void PlaySound(string soundName, IAudioClip? sound)
    {
        if (sound == null)
        {
            _logger.Warn("AudioDemo", $"{soundName} sound not loaded");
            return;
        }

        _lastSoundPlayed = soundName;
        _soundFeedbackTimer = FEEDBACK_DURATION;

        // Set volume for this specific sound
        _soundPlayer.SetSoundVolume(sound, _sfxVolume);
        _soundPlayer.PlaySound(sound);

        _logger.Info("AudioDemo", $"Played {soundName} sound at {_sfxVolume:P0} volume");
    }

    private void DrawVolumeBar(Vector2 position, float volume, Color color)
    {
        const float barWidth = 300f;
        const float barHeight = 20f;

        // Background
        _renderer.DrawRectangle(position, new Vector2(barWidth, barHeight), new Color(50, 50, 50, 255));

        // Filled portion
        if (volume > 0)
        {
            _renderer.DrawRectangle(position, new Vector2(barWidth * volume, barHeight), color);
        }

        // Border
        _renderer.DrawRectangleLines(position, new Vector2(barWidth, barHeight), Color.White);
    }
}
