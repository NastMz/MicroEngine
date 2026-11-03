using System.Numerics;
using MicroEngine.Audio;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Platform;

namespace MicroEngine.Core;

/// <summary>The subsystems a host may supply to a profile instead of the profile's own.</summary>
[Flags]
public enum PlatformParts
{
    /// <summary>The host may replace nothing.</summary>
    None = 0,
    /// <summary>Replace the audio device (<see cref="PlatformRequest.Audio"/>).</summary>
    Audio = 1,
    /// <summary>Add input sources next to the backend's (<see cref="PlatformRequest.ExtraInput"/>).</summary>
    Input = 2,
}

/// <summary>
/// What a host asks of a platform: the game's settings, window options and any subsystem it supplies itself. Every
/// option is explicit and has a documented default; none depends on how the loop advances time (that is the loop's
/// business only, <see cref="RunOptions.TimeMode"/>). A backend honours the supplied parts by building its audio device
/// with <see cref="CreateAudio"/> and its input with <see cref="CombineInput"/>; <see cref="PlatformProfile.Create"/>
/// checks that it did.
/// </summary>
public sealed record PlatformRequest(GameSettings Settings)
{
    /// <summary>Window scale; null uses <see cref="GameSettings.Scale"/>.</summary>
    public int? Scale { get; init; }
    /// <summary>Renders into a hidden window (captures still work). A hidden window never waits for the display.</summary>
    public bool Hidden { get; init; }
    /// <summary>
    /// Whether the frames of a visible window may wait for the display's refresh (true by default): VSync as
    /// <see cref="GraphicsSettings.VSync"/> asks, and frames paced to the display (<see cref="IDisplayPacing"/>). False
    /// never waits, whatever the settings say, for checks that run as fast as they can.
    /// </summary>
    public bool WaitForDisplay { get; init; } = true;
    /// <summary>
    /// Whether the backend starts its gamepad subsystem only after the first frame is presented (false by default:
    /// with the platform). Hosts that present a start screen while the game loads set it, so a slow gamepad start-up
    /// (about a second on some Windows machines) never keeps the window from appearing; pads already connected are
    /// reported when it starts.
    /// </summary>
    public bool StartGamepadsAfterFirstFrame { get; init; }
    /// <summary>No sound: the backend opens no audio device.</summary>
    public bool Mute { get; init; }
    /// <summary>
    /// Whether the backend may open a physical audio device: <see cref="AudioOutput.Automatic"/> (the default) and
    /// <see cref="AudioOutput.Device"/> open one, <see cref="AudioOutput.Silent"/> opens none.
    /// </summary>
    public AudioOutput AudioOutput { get; init; }
    /// <summary>
    /// Size factor of the PNGs written by <see cref="IPlatform.SaveScreenshot"/>: 3 for pixel-art canvases (so their
    /// pixels are visible), 1 for smoothly scaled ones (already high-resolution).
    /// </summary>
    public int CaptureScale { get; init; } = Settings.Scaling == Graphics.CanvasScaling.Smooth ? 1 : 3;
    /// <summary>Creates the audio device used instead of the backend's; the platform owns and disposes it.</summary>
    public Func<IAudioDevice>? Audio { get; init; }
    /// <summary>Input sources read together with the backend's (an on-screen pad, a network peer, a test).</summary>
    public IReadOnlyList<IInputSource> ExtraInput { get; init; } = [];
    /// <summary>The game's application identity (name, id, icon, orientation); null keeps the backend's defaults.</summary>
    public AppIdentity? Identity { get; init; }

    /// <summary>Whether the backend should open its own audio device: no device was supplied, no mute, and the output is not silent.</summary>
    public bool OpensOwnAudio => Audio is null && !Mute && AudioOutput != AudioOutput.Silent;

    /// <summary>
    /// The audio device a backend uses for this request: the one the host supplied (<see cref="Audio"/>), else the
    /// backend's own (<paramref name="openOwn"/>) when <see cref="OpensOwnAudio"/>, else a silent device. The backend
    /// owns and disposes the result.
    /// </summary>
    public IAudioDevice CreateAudio(Func<IAudioDevice> openOwn)
    {
        ArgumentNullException.ThrowIfNull(openOwn);
        return Audio is { } supplied ? supplied() : OpensOwnAudio ? openOwn() : new SilentAudioDevice();
    }

    /// <summary>The input a backend reports for this request: its own source, read together with <see cref="ExtraInput"/> when there are any.</summary>
    public IInputSource CombineInput(IInputSource own)
    {
        ArgumentNullException.ThrowIfNull(own);
        return ExtraInput.Count == 0 ? own : new CompositeInput([own, .. ExtraInput]);
    }
}

