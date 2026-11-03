namespace MicroEngine.Hosting;

/// <summary>A named way to build a platform that a host can choose (the engine's platform profiles implement it).</summary>
public interface IHostProfile
{
    /// <summary>The name hosts select it by, compared case-insensitively.</summary>
    string Name { get; }
    /// <summary>Why it cannot run in this process, or null when it can.</summary>
    string? UnsupportedReason { get; }
}

/// <summary>The platforms a launch tries, in order, or why none can be tried.</summary>
/// <param name="Candidates">The profiles to try in order; the next one is tried when one fails to start. Empty when <paramref name="Error"/> is set.</param>
/// <param name="Error">Why no profile can be tried (an unknown or unsupported name, an empty catalog), or null.</param>
/// <param name="Notes">Messages for the player about the choice (a preference that was skipped).</param>
public sealed record ProfileChoice<TProfile>(IReadOnlyList<TProfile> Candidates, string? Error, IReadOnlyList<string> Notes) where TProfile : class, IHostProfile;

/// <summary>
/// How every host chooses a platform from its catalog (an ordered list of profiles, the most preferred first). The rules
/// are the launcher's, written once: a named platform (<see cref="LaunchRequest.Platform"/>, else the developer variable
/// <c>MICROENGINE_PLATFORM</c>) is the only candidate; <see cref="LaunchRequest.Headless"/> names the headless platform;
/// otherwise the game's preferred backend comes first, then every other platform that can run here except the headless
/// one (a player who sees no window would not know why), or the catalog's default when that leaves none.
/// </summary>
public static class ProfileSelection
{
    /// <summary>The developer variable that names the platform when the launch names none.</summary>
    public const string PlatformVariable = "MICROENGINE_PLATFORM";

    /// <summary>The profile named <paramref name="name"/> (ignoring case), or null.</summary>
    public static TProfile? Find<TProfile>(IReadOnlyList<TProfile> catalog, string name) where TProfile : class, IHostProfile
    {
        ArgumentNullException.ThrowIfNull(catalog);
        foreach (var p in catalog) if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) return p;
        return null;
    }

    /// <summary>The first profile that can run here, or why there is none.</summary>
    public static (TProfile? Profile, string? Error) Default<TProfile>(IReadOnlyList<TProfile> catalog) where TProfile : class, IHostProfile
    {
        ArgumentNullException.ThrowIfNull(catalog);
        foreach (var p in catalog) if (p.UnsupportedReason is null) return (p, null);
        return (null, catalog.Count == 0
            ? "No platform is registered."
            : $"No platform can run here. {string.Join(" ", catalog.Select(p => $"{p.Name}: {p.UnsupportedReason}."))}");
    }

    /// <summary>The named profile, or <see cref="Default{TProfile}"/> for null or empty; otherwise why it cannot be used.</summary>
    public static (TProfile? Profile, string? Error) Resolve<TProfile>(IReadOnlyList<TProfile> catalog, string? name) where TProfile : class, IHostProfile
    {
        if (string.IsNullOrWhiteSpace(name)) return Default(catalog);
        if (Find(catalog, name) is not { } profile)
            return (null, $"Unknown platform '{name}'. Available platforms: {string.Join(", ", catalog.Select(p => p.Name))}.");
        return profile.UnsupportedReason is { } reason ? (null, $"Platform '{profile.Name}' cannot run here: {reason}.") : (profile, null);
    }

    /// <summary>
    /// The profiles tried when nobody named one: every profile that can run here except the headless one, or the
    /// catalog's default when that leaves none.
    /// </summary>
    public static (IReadOnlyList<TProfile> Profiles, string? Error) Fallbacks<TProfile>(IReadOnlyList<TProfile> catalog) where TProfile : class, IHostProfile
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var windowed = catalog.Where(p => p.UnsupportedReason is null && !string.Equals(p.Name, LaunchRequest.HeadlessName, StringComparison.OrdinalIgnoreCase)).ToList();
        if (windowed.Count > 0) return (windowed, null);
        var (fallback, error) = Default(catalog);
        return fallback is null ? ([], error) : ([fallback], null);
    }

    /// <summary>
    /// The profiles tried when the game prefers <paramref name="backend"/> and nobody named one: that profile first, then
    /// the <see cref="Fallbacks{TProfile}"/>. A preference the catalog lacks or that cannot run here becomes a note and is
    /// skipped.
    /// </summary>
    public static ProfileChoice<TProfile> Preferred<TProfile>(IReadOnlyList<TProfile> catalog, string? backend) where TProfile : class, IHostProfile
    {
        var (fallbacks, error) = Fallbacks(catalog);
        if (error is not null) return new([], error, []);
        if (backend is null) return new(fallbacks, null, []);
        var preferred = Find(catalog, backend);
        if (preferred is null || preferred.UnsupportedReason is not null)
            return new(fallbacks, null, [$"platform: the game prefers '{backend}', which {(preferred is null ? "is not in this build" : $"is unavailable here ({preferred.UnsupportedReason})")}; choosing automatically"]);
        return new([preferred, .. fallbacks.Where(p => p != preferred)], null, []);
    }

    /// <summary>
    /// The profiles a launch tries, in order (see the class remarks). <paramref name="headless"/> stands in for
    /// <c>--headless</c> when the catalog has no headless profile; <paramref name="backend"/> is the game's preferred one.
    /// </summary>
    public static ProfileChoice<TProfile> Choose<TProfile>(IReadOnlyList<TProfile> catalog, LaunchRequest request, HostEnvironment environment, string? backend, TProfile? headless = null)
        where TProfile : class, IHostProfile
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(environment);
        string? name = request.Headless ? LaunchRequest.HeadlessName : request.Platform ?? environment.Variables(PlatformVariable);
        if (request.Headless && headless is not null && Find(catalog, LaunchRequest.HeadlessName) is null) return new([headless], null, []);
        if (name is null) return Preferred(catalog, backend);
        var (profile, error) = Resolve(catalog, name);
        return profile is null ? new([], error, []) : new([profile], null, []);
    }
}
