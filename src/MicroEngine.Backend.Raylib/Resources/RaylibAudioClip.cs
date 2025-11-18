using MicroEngine.Core.Resources;

namespace MicroEngine.Backend.Raylib.Resources;

/// <summary>
/// Raylib implementation of IAudioClip.
/// </summary>
internal sealed class RaylibAudioClip : IAudioClip
{
    private readonly Raylib_cs.Sound _sound;
    private readonly Raylib_cs.Music _music;
    private readonly bool _isStreaming;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance for a sound effect.
    /// </summary>
    public RaylibAudioClip(ResourceId id, string path, Raylib_cs.Sound sound)
    {
        Id = id;
        Path = path;
        _sound = sound;
        _isStreaming = false;
        IsLoaded = true;
    }

    /// <summary>
    /// Initializes a new instance for streaming music.
    /// </summary>
    public RaylibAudioClip(ResourceId id, string path, Raylib_cs.Music music)
    {
        Id = id;
        Path = path;
        _music = music;
        _isStreaming = true;
        IsLoaded = true;
    }

    /// <inheritdoc/>
    public ResourceId Id { get; }

    /// <inheritdoc/>
    public string Path { get; }

    /// <inheritdoc/>
    public bool IsLoaded { get; private set; }

    /// <inheritdoc/>
    public long SizeInBytes => _isStreaming ? 1024 * 1024 : 512 * 1024; // Rough estimate

    /// <inheritdoc/>
    public float Duration => _isStreaming 
        ? Raylib_cs.Raylib.GetMusicTimeLength(_music) 
        : 0f; // Sound duration not available in Raylib

    /// <inheritdoc/>
    public int SampleRate => _isStreaming ? (int)_music.Stream.SampleRate : (int)_sound.Stream.SampleRate;

    /// <inheritdoc/>
    public int Channels => _isStreaming ? (int)_music.Stream.Channels : (int)_sound.Stream.Channels;

    /// <inheritdoc/>
    public bool IsStreaming => _isStreaming;

    /// <summary>
    /// Gets the underlying Raylib sound (if not streaming).
    /// </summary>
    internal Raylib_cs.Sound? NativeSound => !_isStreaming ? _sound : null;

    /// <summary>
    /// Gets the underlying Raylib music (if streaming).
    /// </summary>
    internal Raylib_cs.Music? NativeMusic => _isStreaming ? _music : null;

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (IsLoaded)
        {
            if (_isStreaming)
            {
                Raylib_cs.Raylib.UnloadMusicStream(_music);
            }
            else
            {
                Raylib_cs.Raylib.UnloadSound(_sound);
            }
            IsLoaded = false;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
