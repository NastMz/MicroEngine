using MicroEngine.Audio;
using SDL;
using static SDL.SDL3;

namespace MicroEngine.Sdl;

/// <summary>
/// SDL3 audio. SDL mixes every audio stream bound to a device, so the device keeps a pool of streams (voices) for sound
/// effects, each converting its clip's rate and channels and applying pitch (frequency ratio) and volume (gain); pan is
/// applied when the clip is copied into its voice. Music is one stream topped up from <see cref="Update"/>, decoded from
/// PCM or OGG Vorbis (NVorbis) in managed code. It works without a window, so a host can use it with another platform.
/// </summary>
public sealed unsafe class SdlAudioDevice : IAudioDevice, IDisposable
{
    private const int Voices = 16;
    private const int Rate = 48000;
    private readonly SDL_AudioDeviceID _device;
    private readonly SDL_AudioStream*[] _voices = new SDL_AudioStream*[Voices];
    private readonly List<AudioClip> _sounds = [];
    private readonly List<Func<MusicSource>> _music = [];
    private SDL_AudioStream* _musicStream;
    private MusicSource? _current;
    private float _musicVolume = 1;
    private bool _paused;
    private int _nextVoice;

    private SdlAudioDevice(SDL_AudioDeviceID device)
    {
        _device = device;
        for (int i = 0; i < Voices; i++) _voices[i] = Bind(1, AudioClip.DefaultSampleRate);
    }

