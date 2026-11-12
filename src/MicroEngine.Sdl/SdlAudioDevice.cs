using MicroEngine.Audio;
using MicroEngine.Kernel;
using MicroEngine.Platform;
using SDL;
using static SDL.SDL3;

namespace MicroEngine.Sdl;

/// <summary>
/// SDL3 audio: the engine's mixer (<see cref="MixerAudioDevice"/>, the same voices, priorities and music on every
/// backend) playing through SDL's default playback device. A feeder thread keeps <see cref="DefaultLatencyMilliseconds"/>
/// of audio queued in an SDL audio stream (48 kHz stereo float), so main-thread stalls and garbage collections shorter
/// than that are not heard. It works without a window, so a host can use it with another platform. In a browser SDL's
/// own audio is not used: the mixer plays through the page's AudioWorklet queue, filled once per frame
/// (<see cref="BrowserAudioSink"/>).
/// </summary>
public sealed class SdlAudioDevice : MixerAudioDevice
{
    /// <summary>Audio kept queued by default, in milliseconds: longer than the longest collection measured on desktop.</summary>
    public const double DefaultLatencyMilliseconds = 60;
    private const int Rate = 48000;

    private SdlAudioDevice(IAudioSink sink) : base(sink) { }

    /// <summary>Opens the default playback device, or returns a silent device (and says why) when there is none.</summary>
    public static IAudioDevice TryCreate() => TryCreate(new ConsoleLog());

    /// <summary>Opens the default playback device, or returns a silent device and writes why to <paramref name="log"/> when there is none.</summary>
    public static IAudioDevice TryCreate(ILog log)
    {
        ArgumentNullException.ThrowIfNull(log);
        return OperatingSystem.IsBrowser() ? OpenBrowser(log) : Open(null, DefaultLatencyMilliseconds, log);
    }

    /// <summary>The page's AudioWorklet sink; silence (and why) when the page offers no audio.</summary>
    [System.Runtime.Versioning.SupportedOSPlatform("browser")]
    private static IAudioDevice OpenBrowser(ILog log)
    {
        try { return new SdlAudioDevice(new BrowserAudioSink(BrowserAudioSink.DefaultLatencyMilliseconds)); }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            log.Write(LogLevel.Warning, "audio", $"the page offers no audio output ({e.Message}), continuing silently");
            return new SilentAudioDevice();
        }
    }

    /// <summary>
    /// Opens the default playback device of SDL's audio driver <paramref name="driver"/>: "dummy" runs SDL's whole audio
    /// path without any device or sound, for tests; "disk" writes the mixed output to a file (SDL's
    /// <c>SDL_AUDIO_DISK_OUTPUT_FILE</c> hint). Returns a silent device (and says why) when the driver is unavailable
    /// or SDL's audio was already started with another driver.
    /// </summary>
    public static IAudioDevice TryCreate(string driver)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(driver);
        return Open(driver, DefaultLatencyMilliseconds, new ConsoleLog());
    }

    /// <summary>
    /// Opens the default playback device (of <paramref name="driver"/>, or SDL's choice when null) keeping
    /// <paramref name="latencyMilliseconds"/> of audio queued: lower plays sounds sooner, higher survives longer pauses.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="latencyMilliseconds"/> is not between 5 and 1000.</exception>
    public static IAudioDevice TryCreate(string? driver, double latencyMilliseconds)
    {
        if (latencyMilliseconds is < 5 or > 1000) throw new ArgumentOutOfRangeException(nameof(latencyMilliseconds));
        return Open(driver, latencyMilliseconds, new ConsoleLog());
    }

    private static unsafe IAudioDevice Open(string? driver, double latency, ILog log)
    {
        try
        {
            if (driver is not null) SDL_SetHint(SDL_HINT_AUDIO_DRIVER, driver);
            bool started;
            try { started = SDL_InitSubSystem(SDL_InitFlags.SDL_INIT_AUDIO); }
            finally { if (driver is not null) SDL_ResetHint(SDL_HINT_AUDIO_DRIVER); }
            if (started && driver is not null && SDL_GetCurrentAudioDriver() != driver)
            {
                log.Write(LogLevel.Warning, "audio", $"SDL audio already runs with the '{SDL_GetCurrentAudioDriver()}' driver, not '{driver}'; continuing silently");
                SDL_QuitSubSystem(SDL_InitFlags.SDL_INIT_AUDIO);
                return new SilentAudioDevice();
            }
            if (!started)
            {
                log.Write(LogLevel.Warning, "audio", $"{SDL_GetError()}, continuing silently");
                return new SilentAudioDevice();
            }
            var spec = new SDL_AudioSpec { format = SDL_AudioFormat.SDL_AUDIO_F32LE, channels = 2, freq = Rate };
            var device = SDL_OpenAudioDevice(SDL_AUDIO_DEVICE_DEFAULT_PLAYBACK, &spec);
            if (device == 0)
            {
                log.Write(LogLevel.Warning, "audio", $"no device available ({SDL_GetError()}), continuing silently");
                SDL_QuitSubSystem(SDL_InitFlags.SDL_INIT_AUDIO);
                return new SilentAudioDevice();
            }
            return new SdlAudioDevice(new SdlFeederSink(device, new AudioFormat(Rate, 2), latency));
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
        {
            log.Write(LogLevel.Warning, "audio", $"{e.Message}");
            return new SilentAudioDevice();
        }
    }
}
