using MicroEngine;
using MicroEngine.Audio;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Input;
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
