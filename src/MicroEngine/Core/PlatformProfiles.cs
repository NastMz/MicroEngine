using System.Numerics;
using MicroEngine.Audio;
using MicroEngine.Graphics;
using MicroEngine.Input;

namespace MicroEngine.Core;

/// <summary>The subsystems a host may supply to a profile instead of the profile's own.</summary>
[Flags]
public enum PlatformParts
{
    None = 0,
    /// <summary>Replace the audio device (<see cref="PlatformRequest.Audio"/>).</summary>
    Audio = 1,
    /// <summary>Add input sources next to the backend's (<see cref="PlatformRequest.ExtraInput"/>).</summary>
    Input = 2,
}

/// <summary>
/// What a host asks of a platform: the game's settings, window options and any subsystem it supplies itself.
/// A backend reads the window options and must not open its own audio device when <see cref="OpensOwnAudio"/> is false.
/// </summary>
public sealed record PlatformRequest(GameSettings Settings)
{
    /// <summary>Window scale; null uses <see cref="GameSettings.Scale"/>.</summary>
    public int? Scale { get; init; }
    public bool Hidden { get; init; }
    /// <summary>One simulation step per frame instead of wall-clock time (scripted runs).</summary>
    public bool FixedStep { get; init; }
    public bool Mute { get; init; }
    /// <summary>Size factor of the PNGs written by <see cref="IPlatform.SaveScreenshot"/>.</summary>
    public int CaptureScale { get; init; } = 3;
    /// <summary>Creates the audio device used instead of the backend's; the platform disposes it.</summary>
    public Func<IAudioDevice>? Audio { get; init; }
    /// <summary>Input sources read together with the backend's (an on-screen pad, a network peer, a test).</summary>
    public IReadOnlyList<IInputSource> ExtraInput { get; init; } = [];
    /// <summary>The game's application identity (name, id, icon, orientation); null keeps the backend's defaults.</summary>
    public AppIdentity? Identity { get; init; }

    /// <summary>Whether the backend should open its own audio device.</summary>
    public bool OpensOwnAudio => Audio is null && !Mute;
}

/// <summary>
/// A named way to build an <see cref="IPlatform"/>: an official backend ("sdl", "raylib", "headless") or one a
/// developer registers. The profile decides which parts a host may replace (<see cref="Replaceable"/>), because some
/// subsystems share state with the window and cannot be swapped independently.
/// </summary>
public sealed class PlatformProfile
{
    private readonly Func<PlatformRequest, IPlatform> _create;

    public PlatformProfile(string name, string description, Func<PlatformRequest, IPlatform> create)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Description = description ?? "";
        _create = create ?? throw new ArgumentNullException(nameof(create));
    }

    public string Name { get; }
    public string Description { get; }
    public PlatformParts Replaceable { get; init; } = PlatformParts.Audio | PlatformParts.Input;
    /// <summary>Returns why the profile cannot run in this process (system, missing library), or null when it can.</summary>
    public Func<string?> CheckSupport { get; init; } = () => null;

    public string? UnsupportedReason => CheckSupport();

    /// <summary>Builds the platform, applying the request's replacements; throws <see cref="PlatformConfigurationException"/> for a request the profile cannot honour.</summary>
    public IPlatform Create(PlatformRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (UnsupportedReason is { } reason) throw new PlatformConfigurationException($"Platform '{Name}' cannot run here: {reason}.");
        if (request.Audio is not null && !Replaceable.HasFlag(PlatformParts.Audio))
            throw new PlatformConfigurationException($"Platform '{Name}' does not allow replacing its audio device.");
        if (request.ExtraInput.Count > 0 && !Replaceable.HasFlag(PlatformParts.Input))
            throw new PlatformConfigurationException($"Platform '{Name}' does not allow adding input sources.");
        var platform = _create(request);
        if (request.Audio is null && request.ExtraInput.Count == 0) return platform;
        IAudioDevice? audio = null;
        try
        {
            audio = request.Audio?.Invoke();
            return new ComposedPlatform(platform, audio, request.ExtraInput);
        }
        catch
        {
            (audio as IDisposable)?.Dispose();
            platform.Dispose();
            throw;
        }
    }

    public override string ToString() => Name;
}

