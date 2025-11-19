using MicroEngine.Core.Audio;
using MicroEngine.Core.Resources;

namespace MicroEngine.Backend.Raylib;

/// <summary>
/// Raylib implementation of IAudioBackend.
/// </summary>
public sealed class RaylibAudioBackend : IAudioBackend
{
    private const string INVALID_MUSIC_ERROR = "Music must be a streaming RaylibAudioClip";
    private const string INVALID_SOUND_ERROR = "Sound must be a non-streaming RaylibAudioClip";

    private bool _initialized;

    /// <inheritdoc/>
    public void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        Raylib_cs.Raylib.InitAudioDevice();
        _initialized = true;
    }

    /// <inheritdoc/>
    public void Shutdown()
    {
        if (!_initialized)
        {
            return;
        }

        Raylib_cs.Raylib.CloseAudioDevice();
        _initialized = false;
    }

    /// <inheritdoc/>
    public void PlaySound(IAudioClip sound)
    {
        if (sound is not Resources.RaylibAudioClip raylibClip || raylibClip.IsStreaming)
        {
            throw new ArgumentException(INVALID_SOUND_ERROR, nameof(sound));
        }

        var nativeSound = raylibClip.NativeSound;
        if (nativeSound.HasValue)
        {
            Raylib_cs.Raylib.PlaySound(nativeSound.Value);
        }
    }

    /// <inheritdoc/>
    public void StopSound(IAudioClip sound)
    {
        if (sound is not Resources.RaylibAudioClip raylibClip || raylibClip.IsStreaming)
        {
            throw new ArgumentException(INVALID_SOUND_ERROR, nameof(sound));
        }

        var nativeSound = raylibClip.NativeSound;
        if (nativeSound.HasValue)
        {
            Raylib_cs.Raylib.StopSound(nativeSound.Value);
        }
    }

    /// <inheritdoc/>
    public void SetSoundVolume(IAudioClip sound, float volume)
    {
        if (sound is not Resources.RaylibAudioClip raylibClip || raylibClip.IsStreaming)
        {
            throw new ArgumentException(INVALID_SOUND_ERROR, nameof(sound));
        }

        var nativeSound = raylibClip.NativeSound;
        if (nativeSound.HasValue)
        {
            Raylib_cs.Raylib.SetSoundVolume(nativeSound.Value, volume);
        }
    }

    /// <inheritdoc/>
    public void PlayMusic(IAudioClip music)
    {
        if (music is not Resources.RaylibAudioClip raylibClip || !raylibClip.IsStreaming)
        {
            throw new ArgumentException(INVALID_MUSIC_ERROR, nameof(music));
        }

        var nativeMusic = raylibClip.NativeMusic;
        if (nativeMusic.HasValue)
        {
            Raylib_cs.Raylib.PlayMusicStream(nativeMusic.Value);
        }
    }

    /// <inheritdoc/>
    public void StopMusic(IAudioClip music)
    {
        if (music is not Resources.RaylibAudioClip raylibClip || !raylibClip.IsStreaming)
        {
            throw new ArgumentException(INVALID_MUSIC_ERROR, nameof(music));
        }

        var nativeMusic = raylibClip.NativeMusic;
        if (nativeMusic.HasValue)
        {
            Raylib_cs.Raylib.StopMusicStream(nativeMusic.Value);
        }
    }

    /// <inheritdoc/>
    public void PauseMusic(IAudioClip music)
    {
        if (music is not Resources.RaylibAudioClip raylibClip || !raylibClip.IsStreaming)
        {
            throw new ArgumentException(INVALID_MUSIC_ERROR, nameof(music));
        }

        var nativeMusic = raylibClip.NativeMusic;
        if (nativeMusic.HasValue)
        {
            Raylib_cs.Raylib.PauseMusicStream(nativeMusic.Value);
        }
    }

    /// <inheritdoc/>
    public void ResumeMusic(IAudioClip music)
    {
        if (music is not Resources.RaylibAudioClip raylibClip || !raylibClip.IsStreaming)
        {
            throw new ArgumentException(INVALID_MUSIC_ERROR, nameof(music));
        }

        var nativeMusic = raylibClip.NativeMusic;
        if (nativeMusic.HasValue)
        {
            Raylib_cs.Raylib.ResumeMusicStream(nativeMusic.Value);
        }
    }

    /// <inheritdoc/>
    public void UpdateMusic(IAudioClip music)
    {
        if (music is not Resources.RaylibAudioClip raylibClip || !raylibClip.IsStreaming)
        {
            throw new ArgumentException(INVALID_MUSIC_ERROR, nameof(music));
        }

        var nativeMusic = raylibClip.NativeMusic;
        if (nativeMusic.HasValue)
        {
            Raylib_cs.Raylib.UpdateMusicStream(nativeMusic.Value);
        }
    }

    /// <inheritdoc/>
    public bool IsMusicPlaying(IAudioClip music)
    {
        if (music is not Resources.RaylibAudioClip raylibClip || !raylibClip.IsStreaming)
        {
            throw new ArgumentException(INVALID_MUSIC_ERROR, nameof(music));
        }

        var nativeMusic = raylibClip.NativeMusic;
        return nativeMusic.HasValue && Raylib_cs.Raylib.IsMusicStreamPlaying(nativeMusic.Value);
    }

    /// <inheritdoc/>
    public void SetMasterVolume(float volume)
    {
        Raylib_cs.Raylib.SetMasterVolume(System.Math.Clamp(volume, 0f, 1f));
    }

    /// <inheritdoc/>
    public float GetMasterVolume()
    {
        // Raylib doesn't provide a way to get master volume, so we can't implement this
        return 1.0f;
    }

    /// <inheritdoc/>
    public void SetMusicVolume(IAudioClip music, float volume)
    {
        if (music is not Resources.RaylibAudioClip raylibClip || !raylibClip.IsStreaming)
        {
            throw new ArgumentException(INVALID_MUSIC_ERROR, nameof(music));
        }

        var nativeMusic = raylibClip.NativeMusic;
        if (nativeMusic.HasValue)
        {
            Raylib_cs.Raylib.SetMusicVolume(nativeMusic.Value, System.Math.Clamp(volume, 0f, 1f));
        }
    }
}
