using MicroEngine.Content;

namespace MicroEngine.Audio;

/// <summary>
/// The bytes of a music file loaded from content (the "music" asset kind) and its format ("wav", "ogg", "mp3"): music is
/// decoded while it plays rather than all at once.
/// </summary>
public sealed class EncodedAudio
{
    /// <summary>Wraps a music file's bytes without copying them.</summary>
    /// <param name="data">The whole file.</param>
    /// <param name="format">Its format, the file extension without the dot ("ogg"); compared without regard to case.</param>
    /// <exception cref="ArgumentException"><paramref name="format"/> is null, empty or white space.</exception>
    public EncodedAudio(byte[] data, string format)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrWhiteSpace(format);
        Data = data;
        Format = format.TrimStart('.').ToLowerInvariant();
    }

    /// <summary>The file's bytes. The array is not copied.</summary>
    public byte[] Data { get; }
    /// <summary>The format, lower case without the dot: "wav", "ogg", "mp3".</summary>
    public string Format { get; }
}

/// <summary>
/// The audio loaders of the content store (<c>Game.Assets.Sound</c>, <c>Game.Assets.Music</c>): "sound" decodes a PCM WAV file into an <see cref="AudioClip"/> (".wav"), and
/// "music" reads a music file's bytes as <see cref="EncodedAudio"/> (".ogg", ".mp3"; a ".wav" asked for as music too).
/// </summary>
public static class AudioAssetLoaders
{
    /// <summary>The kind of decoded sound effects (<see cref="AudioClip"/>).</summary>
    public const string Sound = "sound";
    /// <summary>The kind of music files (<see cref="EncodedAudio"/>).</summary>
    public const string Music = "music";

    /// <summary>Registers both loaders in <paramref name="loaders"/> (<c>Game.Assets</c> has them already).</summary>
    public static void Register(AssetLoaderRegistry loaders)
    {
        loaders.Add(new AssetLoader<AudioClip>(Sound, c => AudioClip.FromWav(c.ReadBytes()), [".wav"], clip => clip.Samples.LongLength * sizeof(float)));
        loaders.Add(new AssetLoader<EncodedAudio>(Music, c => new EncodedAudio(c.ReadBytes(), Path.GetExtension(c.Path) is { Length: > 1 } e ? e : "wav"),
            [".ogg", ".mp3"], music => music.Data.LongLength));
    }
}
