using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using MicroEngine.Platform;

namespace MicroEngine.Sdl;

/// <summary>
/// The browser sink: the page plays the mixer's output through an AudioWorklet (Web Audio's own thread), and the game
/// fills the worklet's queue up to <see cref="DefaultLatencyMilliseconds"/> ahead once per frame
/// (<see cref="AudioSinkSchedule.HostPump"/>): .NET in the browser has one thread, so rendering happens on it, between
/// frames. A frame longer than the queue is heard as a gap. The page creates its AudioContext inside the gesture that
/// starts the game and suspends it while the page is hidden. The page registers the "microengine" JavaScript module's
/// <c>audio.start</c>, <c>audio.write</c>, <c>audio.buffered</c> and <c>audio.stop</c> functions; the browser host's page
/// does (src/MicroEngine.Cli/Hosts/web/wwwroot/main.js and audio-worklet.js).
/// </summary>
[SupportedOSPlatform("browser")]
internal sealed partial class BrowserAudioSink : IAudioSink
{
    /// <summary>Audio queued ahead by default: a frame shorter than about this is not heard.</summary>
    public const double DefaultLatencyMilliseconds = 100;
    private readonly int _target;
    private readonly float[] _buffer;
    private AudioRender? _render;

    /// <param name="latencyMilliseconds">Audio kept queued in the worklet.</param>
    /// <exception cref="JSException">The page offers no audio (no AudioContext, or the module's functions are missing).</exception>
    public BrowserAudioSink(double latencyMilliseconds)
    {
        int rate = (int)AudioStart(2);
        if (rate <= 0) throw new InvalidOperationException("the page has no audio context");
        Format = new AudioFormat(rate, 2);
        _target = (int)(latencyMilliseconds / 1000 * rate);
        _buffer = new float[(_target + 128) * 2];
    }

    public AudioFormat Format { get; }
    public AudioSinkSchedule Schedule => AudioSinkSchedule.HostPump;
    /// <summary>The queue target plus one render quantum (128 frames).</summary>
    public int LatencyFrames => _target + 128;

    public void Start(AudioRender render) => _render = render;

    /// <summary>Renders until the worklet's queue holds its target.</summary>
    public void Pump()
    {
        if (_render is null) return;
        int frames = Math.Min(_target - (int)AudioBuffered(), _buffer.Length / 2);
        if (frames <= 0) return;
        var span = _buffer.AsSpan(0, frames * 2);
        _render(span);
        AudioWrite(MemoryMarshal.AsBytes(span));
    }

    public void Dispose()
    {
        _render = null;
        AudioStop();
    }

    /// <summary>Creates (or reuses) the page's audio output; returns its sample rate, 0 when there is none.</summary>
    [JSImport("audio.start", "microengine")]
    private static partial double AudioStart(int channels);

    /// <summary>Queues interleaved float samples (copied by the page before the call returns).</summary>
    [JSImport("audio.write", "microengine")]
    private static partial void AudioWrite([JSMarshalAs<JSType.MemoryView>] Span<byte> interleaved);

    /// <summary>Frames queued and not yet played.</summary>
    [JSImport("audio.buffered", "microengine")]
    private static partial double AudioBuffered();

    /// <summary>Stops playing and forgets what is queued.</summary>
    [JSImport("audio.stop", "microengine")]
    private static partial void AudioStop();
}
