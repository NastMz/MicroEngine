using MicroEngine.Platform;

namespace MicroEngine.Audio;

/// <summary>
/// An <see cref="IAudioDevice"/> over the engine's one mixer (<see cref="AudioEngine"/>) and a PCM sink: every backend
/// device is one of these with its own sink, so voices, priorities, stealing, music and fades behave the same
/// everywhere. Music decodes in managed code, streamed and decoded ahead outside the simulation step: the formats of
/// <see cref="AudioDecoders"/> (WAV, OGG Vorbis, MP3 and what a game registers), and formats a backend adds for itself
/// (<see cref="AddDecoder"/>).
/// </summary>
/// <remarks>
/// Sound effects play on the "sfx" bus with priority 0, music on the "music" bus with the highest priority, so the voice
/// limit (<see cref="AudioEngineSettings.MaxVoices"/>) never steals the music. <see cref="Update"/> ends a simulation
/// step; <see cref="Pump"/> is the host frame's audio work.
/// </remarks>
public class MixerAudioDevice : IAudioDevice, IDisposable
{
    // Loaded sounds and music by handle id; an unloaded one leaves its place free for the next.
    private readonly List<AudioClip?> _sounds = [];
    private readonly List<Music?> _music = [];
    private readonly Stack<int> _freeSounds = new(), _freeMusic = new();
    private readonly Dictionary<string, Func<byte[], IAudioStream>> _decoders = new(StringComparer.OrdinalIgnoreCase);
    private VoiceHandle _musicVoice;
    private MusicHandle? _current;

    /// <summary>A device mixing into <paramref name="sink"/>, which starts rendering now.</summary>
    public MixerAudioDevice(IAudioSink sink, AudioEngineSettings? settings = null)
    {
        settings ??= new AudioEngineSettings();
        // A game's mix is kept within full scale unless the settings say otherwise.
        Engine = new AudioEngine(sink, settings with { Limiter = settings.Limiter ?? true });
    }

    /// <summary>The mixer, for what <see cref="IAudioDevice"/> does not cover: buses, handles, priorities.</summary>
    public AudioEngine Engine { get; }

    /// <summary>True: the sink plays. A backend whose device could not open uses a <see cref="SilentAudioDevice"/> instead.</summary>
    public virtual bool IsAvailable => true;

    /// <summary>
    /// The formats <see cref="LoadMusicStream"/> decodes, lower case in alphabetical order: those of
    /// <see cref="AudioDecoders"/> ("mp3", "ogg", "wav" and what a game registered) and any this backend added.
    /// </summary>
    public IReadOnlyList<string> MusicFormats =>
        [.. AudioDecoders.Formats.Concat(_decoders.Keys.Select(k => k.ToLowerInvariant())).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];