/// <summary>
/// Which GPU a game prefers on machines with an integrated and a dedicated one. The system has the last word: on
/// Windows, Direct3D 11 and OpenGL use the adapter Windows assigns to the application (Settings, Display, Graphics), which
/// no backend overrides; SDL passes the preference to its Metal (macOS, iOS) and GPU (Direct3D 12, Vulkan, Metal)
/// renderers; raylib's OpenGL ignores it.
/// </summary>
public enum GpuPreference
{
    /// <summary>The backend's and system's default (the high-performance GPU for SDL's Metal and GPU renderers).</summary>
    Automatic,
    /// <summary>Ask for the high-performance (usually dedicated) GPU where the backend can choose.</summary>
    HighPerformance,
    /// <summary>Ask for the low-power (usually integrated) GPU where the backend can choose: light games on laptops.</summary>
    PowerSaving,
}

/// <summary>Where a run's sound may go (<see cref="PlatformRequest.AudioOutput"/>, the launcher's <c>--audio</c>).</summary>
public enum AudioOutput
{
    /// <summary>
    /// The default. A platform opens a physical device, as for <see cref="Device"/>; the launcher's <c>--audio auto</c>
    /// asks for none when the run is in deterministic time (scripts), whose sound would not follow the wall clock.
    /// </summary>
    Automatic,
    /// <summary>Always a physical device, for listening checks and package checks that must prove a device opens.</summary>
    Device,
    /// <summary>Never a physical device.</summary>
    Silent,
}

/// <summary>
/// A named way to build an <see cref="IPlatform"/>: an official backend ("sdl", "raylib", "headless") or one a
/// developer registers. The profile decides which parts a host may replace (<see cref="Replaceable"/>), because some
/// subsystems share state with the window and cannot be swapped independently.
/// </summary>
public sealed class PlatformProfile : Hosting.IHostProfile
{
    private readonly Func<PlatformRequest, IPlatform> _create;

    /// <summary>Creates a profile.</summary>
    /// <param name="name">The name hosts select it by, case-insensitive; must not be empty.</param>
    /// <param name="description">One line shown by <c>--list-platforms</c>.</param>
    /// <param name="create">Builds the platform for a request.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="create"/> is null.</exception>
    public PlatformProfile(string name, string description, Func<PlatformRequest, IPlatform> create)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Description = description ?? "";
        _create = create ?? throw new ArgumentNullException(nameof(create));
    }

    /// <summary>The name hosts select the profile by (compared case-insensitively).</summary>
    public string Name { get; }
    /// <summary>A one-line description for listings.</summary>
    public string Description { get; }
    /// <summary>The parts a host may supply in its request (audio and input by default).</summary>
    public PlatformParts Replaceable { get; init; } = PlatformParts.Audio | PlatformParts.Input;
    /// <summary>Returns why the profile cannot run in this process (system, missing library), or null when it can.</summary>
    public Func<string?> CheckSupport { get; init; } = () => null;

    /// <summary>Why the profile cannot run in this process, or null when it can; evaluates <see cref="CheckSupport"/> on each read.</summary>
    public string? UnsupportedReason => CheckSupport();

    /// <summary>
    /// Builds the platform for <paramref name="request"/>. The supplied parts (<see cref="PlatformRequest.Audio"/>,
    /// <see cref="PlatformRequest.ExtraInput"/>) are composed into the backend when it is built, never by wrapping it,
    /// so the platform returned is the backend itself with all its capabilities. Throws
    /// <see cref="PlatformConfigurationException"/> for a request the profile cannot honour, and when the backend did not
    /// use the parts the request supplied.
    /// </summary>
    public IPlatform Create(PlatformRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (UnsupportedReason is { } reason) throw new PlatformConfigurationException($"Platform '{Name}' cannot run here: {reason}.");
        if (request.Audio is not null && !Replaceable.HasFlag(PlatformParts.Audio))
            throw new PlatformConfigurationException($"Platform '{Name}' does not allow replacing its audio device.");
        if (request.ExtraInput.Count > 0 && !Replaceable.HasFlag(PlatformParts.Input))
            throw new PlatformConfigurationException($"Platform '{Name}' does not allow adding input sources.");
        IAudioDevice? supplied = null;
        var forwarded = request.Audio is { } make ? request with { Audio = () => supplied = make() } : request;
        var platform = _create(forwarded);
        string? ignored = null;
        if (request.Audio is not null && !ReferenceEquals(platform.Audio, supplied))
            ignored = $"Platform '{Name}' does not use the audio device the request supplies: a backend builds its device with PlatformRequest.CreateAudio, or its profile does not declare PlatformParts.Audio.";
        else if (request.ExtraInput.Count > 0 && !(platform.Input is CompositeInput combined && request.ExtraInput.All(combined.Sources.Contains)))
            ignored = $"Platform '{Name}' does not read the input sources the request adds: a backend builds its input with PlatformRequest.CombineInput, or its profile does not declare PlatformParts.Input.";
        if (ignored is null) return platform;
        try { platform.Dispose(); }
        finally { (supplied as IDisposable)?.Dispose(); }
        throw new PlatformConfigurationException(ignored);
    }

    /// <summary>The profile's <see cref="Name"/>.</summary>
    public override string ToString() => Name;
}

