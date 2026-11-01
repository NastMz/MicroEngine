using System.Reflection;
using MicroEngine.Graphics;

namespace MicroEngine.Core;

/// <summary>Screen orientations a game allows on phones (<c>MicroEngineOrientation</c> in its project).</summary>
public enum AppOrientation
{
    Landscape,
    Portrait,
    Any,
}

/// <summary>
/// The application identity a game declares once, in its project file (docs/guide/app-identity.md):
/// <c>ApplicationTitle</c>, <c>ApplicationId</c>, <c>Version</c>, <c>MicroEngineIcon</c> and
/// <c>MicroEngineOrientation</c>. The MicroEngine build targets write it into the game assembly; packaging turns it into
/// each system's metadata, and platforms read it at run time (application name, window icon, phone orientation).
/// </summary>
public sealed record AppIdentity(string Title, string? Id, string Version, AppOrientation Orientation)
{
    /// <summary>Name of the embedded PNG resource holding the icon.</summary>
    public const string IconResource = "MicroEngine.AppIcon.png";
    private const string Prefix = "MicroEngine.";

    /// <summary>The game's icon, decoded from the assembly; null when the project declares none.</summary>
    public PixelImage? Icon { get; init; }

    /// <summary>The identity of the assembly that defines <paramref name="game"/>'s type.</summary>
    public static AppIdentity? Of(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);
        return Of(game.GetType().Assembly);
    }

    /// <summary>
    /// Reads the identity the build targets wrote into <paramref name="assembly"/>; null for an assembly built without
    /// them (a test or a game that predates them), so callers keep their defaults.
    /// </summary>
    public static AppIdentity? Of(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        var values = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Where(a => a.Key.StartsWith(Prefix, StringComparison.Ordinal))
            .ToDictionary(a => a.Key[Prefix.Length..], a => a.Value ?? "", StringComparer.Ordinal);
        if (!values.TryGetValue("ApplicationTitle", out var title) || title.Length == 0) return null;
        var orientation = values.TryGetValue("Orientation", out var o) && Enum.TryParse<AppOrientation>(o, ignoreCase: true, out var parsed)
            ? parsed : AppOrientation.Landscape;
        string version = values.TryGetValue("Version", out var v) && v.Length > 0 ? v : "0.0.0";
        string? id = values.TryGetValue("ApplicationId", out var i) && i.Length > 0 ? i : null;
        PixelImage? icon = null;
        using (var stream = assembly.GetManifestResourceStream(IconResource))
        {
            if (stream is not null)
            {
                using var buffer = new MemoryStream();
                stream.CopyTo(buffer);
                icon = PixelImage.FromPng(buffer.ToArray());
            }
        }
        return new AppIdentity(title, id, version, orientation) { Icon = icon };
    }
}
