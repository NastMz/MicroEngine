using MicroEngine.Platform;

namespace MicroEngine.Audio;

/// <summary>
/// An <see cref="IAudioDevice"/> over the engine's one mixer (<see cref="AudioEngine"/>) and a PCM sink: every backend
/// device is one of these with its own sink, so voices, priorities, stealing, music and fades behave the same
/// everywhere. Music decodes in managed code: WAV, OGG Vorbis (streamed, decoded ahead outside the simulation step), and
/// formats a backend adds (<see cref="AddDecoder"/>).
/// </summary>
/// <remarks>
/// Sound effects play on the "sfx" bus with priority 0, music on the "music" bus with the highest priority, so the voice
/// limit (<see cref="AudioEngineSettings.MaxVoices"/>) never steals the music. <see cref="Update"/> ends a simulation
/// step; <see cref="Pump"/> is the host frame's audio work.
/// </remarks>
public class MixerAudioDevice : IAudioDevice, IDisposable
{
    private readonly List<AudioClip> _sounds = [];
    private readonly List<Music> _music = [];
    private readonly Dictionary<string, Func<byte[], IAudioStream>> _decoders = new(StringComparer.OrdinalIgnoreCase);
    private VoiceHandle _musicVoice;
    private MusicHandle? _current;

    /// <summary>A device mixing into <paramref name="sink"/>, which starts rendering now.</summary>
    public MixerAudioDevice(IAudioSink sink, AudioEngineSettings? settings = null)
    {
        Engine = new AudioEngine(sink, settings);
        _decoders["ogg"] = VorbisStream.Open;
    }

    /// <summary>The mixer, for what <see cref="IAudioDevice"/> does not cover: buses, handles, priorities.</summary>
    public AudioEngine Engine { get; }

    /// <summary>True: the sink plays. A backend whose device could not open uses a <see cref="SilentAudioDevice"/> instead.</summary>
    public virtual bool IsAvailable => true;

    /// <summary>The formats <see cref="LoadMusicStream"/> decodes, lower case: "wav", "ogg" and any a backend added.</summary>
    public IReadOnlyList<string> MusicFormats => ["wav", .. _decoders.Keys.Select(k => k.ToLowerInvariant()).Order(StringComparer.Ordinal)];

