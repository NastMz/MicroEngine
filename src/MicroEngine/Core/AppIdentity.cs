using System.Reflection;
using MicroEngine.Graphics;

namespace MicroEngine.Core;

/// <summary>Screen orientations a game allows on phones (<c>MicroEngineOrientation</c> in its project).</summary>
public enum AppOrientation
{
    /// <summary>Wider than tall (the default).</summary>
    Landscape,
    /// <summary>Taller than wide.</summary>
    Portrait,
    /// <summary>Follows the device.</summary>
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
    /// <summary>Name of the embedded PNG resource holding the start screen's image (<c>MicroEngineSplashImage</c>).</summary>
    public const string SplashImageResource = "MicroEngine.SplashImage.png";
    private const string Prefix = "MicroEngine.";

    /// <summary>The game's icon, decoded from the assembly; null when the project declares none.</summary>
    public PixelImage? Icon { get; init; }

    /// <summary>
    /// Whether the game opens on its start screen (<c>MicroEngineSplash</c>, true by default): the system's launch
    /// screen on phones and the loading screen in browsers show <see cref="SplashImage"/> on <see cref="SplashColor"/>,
    /// and the engine keeps showing it while the game loads, so no black screen comes before the first frame.
    /// </summary>
    public bool Splash { get; init; } = true;
    /// <summary>The start screen's background (<c>MicroEngineSplashColor</c>); null: <see cref="EdgeColor"/> of the image.</summary>
    public Color? SplashBackground { get; init; }
    /// <summary>The start screen's own image (<c>MicroEngineSplashImage</c>); null: the icon.</summary>
    public PixelImage? SplashImageOverride { get; init; }
    /// <summary>Whether the start screen says "Made with MicroEngine" (<c>MicroEngineSplashBranding</c>, false by default).</summary>
    public bool SplashBranding { get; init; }
    /// <summary>The image the start screen shows: <see cref="SplashImageOverride"/>, else the icon (null when there is neither).</summary>
    public PixelImage? SplashImage => SplashImageOverride ?? Icon;
    /// <summary>The start screen's background colour: <see cref="SplashBackground"/>, else the edge of its image, else near black.</summary>
    public Color SplashColor => SplashBackground ?? (SplashImage is { } image ? EdgeColor(image) : DefaultBackground);

    /// <summary>The background used when nothing gives one.</summary>
    public static readonly Color DefaultBackground = Color.Hex("14141e");

    /// <summary>
    /// The average opaque colour along an image's border: a background that continues a full-bleed icon (Android's
    /// adaptive icon, launch screens). Transparent borders give <see cref="DefaultBackground"/>.
    /// </summary>
    public static Color EdgeColor(PixelImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        long r = 0, g = 0, b = 0, n = 0;
        void Add(Color c) { if (c.A < 128) return; r += c.R; g += c.G; b += c.B; n++; }
        for (int x = 0; x < image.Width; x++) { Add(image[x, 0]); Add(image[x, image.Height - 1]); }
        for (int y = 1; y < image.Height - 1; y++) { Add(image[0, y]); Add(image[image.Width - 1, y]); }
        return n == 0 ? DefaultBackground : new Color((byte)(r / n), (byte)(g / n), (byte)(b / n));
    }

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
        Color? background = null;
        if (values.TryGetValue("SplashColor", out var c) && c.Length > 0)
            try { background = Color.Hex(c); } catch (FormatException) { }
        return new AppIdentity(title, id, version, orientation)
        {
            Icon = Image(assembly, IconResource),
            Splash = !values.TryGetValue("Splash", out var splash) || !splash.Equals("false", StringComparison.OrdinalIgnoreCase),
            SplashBackground = background,
            SplashImageOverride = Image(assembly, SplashImageResource),
            SplashBranding = values.TryGetValue("SplashBranding", out var branding) && branding.Equals("true", StringComparison.OrdinalIgnoreCase),
        };
    }

    private static PixelImage? Image(Assembly assembly, string resource)
    {
        using var stream = assembly.GetManifestResourceStream(resource);
        if (stream is null) return null;
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return PixelImage.FromPng(buffer.ToArray());
    }
}
