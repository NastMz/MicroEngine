using MicroEngine.Core.Resources;

namespace MicroEngine.Backend.Raylib.Resources;

/// <summary>
/// Raylib implementation of IResourceLoader for audio clips.
/// Supports WAV, OGG, MP3, FLAC formats.
/// </summary>
public sealed class RaylibAudioClipLoader : IResourceLoader<IAudioClip>
{
    private static readonly string[] EXTENSIONS = [".wav", ".ogg", ".mp3", ".flac"];
    private static readonly string[] STREAMING_EXTENSIONS = [".ogg", ".mp3"];
    private uint _nextId = 1;

    /// <inheritdoc/>
    public IReadOnlyList<string> SupportedExtensions => EXTENSIONS;

    /// <inheritdoc/>
    public IAudioClip Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Audio file not found: {path}", path);
        }

        var extension = System.IO.Path.GetExtension(path).ToLowerInvariant();
        var id = new ResourceId(_nextId++);

        // Use streaming for music formats (OGG, MP3)
        if (STREAMING_EXTENSIONS.Contains(extension))
        {
            var music = Raylib_cs.Raylib.LoadMusicStream(path);
            if (music.FrameCount == 0)
            {
                throw new InvalidDataException($"Failed to load music: {path}");
            }
            return new RaylibAudioClip(id, path, music);
        }
        else
        {
            var sound = Raylib_cs.Raylib.LoadSound(path);
            if (sound.FrameCount == 0)
            {
                throw new InvalidDataException($"Failed to load sound: {path}");
            }
            return new RaylibAudioClip(id, path, sound);
        }
    }

    /// <inheritdoc/>
    public void Unload(IAudioClip resource)
    {
        resource.Dispose();
    }
}