    /// <summary>Opens the default playback device, or returns a silent device (and says why) when there is none.</summary>
    public static IAudioDevice TryCreate()
    {
        try
        {
            if (!SDL_InitSubSystem(SDL_InitFlags.SDL_INIT_AUDIO))
            {
                Console.Error.WriteLine($"audio: {SDL_GetError()}, continuing silently");
                return new SilentAudioDevice();
            }
            var spec = new SDL_AudioSpec { format = SDL_AudioFormat.SDL_AUDIO_F32LE, channels = 2, freq = Rate };
            var device = SDL_OpenAudioDevice(SDL_AUDIO_DEVICE_DEFAULT_PLAYBACK, &spec);
            if (device == 0)
            {
                Console.Error.WriteLine($"audio: no device available ({SDL_GetError()}), continuing silently");
                SDL_QuitSubSystem(SDL_InitFlags.SDL_INIT_AUDIO);
                return new SilentAudioDevice();
            }
            return new SdlAudioDevice(device);
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
        {
            Console.Error.WriteLine($"audio: {e.Message}");
            return new SilentAudioDevice();
        }
    }

    public bool IsAvailable => true;
    public MusicHandle? CurrentMusic { get; private set; }

    private SDL_AudioStream* Bind(int channels, int rate)
    {
        var source = new SDL_AudioSpec { format = SDL_AudioFormat.SDL_AUDIO_F32LE, channels = channels, freq = rate };
        var output = new SDL_AudioSpec { format = SDL_AudioFormat.SDL_AUDIO_F32LE, channels = 2, freq = Rate };
        var stream = SDL_CreateAudioStream(&source, &output);
        if (stream == null || !SDL_BindAudioStream(_device, stream)) throw new InvalidOperationException($"SDL audio stream: {SDL_GetError()}");
        return stream;
    }

    public SoundHandle LoadSound(string name, AudioClip clip)
    {
        ArgumentNullException.ThrowIfNull(clip);
        _sounds.Add(clip);
        return new SoundHandle(_sounds.Count - 1, name);
    }

    public MusicHandle LoadMusic(string name, AudioClip clip, bool loop = true)
    {
        ArgumentNullException.ThrowIfNull(clip);
        _music.Add(() => new PcmMusic(clip, loop));
        return new MusicHandle(_music.Count - 1, name);
    }

    public MusicHandle LoadMusicStream(string name, byte[] encoded, string format, bool loop = true)
    {
        ArgumentNullException.ThrowIfNull(encoded);
        switch (format.TrimStart('.').ToLowerInvariant())
        {
            case "wav":
                var clip = AudioClip.FromWav(encoded);
                return LoadMusic(name, clip, loop);
            case "ogg":
                try { using var probe = new NVorbis.VorbisReader(new MemoryStream(encoded), closeOnDispose: true); }
                catch (Exception e) when (e is ArgumentException or InvalidDataException or EndOfStreamException)
                {
                    throw new InvalidDataException($"Music '{name}' could not be decoded as ogg: {e.Message}", e);
                }
                _music.Add(() => new VorbisMusic(encoded, loop));
                return new MusicHandle(_music.Count - 1, name);
            default:
                throw new NotSupportedException($"Music '{name}': the SDL audio device streams wav and ogg, not {format}.");
        }
    }

    public void Play(SoundHandle sound, float volume = 1, float pitch = 1, float pan = 0.5f)
    {
        var clip = _sounds[sound.Id];
        var voice = _voices[_nextVoice];
        _nextVoice = (_nextVoice + 1) % Voices;
        var samples = Pan(clip, pan);
        var source = new SDL_AudioSpec { format = SDL_AudioFormat.SDL_AUDIO_F32LE, channels = 2, freq = clip.SampleRate };
        SDL_ClearAudioStream(voice);
        SDL_SetAudioStreamFormat(voice, &source, null);
        SDL_SetAudioStreamGain(voice, Math.Clamp(volume, 0, 1));
        SDL_SetAudioStreamFrequencyRatio(voice, Math.Clamp(pitch, 0.01f, 100f));
        fixed (float* p = samples) SDL_PutAudioStreamData(voice, (nint)p, samples.Length * sizeof(float));
        SDL_FlushAudioStream(voice);
    }

    /// <summary>Stereo copy of the clip with balance panning (0 = left, 0.5 = centre, 1 = right), as raylib's miniaudio applies it.</summary>
    internal static float[] Pan(AudioClip clip, float pan)
    {
        pan = Math.Clamp(pan, 0, 1);
        float left = Math.Min(1, 2 * (1 - pan)), right = Math.Min(1, 2 * pan);
        int frames = clip.Samples.Length / clip.Channels;
        var stereo = new float[frames * 2];
        for (int i = 0; i < frames; i++)
        {
            float l = clip.Samples[i * clip.Channels], r = clip.Channels == 2 ? clip.Samples[i * 2 + 1] : l;
            stereo[i * 2] = l * left;
            stereo[i * 2 + 1] = r * right;
        }
        return stereo;
    }

    public void PlayMusic(MusicHandle music, float volume = 1)
    {
        StopMusic();
        _current = _music[music.Id]();
        CurrentMusic = music;
        _musicVolume = volume;
        _paused = false;
        _musicStream = Bind(_current.Channels, _current.SampleRate);
        SDL_SetAudioStreamGain(_musicStream, volume);
        Update();
    }

    public void StopMusic()
    {
        if (_musicStream != null) SDL_DestroyAudioStream(_musicStream);
        _musicStream = null;
        _current?.Dispose();
        _current = null;
        CurrentMusic = null;
    }

    public void SetMusicVolume(float volume)
    {
        _musicVolume = volume;
        if (_musicStream != null) SDL_SetAudioStreamGain(_musicStream, volume);
    }

    public void PauseMusic(bool paused)
    {
        if (_musicStream == null || paused == _paused) return;
        _paused = paused;
        // Unbinding keeps the queued audio, so the music resumes where it stopped.
        if (paused) SDL_UnbindAudioStream(_musicStream);
        else SDL_BindAudioStream(_device, _musicStream);
    }

    /// <summary>Keeps about a quarter of a second of music queued.</summary>
    public void Update()
    {
        if (_current is null || _musicStream == null || _paused) return;
        int bytesPerFrame = _current.Channels * sizeof(float);
        int target = _current.SampleRate / 4 * bytesPerFrame;
        var buffer = new float[_current.SampleRate / 8 * _current.Channels];
        while (SDL_GetAudioStreamQueued(_musicStream) < target)
        {
            int read = _current.Read(buffer);
            if (read == 0) break;
            fixed (float* p = buffer) SDL_PutAudioStreamData(_musicStream, (nint)p, read * sizeof(float));
        }
    }

    public void Dispose()
    {
        StopMusic();
        foreach (var v in _voices) if (v != null) SDL_DestroyAudioStream(v);
        SDL_CloseAudioDevice(_device);
        SDL_QuitSubSystem(SDL_InitFlags.SDL_INIT_AUDIO);
    }

    /// <summary>A playing piece of music: interleaved float samples, looping or not.</summary>
    private abstract class MusicSource : IDisposable
    {
        public abstract int Channels { get; }
        public abstract int SampleRate { get; }
        /// <summary>Fills the buffer from the start; returns the samples written, 0 at the end of non-looping music.</summary>
        public abstract int Read(float[] buffer);
        public virtual void Dispose() { }
    }

    private sealed class PcmMusic(AudioClip clip, bool loop) : MusicSource
    {
        private int _position;
        public override int Channels => clip.Channels;
        public override int SampleRate => clip.SampleRate;

        public override int Read(float[] buffer)
        {
            int written = 0;
            while (written < buffer.Length && clip.Samples.Length > 0)
            {
                if (_position >= clip.Samples.Length)
                {
                    if (!loop) break;
                    _position = 0;
                }
                int n = Math.Min(buffer.Length - written, clip.Samples.Length - _position);
                Array.Copy(clip.Samples, _position, buffer, written, n);
                _position += n;
                written += n;
            }
            return written;
        }
    }

    private sealed class VorbisMusic(byte[] encoded, bool loop) : MusicSource
    {
        private readonly NVorbis.VorbisReader _reader = new(new MemoryStream(encoded), closeOnDispose: true);
        public override int Channels => _reader.Channels;
        public override int SampleRate => _reader.SampleRate;

        public override int Read(float[] buffer)
        {
            int written = 0;
            int frames = buffer.Length / Channels * Channels;
            while (written < frames)
            {
                int n = _reader.ReadSamples(buffer, written, frames - written);
                if (n > 0) { written += n; continue; }
                if (!loop || written == 0 && _reader.SamplePosition == 0) break;
                _reader.SamplePosition = 0;
            }
            return written;
        }

        public override void Dispose() => _reader.Dispose();
    }
}
