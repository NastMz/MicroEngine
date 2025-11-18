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
    private readonly ResourceValidator _validator;
    private uint _nextId = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="RaylibAudioClipLoader"/> class.
    /// </summary>
    /// <param name="validator">Resource validator instance.</param>
    public RaylibAudioClipLoader(ResourceValidator? validator = null)
    {
        _validator = validator ?? new ResourceValidator();
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> SupportedExtensions => EXTENSIONS;

    /// <inheritdoc/>
    public ResourceValidationResult Validate(string path)
    {
        return _validator.Validate(path, SupportedExtensions);
    }

    /// <inheritdoc/>
    public IAudioClip Load(string path, ResourceMetadata? metadata = null)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Audio file not found: {path}", path);
        }

        var extension = System.IO.Path.GetExtension(path).ToLowerInvariant();
        var id = new ResourceId(_nextId++);

        var enrichedMetadata = metadata ?? ResourceMetadata.FromFile(path);

        // Use streaming for music formats (OGG, MP3)
        if (STREAMING_EXTENSIONS.Contains(extension))
        {
            var music = Raylib_cs.Raylib.LoadMusicStream(path);
            if (music.FrameCount == 0)
            {
                throw new InvalidDataException($"Failed to load music: {path}");
            }
            
            enrichedMetadata = enrichedMetadata.WithCustomMetadata(new Dictionary<string, string>
            {
                ["Type"] = "Music",
                ["Streaming"] = "true",
                ["SampleRate"] = music.Stream.SampleRate.ToString(),
                ["Channels"] = music.Stream.Channels.ToString()
            });
            
            return new RaylibAudioClip(id, path, music, enrichedMetadata);
        }
        else
        {
            var sound = Raylib_cs.Raylib.LoadSound(path);
            if (sound.FrameCount == 0)
            {
                throw new InvalidDataException($"Failed to load sound: {path}");
            }
            
            enrichedMetadata = enrichedMetadata.WithCustomMetadata(new Dictionary<string, string>
            {
                ["Type"] = "Sound",
                ["Streaming"] = "false",
                ["SampleRate"] = sound.Stream.SampleRate.ToString(),
                ["Channels"] = sound.Stream.Channels.ToString()
            });
            
            return new RaylibAudioClip(id, path, sound, enrichedMetadata);
        }
    }

    /// <inheritdoc/>
    public void Unload(IAudioClip resource)
    {
        resource.Dispose();
    }
}
