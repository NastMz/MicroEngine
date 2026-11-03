using System.Diagnostics.CodeAnalysis;
using MicroEngine;
using MicroEngine.Audio;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Platform;
using MicroEngine.Scenes;

namespace Examples;

#region save-storage
/// <summary>
/// Where save slots live is an ISaveStorage: files on desktops and phones, localStorage in browsers. This one keeps them
/// in memory (tests, a demo kiosk); a cloud service or a console's save API would implement the same six members.
/// </summary>
public sealed class MemorySaveStorage : ISaveStorage
{
    private readonly Dictionary<string, string> _slots = [];

    public string Location => "memory";
    public string? Read(string slot) => _slots.GetValueOrDefault(slot);
    public void Write(string slot, string text) => _slots[slot] = text;
    public bool Exists(string slot) => _slots.ContainsKey(slot);
    public void Delete(string slot) => _slots.Remove(slot);
    public void SetAside(string slot)
    {
        if (_slots.Remove(slot, out var text)) _slots[slot + ".corrupt"] = text;
    }
}
#endregion

#region transition
/// <summary>A transition of your own: two curtains that close, then open on the new scene.</summary>
public sealed class CurtainTransition(float duration = 0.6f) : Transition(duration)
{
    // Cover goes 0 -> 1 while the old scene is hidden; the scene changes at 1, then Cover goes back to 0.
    public override void Draw(IRenderer r)
    {
        float half = r.Width / 2f * Cover;
        r.FillRect(new RectF(0, 0, half, r.Height), Color.Hex("b13e53"));
        r.FillRect(new RectF(r.Width - half, 0, half, r.Height), Color.Hex("b13e53"));
    }
}
#endregion

#region platform
/// <summary>
/// A platform of your own does not have to start from nothing: this one wraps any other platform and counts the
/// frames it presents (telemetry, a frame-rate overlay, a recorder would hook in the same place). It forwards every
/// member, the capability query included, so the wrapped platform's optional features (display control, touch screen,
/// diagnostics...) stay available.
/// </summary>
public sealed class CountingPlatform(IPlatform inner) : IPlatform
{
    public long Frames { get; private set; }

    public IGraphicsDevice Graphics => inner.Graphics;
    public IAudioDevice Audio => inner.Audio;
    public IInputSource Input => inner.Input;
    public IMonotonicClock Clock => inner.Clock;
    public bool PumpEvents(IEngineEventSink sink) => inner.PumpEvents(sink);
    public ISurface? MainSurface => inner.MainSurface;
    public bool CanBlock => inner.CanBlock;
    public bool TryGetCapability<T>([NotNullWhen(true)] out T? capability) where T : class => inner.TryGetCapability(out capability);
    public Color Letterbox { get => inner.Letterbox; set => inner.Letterbox = value; }
    public IRenderer BeginFrame() => inner.BeginFrame();
    public void EndFrame()
    {
        inner.EndFrame();
        Frames++;
    }
    public void SaveScreenshot(string path) => inner.SaveScreenshot(path);
    public void ToggleFullscreen() => inner.ToggleFullscreen();
    public void Dispose()
    {
        Console.WriteLine($"{Frames} frames presented");
        inner.Dispose();
    }

    /// <summary>A profile for the launcher's catalog: "--platform sdl-counted" runs SDL through this wrapper.</summary>
    public static PlatformProfile Wrap(PlatformProfile profile) => new(
        $"{profile.Name}-counted", $"{profile.Description}, counting frames",
        request => new CountingPlatform(profile.Create(request)))
    {
        CheckSupport = () => profile.UnsupportedReason,   // runs wherever the wrapped platform runs
        Replaceable = profile.Replaceable,
    };
}
#endregion
