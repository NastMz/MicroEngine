# Audio Backend Module

**Module:** Engine.Backend.Audio  
**Status:** Active  
**Version:** 1.0  
**Last Updated:** November 2025

---

## Overview

The Audio Backend module provides a unified, backend-agnostic interface for audio playback in MicroEngine.

It supports:

- **Sound effects** (short, one-shot sounds)
- **Music streaming** (long background music)
- **3D spatial audio** (positional sound with falloff)
- **Audio mixing** (multiple simultaneous sounds)
- **Volume control** (master, music, SFX channels)
- **Audio playback control** (play, pause, stop, loop)
- **Multiple backends** (Raylib, OpenAL, FMOD, etc.)

The audio system is designed to be:

- **Platform-independent:** Core logic never depends on specific audio libraries
- **Performant:** Efficient streaming and mixing
- **Flexible:** Easy to swap backends
- **Feature-rich:** Support for advanced audio features

---

## Table of Contents

1. [Core Concepts](#core-concepts)
2. [Architecture](#architecture)
3. [IAudioBackend Interface](#iaudiobackend-interface)
4. [Sound Effects](#sound-effects)
5. [Music Streaming](#music-streaming)
6. [3D Spatial Audio](#3d-spatial-audio)
7. [Volume and Mixing](#volume-and-mixing)
8. [Audio Resources](#audio-resources)
9. [Backend Implementations](#backend-implementations)
10. [Usage Examples](#usage-examples)
11. [Best Practices](#best-practices)
12. [API Reference](#api-reference)

---

## Core Concepts

### What is an Audio Backend?

An audio backend is a concrete implementation of the `IAudioBackend` interface that handles audio playback using
platform-specific audio libraries.

### Sound vs Music

**Sound Effects (SFX):**

- Short audio clips (< 10 seconds)
- Loaded entirely into memory
- Low latency playback
- Multiple instances can play simultaneously
- Examples: jump, explosion, footstep

**Music:**

- Long audio files (> 10 seconds)
- Streamed from disk
- Higher latency acceptable
- Typically one instance playing
- Examples: background music, ambient sounds

### Audio Channels

Audio is organized into channels for volume control:

- **Master:** Global volume multiplier
- **Music:** Background music and ambience
- **SFX:** Sound effects
- **Voice:** Dialog and speech (future)

---

## Architecture

### Class Diagram

```
IAudioBackend (interface)
├── Sound playback
├── Music streaming
├── Volume control
├── 3D audio
└── Resource management

Implementations:
├── RaylibAudioBackend
├── OpenALAudioBackend (future)
└── FMODAudioBackend (future)
```

### Core Interfaces

#### IAudioBackend

Main audio interface.

```csharp
public interface IAudioBackend : IDisposable
{
    void Initialize();
    void Update();

    // Sound effects
    void PlaySound(Sound sound, float volume = 1.0f);
    void StopSound(Sound sound);
    bool IsSoundPlaying(Sound sound);

    // Music
    void PlayMusic(Music music, bool loop = true);
    void StopMusic();
    void PauseMusic();
    void ResumeMusic();
    bool IsMusicPlaying();

    // Volume
    void SetMasterVolume(float volume);
    void SetMusicVolume(float volume);
    void SetSFXVolume(float volume);

    // 3D audio
    void SetListenerPosition(Vector3 position);
    void SetListenerOrientation(Vector3 forward, Vector3 up);
    void PlaySound3D(Sound sound, Vector3 position, float volume = 1.0f);
}
```

---

## IAudioBackend Interface

### Initialization

Initialize the audio system:

```csharp
var audioBackend = new RaylibAudioBackend();
audioBackend.Initialize();
```

### Update Loop

Update the audio system every frame:

```csharp
public void GameLoop()
{
    while (running)
    {
        audioBackend.Update(); // Update streaming, 3D audio, etc.

        world.Update(deltaTime);
        renderer.Render();
    }
}
```

### Cleanup

Dispose of audio resources:

```csharp
audioBackend.Dispose();
```

---

## Sound Effects

### Loading Sounds

```csharp
public override void OnLoad(SceneContext context)
{
    base.OnLoad(context);
    
    var jumpSound = context.AudioCache.Load("sounds/jump.wav");
    var explosionSound = context.AudioCache.Load("sounds/explosion.wav");
}
```

### Playing Sounds

```csharp
// Simple playback
audioBackend.PlaySound(jumpSound);

// With volume
audioBackend.PlaySound(explosionSound, volume: 0.8f);
```

### Stopping Sounds

```csharp
audioBackend.StopSound(jumpSound);
```

### Sound State

```csharp
if (audioBackend.IsSoundPlaying(footstepSound))
{
    // Sound is still playing
}
```

### Overlapping Sounds

Multiple instances of the same sound can play simultaneously:

```csharp
// Both will play
audioBackend.PlaySound(coinSound);
audioBackend.PlaySound(coinSound); // Second instance
```

### Sound Properties

```csharp
public class Sound : IResource
{
    public float Duration { get; }
    public int SampleRate { get; }
    public int Channels { get; }
    public AudioFormat Format { get; }
}
```

---

## Music Streaming

### Loading Music

```csharp
var backgroundMusic = ResourceManager.Load<Music>("music/level1.ogg");
```

### Playing Music

```csharp
// Play once
audioBackend.PlayMusic(backgroundMusic, loop: false);

// Loop indefinitely
audioBackend.PlayMusic(backgroundMusic, loop: true);
```

### Music Control

#### Pause / Resume

```csharp
audioBackend.PauseMusic();
audioBackend.ResumeMusic();
```

#### Stop

```csharp
audioBackend.StopMusic();
```

#### Check State

```csharp
if (audioBackend.IsMusicPlaying())
{
    // Music is playing
}
```

### Music Transitions

Smooth transitions between music tracks:

```csharp
public class MusicManager
{
    private Music _currentMusic;
    private float _fadeOutDuration = 1.0f;

    public void TransitionTo(Music newMusic)
    {
        if (_currentMusic != null)
        {
            FadeOut(_currentMusic, _fadeOutDuration, () =>
            {
                audioBackend.StopMusic();
                audioBackend.PlayMusic(newMusic, loop: true);
                FadeIn(newMusic, _fadeOutDuration);
            });
        }
        else
        {
            audioBackend.PlayMusic(newMusic, loop: true);
        }

        _currentMusic = newMusic;
    }
}
```

### Music Properties

```csharp
public class Music : IResource
{
    public float Duration { get; }
    public float CurrentTime { get; set; }
    public bool IsLooping { get; set; }
}
```

---

## 2D Spatial Audio

### Listener Setup

Set the listener position (usually the camera or player) to enable spatial calculations:

```csharp
public class AudioListenerSystem : ISystem
{
    private readonly IAudioBackend _audio;

    public void Update(World world, float deltaTime)
    {
        var camera = world.Query<CameraComponent, TransformComponent>().First();
        var transform = camera.GetComponent<TransformComponent>();

        // Update listener position for 2D spatial audio
        _audio.SetListenerPosition(transform.Position);
    }
}
```

### Playing Spatial Sounds

Play sounds at specific 2D coordinates with distance-based attenuation:

```csharp
// Play sound at specific position
var explosionPos = new Vector2(100, 50);
float maxDistance = 500f; // Sound becomes inaudible beyond this distance

_audio.PlaySoundAtPosition(explosionSound, explosionPos, maxDistance);
```

### Distance Attenuation

The backend automatically calculates volume based on the distance between the listener and the sound source.

-   **Linear Attenuation:** Volume drops linearly from 1.0 (at source) to 0.0 (at maxDistance).
-   **Max Distance:** The radius at which the sound becomes completely silent.

```csharp
// Internal logic example
float distance = Vector2.Distance(listenerPos, soundPos);
float volume = 1.0f - Math.Clamp(distance / maxDistance, 0f, 1f);
```

### Best Practices

1.  **Update Listener Every Frame:** Ensure `SetListenerPosition` is called in your update loop (e.g., in a `CameraSystem` or `AudioListenerSystem`).
2.  **Tune Max Distance:** Adjust `maxDistance` based on the importance of the sound. Loud explosions should have a larger range than footsteps.
3.  **Use Mono Sounds:** Spatial audio works best with mono sound files, as the backend handles the stereo panning and volume.

---

## Volume and Mixing

### Master Volume

Global volume multiplier:

```csharp
audioBackend.SetMasterVolume(0.8f); // 80%
```

### Channel Volumes

Independent control for music and SFX:

```csharp
audioBackend.SetMusicVolume(0.7f);
audioBackend.SetSFXVolume(1.0f);
```

### Volume Hierarchy

```
Final Volume = Master × Channel × Sound Volume

Example:
Master = 0.8
Music Channel = 0.7
Sound Volume = 1.0
Final = 0.8 × 0.7 × 1.0 = 0.56 (56%)
```

### Volume Fading

```csharp
public class AudioFader
{
    private float _targetVolume;
    private float _fadeTime;
    private float _elapsed;

    public void FadeTo(float target, float duration)
    {
        _targetVolume = target;
        _fadeTime = duration;
        _elapsed = 0;
    }

    public void Update(float deltaTime)
    {
        if (_elapsed < _fadeTime)
        {
            _elapsed += deltaTime;
            var t = _elapsed / _fadeTime;
            var currentVolume = Mathf.Lerp(_startVolume, _targetVolume, t);
            audioBackend.SetMusicVolume(currentVolume);
        }
    }
}
```

### Audio Settings

```csharp
public class AudioSettings
{
    public float MasterVolume { get; set; } = 1.0f;
    public float MusicVolume { get; set; } = 0.7f;
    public float SFXVolume { get; set; } = 1.0f;
    public bool Muted { get; set; } = false;

    public void Apply(IAudioBackend audio)
    {
        if (Muted)
        {
            audio.SetMasterVolume(0);
        }
        else
        {
            audio.SetMasterVolume(MasterVolume);
            audio.SetMusicVolume(MusicVolume);
            audio.SetSFXVolume(SFXVolume);
        }
    }
}
```

---

## Audio Resources

### Loading Audio

```csharp
// Load sound effect
var sound = ResourceManager.Load<Sound>("sounds/coin.wav");

// Load music
var music = ResourceManager.Load<Music>("music/background.ogg");
```

### Supported Formats

**Sounds:**

- WAV (uncompressed, low latency)
- OGG (compressed, smaller files)
- MP3 (compressed, good compatibility)

**Music:**

- OGG (recommended, good quality/size ratio)
- MP3 (widely supported)
- FLAC (lossless, large files)

### Resource Management

```csharp
public class GameplayScene : Scene
{
    private Sound _jumpSound;
    private Music _levelMusic;

    public override void OnLoad(SceneContext context)
    {
        base.OnLoad(context);
        
        _jumpSound = context.AudioCache.Load("sounds/jump.wav");
        _levelMusic = context.AudioCache.Load("music/level1.ogg");

        context.MusicPlayer.Play(_levelMusic, loop: true);
    }

    public override void OnUnload()
    {
        Context.MusicPlayer.Stop();
        base.OnUnload();
    }
}
```

---

## Backend Implementations

### Raylib Audio Backend

```csharp
public class RaylibAudioBackend : IAudioBackend
{
    public void Initialize()
    {
        Raylib.InitAudioDevice();
    }

    public void PlaySound(Sound sound, float volume)
    {
        Raylib.SetSoundVolume(sound.Handle, volume);
        Raylib.PlaySound(sound.Handle);
    }

    public void PlayMusic(Music music, bool loop)
    {
        Raylib.PlayMusicStream(music.Handle);
        music.IsLooping = loop;
    }

    public void Update()
    {
        if (_currentMusic != null)
        {
            Raylib.UpdateMusicStream(_currentMusic.Handle);
        }
    }

    public void Dispose()
    {
        Raylib.CloseAudioDevice();
    }
}
```

### Custom Audio Backend

```csharp
public class CustomAudioBackend : IAudioBackend
{
    private readonly AudioEngine _engine;
    private readonly List<SoundInstance> _playingSounds;

    public void PlaySound(Sound sound, float volume)
    {
        var instance = _engine.CreateInstance(sound);
        instance.Volume = volume * _sfxVolume * _masterVolume;
        instance.Play();

        _playingSounds.Add(instance);
    }

    public void Update()
    {
        // Remove finished sounds
        _playingSounds.RemoveAll(s => !s.IsPlaying);

        // Update music streaming
        _engine.Update();
    }
}
```

---

## Usage Examples

### Example 1: Simple Sound Playback

```csharp
public class PlayerJumpSystem : ISystem
{
    private readonly IAudioBackend _audio;
    private readonly Sound _jumpSound;

    public PlayerJumpSystem(IAudioBackend audio)
    {
        _audio = audio;
        _jumpSound = ResourceManager.Load<Sound>("sounds/jump.wav");
    }

    public void Update(World world, float deltaTime)
    {
        var players = world.Query<PlayerComponent, VelocityComponent>();

        foreach (var player in players)
        {
            var velocity = player.GetComponent<VelocityComponent>();

            if (InputManager.IsKeyPressed(Key.Space) && player.IsGrounded)
            {
                velocity.Value.Y = -500;
                _audio.PlaySound(_jumpSound);
            }
        }
    }
}
```

### Example 2: Background Music Management

```csharp
public class MusicManager
{
    private readonly IAudioBackend _audio;
    private Music _currentMusic;

    public void PlayLevelMusic(int levelNumber)
    {
        var musicPath = $"music/level{levelNumber}.ogg";
        var music = ResourceManager.Load<Music>(musicPath);

        if (_currentMusic != null)
        {
            _audio.StopMusic();
            ResourceManager.Unload(_currentMusic);
        }

        _audio.PlayMusic(music, loop: true);
        _currentMusic = music;
    }

    public void StopMusic()
    {
        _audio.StopMusic();

        if (_currentMusic != null)
        {
            ResourceManager.Unload(_currentMusic);
            _currentMusic = null;
        }
    }
}
```

### Example 3: 3D Positional Audio

```csharp
public class AudioEmitterSystem : ISystem
{
    private readonly IAudioBackend _audio;

    public void Update(World world, float deltaTime)
    {
        // Update listener position
        var camera = world.Query<CameraComponent, TransformComponent>().First();
        var cameraTransform = camera.GetComponent<TransformComponent>();
        _audio.SetListenerPosition(cameraTransform.Position);

        // Play sounds from emitters
        var emitters = world.Query<AudioEmitterComponent, TransformComponent>();

        foreach (var emitter in emitters)
        {
            var transform = emitter.GetComponent<TransformComponent>();
            var audioEmitter = emitter.GetComponent<AudioEmitterComponent>();

            if (audioEmitter.ShouldPlay)
            {
                _audio.PlaySound3D(
                    audioEmitter.Sound,
                    transform.Position,
                    volume: audioEmitter.Volume
                );

                audioEmitter.ShouldPlay = false;
            }
        }
    }
}
```

### Example 4: Audio Settings Menu

```csharp
public class AudioSettingsUI
{
    private readonly IAudioBackend _audio;
    private readonly AudioSettings _settings;

    public void RenderUI()
    {
        // Master volume slider
        _settings.MasterVolume = UI.Slider("Master Volume", _settings.MasterVolume, 0, 1);

        // Music volume slider
        _settings.MusicVolume = UI.Slider("Music Volume", _settings.MusicVolume, 0, 1);

        // SFX volume slider
        _settings.SFXVolume = UI.Slider("SFX Volume", _settings.SFXVolume, 0, 1);

        // Mute toggle
        _settings.Muted = UI.Toggle("Mute", _settings.Muted);

        // Apply settings
        _settings.Apply(_audio);

        // Test button
        if (UI.Button("Test SFX"))
        {
            var testSound = ResourceManager.Load<Sound>("sounds/test.wav");
            _audio.PlaySound(testSound);
        }
    }
}
```

### Example 5: Footstep System

```csharp
public class FootstepSystem : ISystem
{
    private readonly IAudioBackend _audio;
    private readonly Sound[] _footstepSounds;
    private float _timeSinceLastStep;
    private const float StepInterval = 0.4f;

    public FootstepSystem(IAudioBackend audio)
    {
        _audio = audio;
        _footstepSounds = new[]
        {
            ResourceManager.Load<Sound>("sounds/footstep1.wav"),
            ResourceManager.Load<Sound>("sounds/footstep2.wav"),
            ResourceManager.Load<Sound>("sounds/footstep3.wav")
        };
    }

    public void Update(World world, float deltaTime)
    {
        var players = world.Query<PlayerComponent, VelocityComponent>();

        foreach (var player in players)
        {
            var velocity = player.GetComponent<VelocityComponent>();
            var isMoving = velocity.Value.LengthSquared() > 0.1f;

            if (isMoving && player.IsGrounded)
            {
                _timeSinceLastStep += deltaTime;

                if (_timeSinceLastStep >= StepInterval)
                {
                    var randomSound = _footstepSounds[Random.Next(_footstepSounds.Length)];
                    _audio.PlaySound(randomSound, volume: 0.3f);
                    _timeSinceLastStep = 0;
                }
            }
            else
            {
                _timeSinceLastStep = 0;
            }
        }
    }
}
```

---

## Best Practices

### Do's

- ✓ Use `IAudioBackend` interface, never concrete implementations in core
- ✓ Load sounds in scene `OnEnter`, unload in `OnExit`
- ✓ Use SFX for short sounds, music streaming for long tracks
- ✓ Implement volume controls for accessibility
- ✓ Provide mute option
- ✓ Use 3D audio for spatial immersion
- ✓ Limit simultaneous sounds to prevent audio overload
- ✓ Preload frequently-used sounds

### Don'ts

- ✗ Don't play music as sound effects (use streaming)
- ✗ Don't forget to dispose of audio resources
- ✗ Don't play too many sounds simultaneously (causes audio clipping)
- ✗ Don't hardcode volume values (use settings)
- ✗ Don't use uncompressed WAV for music (too large)
- ✗ Don't ignore audio backend errors
- ✗ Don't update music stream if backend handles it automatically

### Audio Organization

```
assets/
└── sounds/
    ├── music/
    │   ├── menu.ogg
    │   ├── level1.ogg
    │   └── boss.ogg
    ├── sfx/
    │   ├── player/
    │   │   ├── jump.wav
    │   │   ├── footstep1.wav
    │   │   └── hurt.wav
    │   ├── enemies/
    │   │   ├── explosion.wav
    │   │   └── laser.wav
    │   └── ui/
    │       ├── click.wav
    │       └── hover.wav
    └── ambient/
        ├── wind.ogg
        └── rain.ogg
```

### Performance Tips

**Sound pooling:**

```csharp
public class SoundPool
{
    private readonly Queue<SoundInstance> _pool;

    public void PlaySound(Sound sound)
    {
        var instance = _pool.Count > 0 ? _pool.Dequeue() : CreateInstance();
        instance.Sound = sound;
        instance.Play();
    }
}
```

**Limit simultaneous sounds:**

```csharp
private const int MaxSimultaneousSounds = 32;
private int _playingCount;

public void PlaySound(Sound sound)
{
    if (_playingCount >= MaxSimultaneousSounds)
        return;

    PlaySoundInternal(sound);
    _playingCount++;
}
```

---

## API Reference

### IAudioBackend

```csharp
public interface IAudioBackend : IDisposable
{
    // Lifecycle
    void Initialize();
    void Update();
    void Shutdown();

    // Sound effects
    void PlaySound(Sound sound, float volume = 1.0f);
    void StopSound(Sound sound);
    void StopAllSounds();
    bool IsSoundPlaying(Sound sound);

    // Music
    void PlayMusic(Music music, bool loop = true);
    void StopMusic();
    void PauseMusic();
    void ResumeMusic();
    bool IsMusicPlaying();
    float GetMusicTimePlayed();
    void SetMusicTimePlayed(float time);

    // Volume control
    void SetMasterVolume(float volume);
    void SetMusicVolume(float volume);
    void SetSFXVolume(float volume);
    float GetMasterVolume();
    float GetMusicVolume();
    float GetSFXVolume();

    // 3D audio
    void SetListenerPosition(Vector3 position);
    void SetListenerOrientation(Vector3 forward, Vector3 up);
    void PlaySound3D(Sound sound, Vector3 position, float volume = 1.0f,
        float minDistance = 10.0f, float maxDistance = 100.0f);
}
```

### Sound

```csharp
public class Sound : IResource
{
    public string Path { get; }
    public float Duration { get; }
    public int SampleRate { get; }
    public int Channels { get; }
    public AudioFormat Format { get; }

    public void Dispose();
}
```

### Music

```csharp
public class Music : IResource
{
    public string Path { get; }
    public float Duration { get; }
    public bool IsLooping { get; set; }
    public float CurrentTime { get; set; }

    public void Dispose();
}
```

---

## Related Documentation

- [Architecture](../ARCHITECTURE.md)
- [Resources Module](RESOURCES.md)
- [ECS Module](ECS.md)
- [Graphics Backend](GRAPHICS_BACKEND.md)

---

**Last Updated:** November 2025  
**Version:** 1.0