    /// <summary>Adds a music format the device decodes (a backend's native decoder, for example).</summary>
    /// <param name="format">The file extension without the dot.</param>
    /// <param name="open">Opens a stream over the file's bytes; throws <see cref="InvalidDataException"/> for bytes it cannot decode.</param>
    protected void AddDecoder(string format, Func<byte[], IAudioStream> open)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(format);
        _decoders[format.TrimStart('.')] = open ?? throw new ArgumentNullException(nameof(open));
    }

    /// <summary>The music started last, until it is stopped or (when it does not loop) ends on the mixer's timeline.</summary>
    public MusicHandle? CurrentMusic
    {
        get
        {
            if (_current is not null && !Engine.IsPlaying(_musicVoice)) _current = null;
            return _current;
        }
    }

    /// <inheritdoc/>
    public SoundHandle LoadSound(string name, AudioClip clip)
    {
        ArgumentNullException.ThrowIfNull(clip);
        _sounds.Add(clip);
        return new SoundHandle(_sounds.Count - 1, name);
    }

    /// <inheritdoc/>
    public MusicHandle LoadMusic(string name, AudioClip clip, bool loop = true)
    {
        ArgumentNullException.ThrowIfNull(clip);
        _music.Add(new Music(clip, null, null, loop));
        return new MusicHandle(_music.Count - 1, name);
    }

    /// <summary>Loads music to stream: "wav" is decoded now; "ogg" and the formats a backend added are decoded while they play.</summary>
    /// <exception cref="NotSupportedException">The format is not one of <see cref="MusicFormats"/>.</exception>
    /// <exception cref="InvalidDataException">The bytes cannot be decoded as that format.</exception>
    public MusicHandle LoadMusicStream(string name, byte[] encoded, string format, bool loop = true)
    {
        ArgumentNullException.ThrowIfNull(encoded);
        format = format.TrimStart('.').ToLowerInvariant();
        if (format == "wav") return LoadMusic(name, AudioClip.FromWav(encoded), loop);
        if (!_decoders.TryGetValue(format, out var open))
            throw new NotSupportedException($"Music '{name}': this audio device decodes {string.Join(", ", MusicFormats)}, not {format}.");
        try { open(encoded).Dispose(); }
        catch (Exception e) when (e is ArgumentException or InvalidDataException or EndOfStreamException or NotSupportedException)
        {
            throw new InvalidDataException($"Music '{name}' could not be decoded as {format}: {e.Message}", e);
        }
        _music.Add(new Music(null, encoded, open, loop));
        return new MusicHandle(_music.Count - 1, name);
    }

    /// <inheritdoc/>
    public virtual void Play(SoundHandle sound, float volume = 1, float pitch = 1, float pan = 0.5f) =>
        Engine.Play(_sounds[sound.Id], new PlayParameters { Volume = volume, Pitch = pitch, Pan = pan });

    /// <summary>
    /// Plays a loaded sound as a voice of the mixer, exactly as <paramref name="parameters"/> say (loop, bus and
    /// priority included), and returns its handle for the engine's <see cref="AudioEngine.Stop"/>,
    /// <see cref="AudioEngine.Set"/>, <see cref="AudioEngine.SetPitch"/> and <see cref="AudioEngine.Pause"/>;
    /// <see cref="VoiceHandle.None"/> when the voice policy refused it.
    /// </summary>
    /// <exception cref="KeyNotFoundException">The bus does not exist.</exception>
    public virtual VoiceHandle PlayVoice(SoundHandle sound, in PlayParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(sound);
        return Engine.Play(_sounds[sound.Id], parameters);
    }

    /// <summary>
    /// Plays loaded music as one more voice of the mixer, beside the track <see cref="PlayMusic"/> plays (which it does
    /// not replace): ambience under the music, layers of one piece. It loops when <paramref name="parameters"/> say so,
    /// whatever the music was loaded with. Streamed music keeps its own pitch.
    /// </summary>
    /// <returns>Its handle, or <see cref="VoiceHandle.None"/> when the voice policy refused it.</returns>
    /// <exception cref="KeyNotFoundException">The bus does not exist.</exception>
    public virtual VoiceHandle PlayMusicVoice(MusicHandle music, in PlayParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(music);
        var track = _music[music.Id];
        return track.Clip is { } clip ? Engine.Play(clip, parameters) : Engine.Play(track.Open!(track.Encoded!), parameters);
    }

    /// <inheritdoc/>
    public void PlayMusic(MusicHandle music, float volume = 1)
    {
        StopMusic();
        var track = _music[music.Id];
        var parameters = new PlayParameters { Volume = volume, Loop = track.Loop, Bus = AudioEngine.MusicBus, Priority = int.MaxValue };
        _musicVoice = track.Clip is { } clip ? Engine.Play(clip, parameters) : Engine.Play(track.Open!(track.Encoded!), parameters);
        _current = music;
    }

    /// <inheritdoc/>
    public void StopMusic()
    {
        Engine.Stop(_musicVoice);
        _musicVoice = VoiceHandle.None;
        _current = null;
    }

    /// <inheritdoc/>
    public void SetMusicVolume(float volume) => Engine.Set(_musicVoice, volume: volume);

    /// <inheritdoc/>
    public void PauseMusic(bool paused) => Engine.Pause(_musicVoice, paused);

    /// <summary>Ends a simulation step (the game calls it once per update through <see cref="AudioMixer.Update"/>).</summary>
    public virtual void Update() => Engine.Advance();

    /// <summary>The host frame's audio work: decodes streamed music ahead and pumps the sink.</summary>
    public virtual void Pump() => Engine.Pump();

    /// <summary>Stops the sink and releases the streams.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Stops the mixer; a backend also closes its device here.</summary>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing) Engine.Dispose();
    }

    private sealed record Music(AudioClip? Clip, byte[]? Encoded, Func<byte[], IAudioStream>? Open, bool Loop);
}

/// <summary>OGG Vorbis decoded in managed code (NVorbis), the same on every backend.</summary>
internal sealed class VorbisStream(NVorbis.VorbisReader reader) : IAudioStream
{
    public static IAudioStream Open(byte[] encoded) => new VorbisStream(new NVorbis.VorbisReader(new MemoryStream(encoded), closeOnDispose: true));

    public int Channels => reader.Channels;
    public int SampleRate => reader.SampleRate;
    public int Read(Span<float> buffer) => reader.ReadSamples(buffer[..(buffer.Length / Channels * Channels)]);
    public void Rewind() => reader.SamplePosition = 0;
    public void Dispose() => reader.Dispose();
}
