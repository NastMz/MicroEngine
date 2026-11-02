using System.Runtime.InteropServices;
using MicroEngine.Audio;
using Rl = Raylib_cs.Raylib;

namespace MicroEngine.Raylib;

/// <summary>
/// raylib audio. Each sound keeps a small pool of aliases so rapid repeats overlap instead of cutting each other off.
/// Decoded music (an <see cref="AudioClip"/>, such as a rendered song) is streamed straight from its samples, a buffer at
/// a time from <see cref="Update"/>; encoded music (ogg, mp3, wav files) is streamed by raylib from an unmanaged copy that
/// lives as long as the stream.
/// </summary>
internal sealed unsafe class RaylibAudioDevice : IAudioDevice, IDisposable
{
    private const int Voices = 4;
    private readonly List<(Raylib_cs.Sound Main, Raylib_cs.Sound[] Aliases)> _sounds = [];
    private readonly List<ITrack> _music = [];
    private readonly int[] _next = new int[1024];
    private int _current = -1;
    private float _musicVolume = 1;

    private RaylibAudioDevice() { }

    public static IAudioDevice TryCreate()
    {
        try
        {
            Rl.InitAudioDevice();
            if (Rl.IsAudioDeviceReady()) return new RaylibAudioDevice();
            Console.Error.WriteLine("audio: no device available, continuing silently");
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
        {
            Console.Error.WriteLine($"audio: {e.Message}");
        }
        return new SilentAudioDevice();
    }

    public bool IsAvailable => true;
    public MusicHandle? CurrentMusic { get; private set; }

    public SoundHandle LoadSound(string name, AudioClip clip)
    {
        ArgumentNullException.ThrowIfNull(clip);
        Raylib_cs.Sound sound;
        // raylib copies (and converts) the samples into its own buffer: no WAV in between.
        fixed (float* samples = clip.Samples)
            sound = Rl.LoadSoundFromWave(new Raylib_cs.Wave
            {
                FrameCount = (uint)(clip.Samples.Length / clip.Channels),
                SampleRate = (uint)clip.SampleRate,
                SampleSize = 32,
                Channels = (uint)clip.Channels,
                Data = samples,
            });
        var aliases = new Raylib_cs.Sound[Voices - 1];
        for (int i = 0; i < aliases.Length; i++) aliases[i] = Rl.LoadSoundAlias(sound);
        _sounds.Add((sound, aliases));
        return new SoundHandle(_sounds.Count - 1, name);
    }

    /// <summary>
    /// Registers decoded music, streamed from its samples. Converting a long song to WAV here cost 50 to 415 ms of one
    /// frame in browsers (interpreted WebAssembly); streaming costs a buffer copy per update.
    /// </summary>
    public MusicHandle LoadMusic(string name, AudioClip clip, bool loop = true)
    {
        ArgumentNullException.ThrowIfNull(clip);
        _music.Add(new ClipTrack(clip, loop));
        return new MusicHandle(_music.Count - 1, name);
    }

    public MusicHandle LoadMusicStream(string name, byte[] encoded, string format, bool loop = true)
    {
        var memory = (nint)NativeMemory.Alloc((nuint)encoded.Length);
        Marshal.Copy(encoded, 0, memory, encoded.Length);
        var extension = "." + format.TrimStart('.').ToLowerInvariant();
        var type = stackalloc sbyte[extension.Length + 1];
        for (int i = 0; i < extension.Length; i++) type[i] = (sbyte)extension[i];
        type[extension.Length] = 0;
        var music = Rl.LoadMusicStreamFromMemory(type, (byte*)memory, encoded.Length);
        if (music.FrameCount == 0)
        {
            NativeMemory.Free((void*)memory);
            throw new InvalidDataException($"Music '{name}' could not be decoded as {format}.");
        }
        music.Looping = loop;
        _music.Add(new EncodedTrack(music, memory));
        return new MusicHandle(_music.Count - 1, name);
    }

    public void Play(SoundHandle sound, float volume = 1, float pitch = 1, float pan = 0.5f)
    {
        var (main, aliases) = _sounds[sound.Id];
        int slot = _next[sound.Id % _next.Length]++ % Voices;
        var voice = slot == 0 ? main : aliases[slot - 1];
        Rl.SetSoundVolume(voice, volume);
        Rl.SetSoundPitch(voice, pitch);
        Rl.SetSoundPan(voice, 1 - pan); // raylib pan: 1 = left, 0 = right
        Rl.PlaySound(voice);
    }

    public void PlayMusic(MusicHandle music, float volume = 1)
    {
        StopMusic();
        _current = music.Id;
        CurrentMusic = music;
        _musicVolume = volume;
        _music[_current].Play(volume);
    }

    public void StopMusic()
    {
        if (_current >= 0) _music[_current].Stop();
        _current = -1;
        CurrentMusic = null;
    }

    public void SetMusicVolume(float volume)
    {
        _musicVolume = volume;
        if (_current >= 0) _music[_current].SetVolume(volume);
    }

    public void PauseMusic(bool paused)
    {
        if (_current >= 0) _music[_current].Pause(paused);
    }

    public void Update()
    {
        if (_current >= 0) _music[_current].Update();
    }

    public void Dispose()
    {
        StopMusic();
        foreach (var (main, aliases) in _sounds)
        {
            foreach (var a in aliases) Rl.UnloadSoundAlias(a);
            Rl.UnloadSound(main);
        }
        foreach (var track in _music) track.Dispose();
        Rl.CloseAudioDevice();
    }

    /// <summary>A piece of music the device can play.</summary>
    private interface ITrack : IDisposable
    {
        void Play(float volume);
        void Stop();
        void Pause(bool paused);
        void SetVolume(float volume);
        void Update();
    }

    /// <summary>Encoded music raylib decodes and streams itself, from memory the track owns.</summary>
    private sealed class EncodedTrack(Raylib_cs.Music music, nint memory) : ITrack
    {
        public void Play(float volume) { Rl.SetMusicVolume(music, volume); Rl.PlayMusicStream(music); }
        public void Stop() => Rl.StopMusicStream(music);
        public void Pause(bool paused) { if (paused) Rl.PauseMusicStream(music); else Rl.ResumeMusicStream(music); }
        public void SetVolume(float volume) => Rl.SetMusicVolume(music, volume);
        public void Update() => Rl.UpdateMusicStream(music);
        public void Dispose() { Rl.UnloadMusicStream(music); NativeMemory.Free((void*)memory); }
    }

    /// <summary>
    /// Decoded music streamed from its float samples: raylib asks for a buffer of <see cref="Frames"/> frames whenever one
    /// has played, and the track copies the next ones (wrapping when it loops, silence after the end when it does not).
    /// </summary>
    private sealed class ClipTrack : ITrack
    {
        private const int Frames = 4096; // about 93 ms at 44.1 kHz, two buffers in flight
        private readonly Raylib_cs.AudioStream _stream;
        private readonly float[] _samples;
        private readonly float[] _chunk;
        private readonly int _channels;
        private readonly bool _loop;
        private int _position;
        private bool _playing, _ended;

        public ClipTrack(AudioClip clip, bool loop)
        {
            _samples = clip.Samples;
            _channels = clip.Channels;
            _loop = loop;
            _chunk = new float[Frames * _channels];
            Rl.SetAudioStreamBufferSizeDefault(Frames);
            _stream = Rl.LoadAudioStream((uint)clip.SampleRate, 32, (uint)clip.Channels);
            Rl.SetAudioStreamBufferSizeDefault(0);
        }

        public void Play(float volume)
        {
            Rl.StopAudioStream(_stream);
            _position = 0;
            _ended = false;
            _playing = true;
            Rl.SetAudioStreamVolume(_stream, volume);
            Fill();
            Rl.PlayAudioStream(_stream);
        }

        public void Stop()
        {
            _playing = false;
            Rl.StopAudioStream(_stream);
        }

        public void Pause(bool paused)
        {
            if (paused) Rl.PauseAudioStream(_stream);
            else Rl.ResumeAudioStream(_stream);
        }

        public void SetVolume(float volume) => Rl.SetAudioStreamVolume(_stream, volume);

        public void Update()
        {
            if (_playing) Fill();
        }

        private void Fill()
        {
            while (Rl.IsAudioStreamProcessed(_stream))
            {
                if (_ended) { Stop(); return; }
                int written = 0;
                while (written < _chunk.Length)
                {
                    if (_position >= _samples.Length)
                    {
                        if (!_loop || _samples.Length == 0) { _ended = true; break; }
                        _position = 0;
                    }
                    int count = Math.Min(_chunk.Length - written, _samples.Length - _position);
                    Array.Copy(_samples, _position, _chunk, written, count);
                    written += count;
                    _position += count;
                }
                if (written < _chunk.Length) Array.Clear(_chunk, written, _chunk.Length - written);
                fixed (float* data = _chunk) Rl.UpdateAudioStream(_stream, data, Frames);
            }
        }

        public void Dispose() => Rl.UnloadAudioStream(_stream);
    }
}
