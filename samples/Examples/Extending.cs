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
/// in memory (tests, a demo kiosk); a cloud service or a console's save API would implement the same seven members.
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
    // A copy set aside is not a slot. This storage keeps no times, so LastWrite is null.
    public IReadOnlyList<SlotInfo> List() =>
        [.. _slots.Where(s => !s.Key.EndsWith(".corrupt")).Select(s => new SlotInfo(s.Key, s.Value.Length, null))];
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
/// A platform of your own does not have to start from nothing: a profile can build on another one. This one runs any
/// platform with the request it is given and reports, as it starts, which platform the launcher got and whether it has
/// sound (telemetry or a host's diagnostics would hook in the same place). The platform it returns is the original
/// one, so every feature it has stays available.
/// </summary>
public static class ReportingPlatform
{
    /// <summary>A profile for the launcher's catalog: "--platform sdl-reported" runs SDL and says so.</summary>
    public static PlatformProfile Wrap(PlatformProfile profile) => new(
        $"{profile.Name}-reported", $"{profile.Description}, reported at start",
        request =>
        {
            var platform = profile.Create(request);
            Console.WriteLine($"started {platform} ({(platform.Audio.IsAvailable ? "with" : "without")} sound)");
            return platform;
        })
    {
        CheckSupport = () => profile.UnsupportedReason,   // runs wherever the original platform runs
        Replaceable = profile.Replaceable,
    };
}
#endregion

#region own-controls
/// <summary>A game with controls of its own: none of the default actions, and the engine's menus follow its names.</summary>
public sealed class OwnControlsGame : Game
{
    public override GameSettings Settings { get; } = new("Own Controls");

    protected override InputMap CreateInput() => new InputMap { Names = ActionNames.Default with { Confirm = "jump", Cancel = "back" } }
        .Bind("left", [Key.A], [PadButton.DPadLeft], InputMap.StickDirection.Left)
        .Bind("right", [Key.D], [PadButton.DPadRight], InputMap.StickDirection.Right)
        .Bind("up", [Key.W], [PadButton.DPadUp], InputMap.StickDirection.Up)
        .Bind("down", [Key.S], [PadButton.DPadDown], InputMap.StickDirection.Down)
        .Bind("jump", [Key.Space], [PadButton.A])
        .Bind("back", [Key.Escape], [PadButton.B])
        .Bind("pause", [Key.P], [PadButton.Start]);

    protected override void Load() { }
}
#endregion

#region audio-effect
/// <summary>
/// An audio effect of your own: the volume of a bus wobbles <see cref="Rate"/> times a second. Added to a bus like the
/// built-in ones: <c>Audio.Bus("sfx").Add(new Tremolo { Rate = 6 })</c>.
/// </summary>
public sealed class Tremolo : AudioEffect
{
    // The audio thread's own copies: it never reads what the game's thread writes.
    private float _rate = 5, _depth = 0.5f, _phase, _step;
    private int _channels = 2;

    public Tremolo() : base(parameters: 2)
    {
        SetParameter(0, 5);
        SetParameter(1, 0.5f);
    }

    /// <summary>Wobbles per second.</summary>
    public float Rate { get => GetParameter(0); set => SetParameter(0, Math.Clamp(value, 0.1f, 40)); }
    /// <summary>How far the volume drops, 0 (not at all) to 1 (to silence).</summary>
    public float Depth { get => GetParameter(1); set => SetParameter(1, Math.Clamp(value, 0, 1)); }

    // The game's thread, once, when the effect is added to a bus: the place to allocate.
    protected override void Prepare(int sampleRate, int channels) => (_step, _channels) = (MathF.Tau / sampleRate, channels);

    // The audio thread, at the exact moment in the sound of the update that set the property.
    protected override void OnParameter(int index, float value)
    {
        if (index == 0) _rate = value;
        else _depth = value;
    }

    protected override void Reset() => _phase = 0;

    // The audio thread: interleaved samples, changed in place. No allocation, no lock, no exception.
    protected override void Process(Span<float> buffer)
    {
        for (int i = 0; i < buffer.Length; i += _channels)
        {
            float gain = 1 - _depth * (0.5f - 0.5f * MathF.Cos(_phase));
            _phase += _step * _rate;
            if (_phase >= MathF.Tau) _phase -= MathF.Tau;
            for (int c = 0; c < _channels; c++) buffer[i + c] *= gain;
        }
    }
}
#endregion
