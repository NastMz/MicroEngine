using System.Diagnostics;
using System.Diagnostics.Metrics;
using MicroEngine.Audio;
using MicroEngine.Graphics;
using GraphicsCounters = MicroEngine.Graphics.Devices.GraphicsCounters;
using DeviceContract = MicroEngine.Graphics.Devices.IGraphicsDevice;
using MicroEngine.Kernel;

namespace MicroEngine.Core;

/// <summary>
/// What a frame cost, as <see cref="GameDiagnostics.LastFrame"/> reports it. Times are in milliseconds of real time.
/// </summary>
/// <param name="Frame">The number of the frame, from 1; 0 when nothing was measured.</param>
/// <param name="Interval">Time since the previous measured frame started (0 for the first one).</param>
/// <param name="Events">The platform's event pump.</param>
/// <param name="Update">The fixed steps and the per-frame updates (<see cref="FramePhase.BeginFrame"/> to <see cref="FramePhase.LateUpdate"/>).</param>
/// <param name="Render">Extracting and drawing (<see cref="FramePhase.Extract"/> and <see cref="FramePhase.Render"/>).</param>
/// <param name="Present">Presenting (<see cref="FramePhase.Present"/> and <see cref="FramePhase.EndFrame"/>), which includes waiting for the display.</param>
/// <param name="Steps">Fixed steps the frame ran.</param>
/// <param name="Allocated">Bytes the game's thread allocated during the frame.</param>
/// <param name="Gen0Collections">Garbage collections of generation 0 during the frame.</param>
/// <param name="Gen1Collections">Garbage collections of generation 1 during the frame.</param>
/// <param name="Gen2Collections">Full garbage collections during the frame.</param>
/// <param name="Graphics">The work the frame submitted to the graphics device (draw calls, passes, uploads; <see cref="GraphicsCounters.LiveResources"/> is the count at its end). Zeros on a platform whose device does not count.</param>
/// <param name="Audio">The mixer's voices at the end of the frame.</param>
public readonly record struct FrameStatistics(long Frame, double Interval, double Events, double Update, double Render, double Present,
    int Steps, long Allocated, int Gen0Collections, int Gen1Collections, int Gen2Collections, GraphicsCounters Graphics, AudioStatistics Audio)
{
    /// <summary>The frame's work: events, update, render and present.</summary>
    public double Total => Events + Update + Render + Present;
    /// <summary>Garbage collections of any generation during the frame.</summary>
    public int Collections => Gen0Collections + Gen1Collections + Gen2Collections;
}

/// <summary>How long one system of the frame schedule took during the last frame (<see cref="GameDiagnostics.Systems"/>).</summary>
/// <param name="Name">The system's name in <see cref="FrameSchedule"/>.</param>
/// <param name="Phase">Its phase.</param>
/// <param name="Milliseconds">Its time during the frame; a fixed phase adds up every step of the frame.</param>
public readonly record struct SystemTiming(string Name, FramePhase Phase, double Milliseconds);

/// <summary>
/// The game's live measurements (<see cref="Game.Diagnostics"/>): the last frame's times, allocations, collections,
/// graphics work and audio voices, and the time of every system of the frame schedule. Off by default; a run measures
/// while <see cref="Enabled"/> is set or while a tool listens to the engine's meter (<see cref="MeterName"/>), and costs
/// nothing measurable otherwise.
/// </summary>
/// <remarks>
/// The same numbers are published as <see cref="System.Diagnostics.Metrics"/> instruments of the meter
/// <c>"MicroEngine"</c>, so <c>dotnet-counters monitor --counters MicroEngine</c>, <c>dotnet-monitor</c> or an
/// OpenTelemetry exporter read them without code in the game. The runtime's own meter (<c>System.Runtime</c>) adds the
/// heap size, GC pauses and thread-pool figures.
/// </remarks>
/// <example>
/// <code>
/// // In Load: measure, and show the numbers when F3 is held.
/// Diagnostics.Enabled = true;
/// // In DrawAboveScenes:
/// var frame = Diagnostics.LastFrame;
/// Font.Draw(renderer, $"{frame.Total:0.0} ms  {frame.Graphics.DrawCalls} draws  {frame.Allocated} B", new Vector2(4, 4), Color.White);
/// </code>
/// </example>
public sealed class GameDiagnostics
{
    /// <summary>The name of the engine's meter (<see cref="Meter"/>): <c>"MicroEngine"</c>.</summary>
    public const string MeterName = "MicroEngine";