/// <summary>
/// The platforms a host can choose from, in order of preference: the first one that can run here is the default.
/// Hosts select by name (a <c>--platform</c> argument, a setting), never game code.
/// </summary>
public sealed class PlatformCatalog
{
    private readonly List<PlatformProfile> _profiles = [];

    public IReadOnlyList<PlatformProfile> Profiles => _profiles;

    public PlatformCatalog Add(PlatformProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (Find(profile.Name) is not null) throw new ArgumentException($"A platform named '{profile.Name}' is already registered.", nameof(profile));
        _profiles.Add(profile);
        return this;
    }

    public PlatformProfile? Find(string name) => _profiles.Find(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>The first registered profile that can run in this process.</summary>
    public PlatformProfile Default => _profiles.Find(p => p.UnsupportedReason is null)
        ?? throw new PlatformConfigurationException(_profiles.Count == 0
            ? "No platform is registered."
            : $"No platform can run here. {string.Join(" ", _profiles.Select(p => $"{p.Name}: {p.UnsupportedReason}."))}");

    /// <summary>The named profile, or <see cref="Default"/> for null or empty.</summary>
    public PlatformProfile Resolve(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return Default;
        var profile = Find(name) ?? throw new PlatformConfigurationException(
            $"Unknown platform '{name}'. Available platforms: {string.Join(", ", _profiles.Select(p => p.Name))}.");
        if (profile.UnsupportedReason is { } reason) throw new PlatformConfigurationException($"Platform '{profile.Name}' cannot run here: {reason}.");
        return profile;
    }
}

/// <summary>A platform request that cannot be met: unknown or unsupported profile, or a part the profile does not let a host replace.</summary>
public sealed class PlatformConfigurationException(string message) : Exception(message);

/// <summary>A backend platform with a replaced audio device and/or extra input sources; it owns all of them.</summary>
internal sealed class ComposedPlatform : IPlatform
{
    private readonly IPlatform _inner;
    private readonly IAudioDevice? _audio;
    private readonly IInputSource _input;

    public ComposedPlatform(IPlatform inner, IAudioDevice? audio, IReadOnlyList<IInputSource> extraInput)
    {
        _inner = inner;
        _audio = audio;
        _input = extraInput.Count == 0 ? inner.Input : new CompositeInput([inner.Input, .. extraInput]);
    }

    public IGraphicsDevice Graphics => _inner.Graphics;
    public IAudioDevice Audio => _audio ?? _inner.Audio;
    public IInputSource Input => _input;
    public bool CloseRequested => _inner.CloseRequested;
    public double Now => _inner.Now;
    public bool RealTime => _inner.RealTime;
    public bool Suspended => _inner.Suspended;
    public ITouchScreen? Touch => _inner.Touch;
    public void PollEvents() => _inner.PollEvents();
    public IRenderer BeginFrame() => _inner.BeginFrame();
    public void EndFrame() => _inner.EndFrame();
    public void SaveScreenshot(string path) => _inner.SaveScreenshot(path);
    public void ToggleFullscreen() => _inner.ToggleFullscreen();

    public void Dispose()
    {
        try { (_audio as IDisposable)?.Dispose(); }
        finally { _inner.Dispose(); }
    }
}

/// <summary>Several input sources read as one: a key or button is down when any source has it down; the stick or axis with the largest deflection wins.</summary>
public sealed class CompositeInput(IReadOnlyList<IInputSource> sources) : IInputSource
{
    private readonly IInputSource[] _sources = [.. sources ?? throw new ArgumentNullException(nameof(sources))];

    public bool IsKeyDown(Key key)
    {
        foreach (var s in _sources) if (s.IsKeyDown(key)) return true;
        return false;
    }

    public bool IsPadDown(PadButton button)
    {
        foreach (var s in _sources) if (s.IsPadDown(button)) return true;
        return false;
    }

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

    public bool PadConnected => _sources.Any(s => s.PadConnected);

    public PointerState Pointer => _sources.Select(s => s.Pointer).FirstOrDefault(p => p.Present);

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
