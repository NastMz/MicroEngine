using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using P5.Audio;
using static SDL.SDL3;

[assembly: SupportedOSPlatform("browser")]

// Main only brings the runtime up; the page calls Probe.Init inside a user gesture, then Probe.Frame once per
// requestAnimationFrame (the browser owns the loop).
Console.WriteLine("P5 web probe: ready");

public static partial class Probe
{
    private static IAudioSink? _sink;
    private static ProbeMixer? _mixer;
    private static Scenario? _run;

    /// <summary>
    /// mode: <c>sdl-pull</c>, <c>sdl-push</c> or <c>worklet</c>. <paramref name="rate"/> is the AudioContext rate the page
    /// created (worklet); <paramref name="framesHint"/> &gt; 0 sets SDL_HINT_AUDIO_DEVICE_SAMPLE_FRAMES (SDL doubles it).
    /// Returns "" or an error.
    /// </summary>
    [JSExport]
    public static string Init(string mode, int targetMs, int rate, int framesHint)
    {
        if (framesHint > 0) SDL_SetHint(SDL_HINT_AUDIO_DEVICE_SAMPLE_FRAMES, framesHint.ToString());
        var format = new AudioFormat(rate, 2);
        int target = rate * targetMs / 1000;
        _sink = mode switch
        {
            "sdl-pull" => SdlPullSink.Create(format),
            "sdl-push" => SdlPushSink.Create(format, target),
            "worklet" => new WorkletSink(format, target),
            _ => null,
        };
        if (_sink is null) return "unknown mode " + mode;
        if (_sink is SdlSink { Error: { } error }) return error;
        _mixer = new ProbeMixer(format) { ClockChannel = true };
        _run = new Scenario(_sink, _mixer);
        _run.Start();
        return "";
    }

    [JSExport]
    public static bool Frame() => _run!.Frame();

    [JSExport]
    public static string Report() => _run!.Report(
        $"browser-wasm {RuntimeInformation.FrameworkDescription}",
        _sink is SdlSink s ? $"\"sdlCallbacks\":{s.Callbacks},\"sdlDriver\":\"{SDL_GetCurrentAudioDriver()}\"" : "\"sdlCallbacks\":0");

    [JSExport]
    public static string Offline() => Determinism.Run();

    [JSExport]
    public static string Cost(int voices, int frames) => P5.Audio.Cost.Run(new AudioFormat(48000, 2), voices, frames, 50);
}

/// <summary>
/// The browser sink: an AudioWorklet (Web Audio's own rendering thread) plays a queue of chunks; the frame loop renders
/// up to the target and posts each chunk to it (<c>p5sink.write</c> in main.js). The worklet reports what it played and
/// any frames it had to fill with silence.
/// </summary>
public sealed partial class WorkletSink(AudioFormat format, int targetFrames) : IAudioSink
{
    private readonly float[] _buffer = new float[1 << 16];
    private AudioRender? _render;

    [JSImport("globalThis.p5sink.write")] private static partial void Write([JSMarshalAs<JSType.MemoryView>] Span<byte> interleaved);
    [JSImport("globalThis.p5sink.buffered")] private static partial int Buffered();
    [JSImport("globalThis.p5sink.played")] private static partial double Played();
    [JSImport("globalThis.p5sink.starved")] private static partial double Starved();
    [JSImport("globalThis.p5sink.quantum")] private static partial int Quantum();

    public AudioFormat Format => format;
    public AudioSinkDriver Driver => AudioSinkDriver.HostPump;
    public int DevicePeriodFrames => Quantum();
    public int TargetBufferedFrames => targetFrames;
    public long StarvedFrames => (long)Starved();
    public long PlayedFrames => (long)Played();

    public void Start(AudioRender render) => _render = render;

    public void Pump()
    {
        if (_render is null) return;
        int buffered = Buffered();
        int frames = Math.Min(targetFrames - buffered, _buffer.Length / format.Channels);
        if (frames <= 0) return;
        var span = _buffer.AsSpan(0, frames * format.Channels);
        _render(span, buffered);
        Write(MemoryMarshal.AsBytes(span));
    }

    public string Describe() => $"worklet target={targetFrames}";
    public void Dispose() { }
}