    private static readonly Meter EngineMeter = new(MeterName);
    private static GameDiagnostics? s_current;
    private static readonly Instrument[] Instruments =
    [
        EngineMeter.CreateObservableGauge("microengine.frame.time", () => s_current?.LastFrame.Interval ?? 0, "ms", "Time between the starts of the last two frames."),
        EngineMeter.CreateObservableGauge("microengine.frame.update", () => s_current?.LastFrame.Update ?? 0, "ms", "The last frame's fixed steps and updates."),
        EngineMeter.CreateObservableGauge("microengine.frame.render", () => s_current?.LastFrame.Render ?? 0, "ms", "The last frame's drawing."),
        EngineMeter.CreateObservableGauge("microengine.frame.present", () => s_current?.LastFrame.Present ?? 0, "ms", "The last frame's presentation, display wait included."),
        EngineMeter.CreateObservableGauge("microengine.frame.allocated", () => s_current?.LastFrame.Allocated ?? 0, "By", "Bytes the game's thread allocated during the last frame."),
        EngineMeter.CreateObservableGauge("microengine.graphics.draw_calls", () => (long)(s_current?.LastFrame.Graphics.DrawCalls ?? 0), "{draw}", "Draw calls of the last frame."),
        EngineMeter.CreateObservableGauge("microengine.audio.voices", () => s_current?.LastFrame.Audio.ActiveVoices ?? 0, "{voice}", "Sounds playing at the end of the last frame."),
    ];

    private readonly Game _game;
    private SystemTiming[] _systems = [];
    private DeviceContract? _device;
    private bool _deviceResolved;
    private long _frameStart;
    private bool _wasActive;

    internal GameDiagnostics(Game game) => _game = game;

    /// <summary>Whether the game measures every frame. Off by default; a tool listening to the engine's meter measures too (<see cref="Active"/>).</summary>
    public bool Enabled { get; set; }

    /// <summary>Whether frames are measured now: <see cref="Enabled"/>, or a listener of the engine's meter is attached.</summary>
    public bool Active
    {
        get
        {
            if (Enabled) return true;
            foreach (var instrument in Instruments) if (instrument.Enabled) return true;
            return false;
        }
    }

    /// <summary>The last measured frame; <c>default</c> (frame 0) until a frame is measured. During an update it is the previous frame.</summary>
    public FrameStatistics LastFrame { get; private set; }

    /// <summary>
    /// Every system of the frame schedule with its time during the last measured frame, in the order phases run and, within
    /// a phase, in the order its systems run. Empty until a frame is measured.
    /// </summary>
    public IReadOnlyList<SystemTiming> Systems => _systems;

    /// <summary>
    /// Starts a frame: true when it is measured. Turns the schedule's timing and the input trace
    /// (<see cref="Input.InputSystem.Trace"/>, read through <see cref="Input.InputSystem.Routed"/>) on or off to follow
    /// <see cref="Active"/>.
    /// </summary>
    internal bool BeginFrame(FrameSchedule schedule)
    {
        bool active = Active;
        if (active != _wasActive)
        {
            schedule.MeasureTime = active;
            _game.InputSystem.Trace = active;
            _wasActive = active;
            if (!active) _frameStart = 0;
        }
        if (!active) return false;
        s_current = this;
        schedule.ResetTimes();
        return true;
    }

    /// <summary>A measured frame's starting point.</summary>
    internal Mark Start()
    {
        if (!_deviceResolved)
        {
            _device = _game.Graphics is DeviceGraphics graphics ? graphics.Device : null;
            _deviceResolved = true;
        }
        long now = Stopwatch.GetTimestamp();
        double interval = _frameStart == 0 ? 0 : Stopwatch.GetElapsedTime(_frameStart, now).TotalMilliseconds;
        _frameStart = now;
        return new Mark(interval, GC.GetAllocatedBytesForCurrentThread(), GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2),
            _device?.Counters ?? default);
    }

    /// <summary>Records a measured frame that started at <paramref name="start"/>.</summary>
    internal void End(in Mark start, long frame, double events, double update, double render, double present, int steps, FrameSchedule schedule)
    {
        var counters = _device?.Counters ?? default;
        var graphics = (counters - start.Graphics) with { LiveResources = counters.LiveResources };
        LastFrame = new FrameStatistics(frame, start.Interval, events, update, render, present, steps,
            GC.GetAllocatedBytesForCurrentThread() - start.Allocated,
            GC.CollectionCount(0) - start.Gen0, GC.CollectionCount(1) - start.Gen1, GC.CollectionCount(2) - start.Gen2,
            graphics, _game.Audio?.Statistics ?? default);
        if (_systems.Length != schedule.Count) _systems = Describe(schedule);
        int i = 0;
        foreach (var phase in Phases)
            foreach (double time in schedule.Times(phase))
            {
                _systems[i] = _systems[i] with { Milliseconds = time };
                i++;
            }
    }

    private static readonly FramePhase[] Phases = Enum.GetValues<FramePhase>();

    private static SystemTiming[] Describe(FrameSchedule schedule)
    {
        var systems = new List<SystemTiming>(schedule.Count);
        foreach (var phase in Phases)
            foreach (var name in schedule.Order(phase)) systems.Add(new SystemTiming(name, phase, 0));
        return [.. systems];
    }

    internal readonly record struct Mark(double Interval, long Allocated, int Gen0, int Gen1, int Gen2, GraphicsCounters Graphics);
}