    /// <summary>
    /// Adds a music format only this device decodes (a backend's native decoder), for a format
    /// <see cref="AudioDecoders"/> does not have: a format registered there is decoded by that decoder on every
    /// device, so a file sounds the same on all of them.
    /// </summary>
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
        return new SoundHandle(Keep(_sounds, _freeSounds, clip), name);
    }

    /// <inheritdoc/>
    public MusicHandle LoadMusic(string name, AudioClip clip, bool loop = true)
    {
        ArgumentNullException.ThrowIfNull(clip);
        return new MusicHandle(Keep(_music, _freeMusic, new Music(clip, null, null, loop)), name);
    }

    private static int Keep<T>(List<T?> list, Stack<int> free, T item) where T : class
    {
        if (free.TryPop(out int id))
        {
            list[id] = item;
            return id;
        }
        list.Add(item);
        return list.Count - 1;
    }

    /// <summary>
    /// Forgets a loaded sound: the device no longer holds its samples, and its handle plays nothing from now on. A
    /// voice already playing it plays on to its end.
    /// </summary>
    public void UnloadSound(SoundHandle sound)
    {
        ArgumentNullException.ThrowIfNull(sound);
        if ((uint)sound.Id >= (uint)_sounds.Count || _sounds[sound.Id] is null) return;
        _sounds[sound.Id] = null;
        _freeSounds.Push(sound.Id);
    }

    /// <summary>
    /// Forgets loaded music: the device no longer holds its samples or its file's bytes, and its handle plays nothing
    /// from now on. If it is the music playing, it stops; a voice playing it beside the music plays on to its end.
    /// </summary>
    public void UnloadMusic(MusicHandle music)
    {
        ArgumentNullException.ThrowIfNull(music);
        if ((uint)music.Id >= (uint)_music.Count || _music[music.Id] is null) return;
        if (ReferenceEquals(_current, music)) StopMusic();
        _music[music.Id] = null;
        _freeMusic.Push(music.Id);
    }

    /// <summary>Sounds and music loaded now (unloaded ones not counted).</summary>
    public (int Sounds, int Music) Loaded => (_sounds.Count - _freeSounds.Count, _music.Count - _freeMusic.Count);

    /// <summary>
    /// Loads music to stream, decoded while it plays: any format of <see cref="MusicFormats"/> ("wav" is decoded now).
    /// A format of <see cref="AudioDecoders"/> is decoded by that decoder; a backend's own decoder serves only a
    /// format that list lacks.
    /// </summary>
    /// <exception cref="NotSupportedException">The format is not one of <see cref="MusicFormats"/>.</exception>
    /// <exception cref="InvalidDataException">The bytes cannot be decoded as that format.</exception>
    public MusicHandle LoadMusicStream(string name, byte[] encoded, string format, bool loop = true)
    {
        ArgumentNullException.ThrowIfNull(encoded);
        format = AudioDecoders.Normalize(format);
        if (format == "wav") return LoadMusic(name, AudioClip.FromWav(encoded), loop);
        if (!AudioDecoders.TryGet(format, out var open) && !_decoders.TryGetValue(format, out open))
            throw new NotSupportedException($"Music '{name}': this audio device decodes {string.Join(", ", MusicFormats)}, not {format}.");
        try { AudioDecoders.Open(encoded, format, open).Dispose(); }
        catch (InvalidDataException e)
        {
            throw new InvalidDataException($"Music '{name}': {e.Message}", e);
        }
        return new MusicHandle(Keep(_music, _freeMusic, new Music(null, encoded, open, loop)), name);
    }

    /// <inheritdoc/>
    public virtual void Play(SoundHandle sound, float volume = 1, float pitch = 1, float pan = 0.5f)
    {
        if (Sound(sound) is { } clip) Engine.Play(clip, new PlayParameters { Volume = volume, Pitch = pitch, Pan = pan });
    }

    /// <summary>Plays a clip that was not loaded into the device: it holds the clip only while a voice plays it.</summary>
    public virtual void Play(AudioClip clip, float volume = 1, float pitch = 1, float pan = 0.5f) =>
        Engine.Play(clip, new PlayParameters { Volume = volume, Pitch = pitch, Pan = pan });

    /// <summary>
    /// Plays a clip that was not loaded into the device as a voice of the mixer, exactly as
    /// <paramref name="parameters"/> say; the device holds the clip only while the voice plays it.
    /// </summary>
    /// <returns>Its handle, or <see cref="VoiceHandle.None"/> when the voice policy refused it.</returns>
    /// <exception cref="KeyNotFoundException">The bus does not exist.</exception>
    public virtual VoiceHandle PlayVoice(AudioClip clip, in PlayParameters parameters) => Engine.Play(clip, parameters);

    private AudioClip? Sound(SoundHandle sound) => (uint)sound.Id < (uint)_sounds.Count ? _sounds[sound.Id] : null;
    private Music? Track(MusicHandle music) => (uint)music.Id < (uint)_music.Count ? _music[music.Id] : null;

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
        return Sound(sound) is { } clip ? Engine.Play(clip, parameters) : VoiceHandle.None;
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
        if (Track(music) is not { } track) return VoiceHandle.None;
        return track.Clip is { } clip ? Engine.Play(clip, parameters) : Engine.Play(track.Open!(track.Encoded!), parameters);
    }

    /// <inheritdoc/>
    public void PlayMusic(MusicHandle music, float volume = 1)
    {
        StopMusic();
        if (Track(music) is not { } track) return;
        var parameters = new PlayParameters { Volume = volume, Loop = track.Loop, Bus = AudioEngine.MusicBus, Priority = int.MaxValue };
        _musicVoice = track.Clip is { } clip ? Engine.Play(clip, parameters) : Engine.Play(track.Open!(track.Encoded!), parameters);
        _current = music;
    }

    /// <summary>
    /// Starts <paramref name="music"/> rising from silence to <paramref name="volume"/> while the music that was
    /// playing falls to silence, both over <paramref name="seconds"/>, frame by frame; the new track is
    /// <see cref="CurrentMusic"/> at once. With no music playing it is a fade-in.
    /// </summary>
    public void CrossfadeMusic(MusicHandle music, float volume, float seconds)
    {
        ArgumentNullException.ThrowIfNull(music);
        Engine.Stop(_musicVoice, seconds);
        if (Track(music) is not { } track)
        {
            (_musicVoice, _current) = (VoiceHandle.None, null);
            return;
        }

        var parameters = new PlayParameters { Volume = volume, Loop = track.Loop, Bus = AudioEngine.MusicBus, Priority = int.MaxValue, FadeIn = seconds };
        _musicVoice = track.Clip is { } clip ? Engine.Play(clip, parameters) : Engine.Play(track.Open!(track.Encoded!), parameters);
        _current = music;
    }

    /// <summary>The voice of the music <see cref="PlayMusic"/> started, for the mixer's calls (its position, a seek); stale once the music stopped or ended.</summary>
    public VoiceHandle MusicVoice => _musicVoice;

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
/// <remarks>
/// The decoder's own seek lands some hundreds of samples away from the place asked for, which a loop would hear as a
/// seam and a game following its music as drift. So a seek here is exact by construction: forward it decodes and
/// drops what lies between, backward it starts the file again first (<see cref="ExactSeekStream"/>).
/// </remarks>
internal sealed class VorbisStream(NVorbis.VorbisReader reader) : IAudioStream
{
    public static IAudioStream Open(byte[] encoded)
    {
        var first = new VorbisStream(new NVorbis.VorbisReader(new MemoryStream(encoded, writable: false), closeOnDispose: true));
        return new ExactSeekStream(first, () => new VorbisStream(new NVorbis.VorbisReader(new MemoryStream(encoded, writable: false), closeOnDispose: true)), first.Frames, first.LoopRegion);
    }

    public long Frames => reader.TotalSamples;

    /// <summary>
    /// The loop the file's comments name, in the convention game tools write: LOOPSTART with LOOPLENGTH or LOOPEND, in
    /// frames.
    /// </summary>
    public LoopRegion? LoopRegion
    {
        get
        {
            if (!Tag("LOOPSTART", out long start)) return null;
            if (Tag("LOOPLENGTH", out long length)) return new LoopRegion(start, start + length);
            return Tag("LOOPEND", out long end) ? new LoopRegion(start, end) : new LoopRegion(start);
        }
    }

    private bool Tag(string name, out long value)
    {
        value = 0;
        string text = reader.Tags.GetTagSingle(name);
        return !string.IsNullOrWhiteSpace(text)
            && long.TryParse(text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out value) && value >= 0;
    }

    public int Channels => reader.Channels;

    public int SampleRate => reader.SampleRate;
    public int Read(Span<float> buffer) => reader.ReadSamples(buffer[..(buffer.Length / Channels * Channels)]);
    public void Rewind() => reader.SamplePosition = 0;
    public void Dispose() => reader.Dispose();
}