/// <summary>
/// The platforms a host can choose from, in order of preference: the first one that can run here is the default.
/// Hosts select by name (a <c>--platform</c> argument, a setting), never game code.
/// </summary>
public sealed class PlatformCatalog
{
    private readonly List<PlatformProfile> _profiles = [];

    /// <summary>The registered profiles in order of preference.</summary>
    public IReadOnlyList<PlatformProfile> Profiles => _profiles;

    /// <summary>Registers a profile after the existing ones.</summary>
    /// <returns>This catalog, for chaining.</returns>
    /// <exception cref="ArgumentException">A profile with the same name (ignoring case) is already registered.</exception>
    public PlatformCatalog Add(PlatformProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (Find(profile.Name) is not null) throw new ArgumentException($"A platform named '{profile.Name}' is already registered.", nameof(profile));
        _profiles.Add(profile);
        return this;
    }

    /// <summary>The profile named <paramref name="name"/> (ignoring case), or null.</summary>
    public PlatformProfile? Find(string name) => Hosting.ProfileSelection.Find(_profiles, name);

    /// <summary>The first registered profile that can run in this process.</summary>
    /// <exception cref="PlatformConfigurationException">No profile is registered, or none can run here.</exception>
    public PlatformProfile Default => Hosting.ProfileSelection.Default(_profiles) is (var profile, var error) && profile is not null
        ? profile : throw new PlatformConfigurationException(error!);

    /// <summary>The named profile, or <see cref="Default"/> for null or empty.</summary>
    /// <exception cref="PlatformConfigurationException">No profile has that name, or it cannot run here.</exception>
    public PlatformProfile Resolve(string? name) => Hosting.ProfileSelection.Resolve(_profiles, name) is (var profile, var error) && profile is not null
        ? profile : throw new PlatformConfigurationException(error!);
}

/// <summary>A platform request that cannot be met: unknown or unsupported profile, or a part the profile does not let a host replace.</summary>
public sealed class PlatformConfigurationException(string message) : Exception(message);

/// <summary>Several input sources read as one: a key or button is down when any source has it down; the stick or axis with the largest deflection wins.</summary>
public sealed class CompositeInput(IReadOnlyList<IInputSource> sources) : IInputSource
{
    private readonly IInputSource[] _sources = [.. sources ?? throw new ArgumentNullException(nameof(sources))];

    /// <summary>The sources read, in order.</summary>
    public IReadOnlyList<IInputSource> Sources => _sources;

    /// <inheritdoc/>
    public bool IsKeyDown(Key key)
    {
        foreach (var s in _sources) if (s.IsKeyDown(key)) return true;
        return false;
    }

    /// <inheritdoc/>
    public bool IsPadDown(PadButton button)
    {
        foreach (var s in _sources) if (s.IsPadDown(button)) return true;
        return false;
    }

    /// <inheritdoc/>
    public float GetPadAxis(PadAxis axis)
    {
        float best = 0;
        foreach (var s in _sources)
        {
            float v = s.GetPadAxis(axis);
            if (MathF.Abs(v) > MathF.Abs(best)) best = v;
        }
        return best;
    }

    /// <inheritdoc/>
    public bool PadConnected => _sources.Any(s => s.PadConnected);

    /// <summary>The style of the first source with a connected gamepad.</summary>
    public GamepadStyle PadStyle => _sources.FirstOrDefault(s => s.PadConnected)?.PadStyle ?? GamepadStyle.Xbox;

    /// <summary>The pointer of the first source whose pointer is present.</summary>
    public PointerState Pointer => _sources.Select(s => s.Pointer).FirstOrDefault(p => p.Present);

    /// <inheritdoc/>
    public Vector2 LeftStick
    {
        get
        {
            var best = Vector2.Zero;
            foreach (var s in _sources)
            {
                var v = s.LeftStick;
                if (v.LengthSquared() > best.LengthSquared()) best = v;
            }
            return best;
        }
    }
}
