using System.Text.Json;
using System.Text.RegularExpressions;

namespace MicroEngine.Cli;

/// <summary>
/// A game's project file and what it declares, read from MSBuild itself (<c>dotnet msbuild -getProperty</c>), so the
/// tool sees exactly what a build sees: the identity (docs/guide/app-identity.md), the content folder and the engine
/// version. Nothing about a game is configured anywhere else.
/// </summary>
public sealed partial class GameProject
{
    private static readonly string[] Names =
    [
        "AssemblyName", "ApplicationTitle", "ApplicationId", "Version", "Description", "MicroEngineIcon", "MicroEngineOrientation",
        "MicroEngineGameFactory", "MicroEngineContentDir", "MicroEngineScriptsDir", "MicroEngineLicenseFile", "MicroEngineThirdPartyNotices",
        "MicroEngineVersion", "MicroEngineAppTargets", "MicroEngineAndroidMinApi", "OutputType", "TargetFramework", "TargetDir",
        "BundledNETCoreAppPackageVersion", "MicroEngineWebRaylibFromPackage", "NetCoreRoot",
        "MicroEngineSplash", "MicroEngineSplashColor", "MicroEngineSplashImage", "MicroEngineSplashBranding", "MicroEngineIconBackground",
    ];

    private readonly Dictionary<string, string> _values;

    private GameProject(string path, Dictionary<string, string> values)
    {
        Path = path;
        _values = values;
    }

    /// <summary>Full path of the .csproj.</summary>
    public string Path { get; }
    public string Directory => System.IO.Path.GetDirectoryName(Path)!;
    public string Id => this["AssemblyName"];
    public string Title => this["ApplicationTitle"].Length > 0 ? this["ApplicationTitle"] : Id;
    public string? ApplicationId => Optional("ApplicationId");
    public string Version => this["Version"].Length > 0 ? this["Version"] : "1.0.0";
    public string Description => this["Description"];
    public string Orientation => this["MicroEngineOrientation"].Length > 0 ? this["MicroEngineOrientation"] : "landscape";
    public string GameFactory => this["MicroEngineGameFactory"];
    public string EngineVersion => this["MicroEngineVersion"].Length > 0 ? this["MicroEngineVersion"] : "unknown";
    public string DotnetVersion => this["BundledNETCoreAppPackageVersion"];
    public string NetCoreRoot => this["NetCoreRoot"];
    public string AndroidMinApi => this["MicroEngineAndroidMinApi"].Length > 0 ? this["MicroEngineAndroidMinApi"] : "24";
    public string? WebRaylibFromPackage => Optional("MicroEngineWebRaylibFromPackage");
    /// <summary>Whether the game opens on its start screen (MicroEngineSplash, true unless "false").</summary>
    public bool Splash => !this["MicroEngineSplash"].Equals("false", StringComparison.OrdinalIgnoreCase);
    /// <summary>Whether the start screen says "Made with MicroEngine" (MicroEngineSplashBranding).</summary>
    public bool SplashBranding => this["MicroEngineSplashBranding"].Equals("true", StringComparison.OrdinalIgnoreCase);
    /// <summary>The start screen's background (MicroEngineSplashColor, #RRGGBB), or null to take it from its image.</summary>
    public string? SplashColor => Optional("MicroEngineSplashColor");
    /// <summary>The start screen's own image (MicroEngineSplashImage), or null for the icon.</summary>
    public string? SplashImage => Optional("MicroEngineSplashImage") is { } image ? Full(image) : null;
    /// <summary>The background of Android's adaptive icon (MicroEngineIconBackground, #RRGGBB), or null for the icon's edge colour.</summary>
    public string? IconBackground => Optional("MicroEngineIconBackground");
    /// <summary>The build output folder of the last <c>dotnet build</c> in the configuration the project was read with.</summary>
    public string TargetDir => this["TargetDir"];

    /// <summary>The icon PNG, or null when the project declares none.</summary>
    public string? Icon => Optional("MicroEngineIcon") is { } icon ? Full(icon) : null;
    /// <summary>The Content folder when it exists.</summary>
    public string? ContentDir => Optional("MicroEngineContentDir") is { } dir && System.IO.Directory.Exists(Full(dir)) ? Full(dir).TrimEnd('/', '\\') + System.IO.Path.DirectorySeparatorChar : null;
    /// <summary>Input scripts packaged for tests on phones and in development web builds (optional).</summary>
    public string? ScriptsDir => Optional("MicroEngineScriptsDir") is { } dir && System.IO.Directory.Exists(Full(dir)) ? Full(dir).TrimEnd('/', '\\') + System.IO.Path.DirectorySeparatorChar : null;
    /// <summary>The game's own licence, shipped as LICENSE.txt: MicroEngineLicenseFile, else LICENSE[.txt|.md] next to the project.</summary>
    public string? LicenseFile => Optional("MicroEngineLicenseFile") is { } file ? Full(file)
        : new[] { "LICENSE", "LICENSE.txt", "LICENSE.md" }.Select(f => System.IO.Path.Combine(Directory, f)).FirstOrDefault(File.Exists);
    /// <summary>Third-party notices shipped as THIRD-PARTY-NOTICES.txt: the game's own list, else MicroEngine's.</summary>
    public string ThirdPartyNotices => Optional("MicroEngineThirdPartyNotices") is { } file ? Full(file) : System.IO.Path.Combine(Tool.Home, "Notices", "THIRD-PARTY-NOTICES.txt");

    public string this[string name] => _values.TryGetValue(name, out var v) ? v : "";

    private string? Optional(string name) => this[name] is { Length: > 0 } v ? v : null;
    private string Full(string relative) => System.IO.Path.GetFullPath(System.IO.Path.Combine(Directory, relative));

    /// <summary>
    /// The project to work on: <paramref name="given"/> (a .csproj or a folder holding exactly one), else the only
    /// .csproj in the current folder.
    /// </summary>
    public static string Locate(string? given)
    {
        string where = System.IO.Path.GetFullPath(given ?? ".");
        if (File.Exists(where))
        {
            if (!where.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)) throw new CliException($"{where} is not a C# project file (.csproj).", 2);
            return where;
        }
        if (!System.IO.Directory.Exists(where)) throw new CliException($"{where} does not exist.", 2);
        var projects = System.IO.Directory.GetFiles(where, "*.csproj");
        return projects.Length switch
        {
            1 => projects[0],
            0 => throw new CliException($"no game project (.csproj) in {where}. Run this inside a game's folder, pass --project <path>, or create one with 'microengine new <Name>'.", 2),
            _ => throw new CliException($"{where} holds several projects ({string.Join(", ", projects.Select(System.IO.Path.GetFileName))}); choose one with --project.", 2),
        };
    }

    /// <summary>Reads the project's properties as MSBuild evaluates them in <paramref name="configuration"/>.</summary>
    public static GameProject Load(string path, string configuration = "Release")
    {
        var project = Read(path, configuration);
        // Package references import the engine's build targets only after a restore.
        if (project["MicroEngineAppTargets"] != "true")
        {
            Tool.Run(Tool.Dotnet, ["restore", path], System.IO.Path.GetDirectoryName(path), "dotnet restore");
            project = Read(path, configuration);
        }
        if (project["MicroEngineAppTargets"] != "true")
            throw new CliException($"{System.IO.Path.GetFileName(path)} does not reference MicroEngine. Add a package (dotnet add package MicroEngine.Desktop --prerelease) or start from 'microengine new'.");
        if (project["OutputType"] is not ("Exe" or "WinExe"))
            throw new CliException($"{System.IO.Path.GetFileName(path)} is a library (OutputType {project["OutputType"]}); a game project is an executable (<OutputType>Exe</OutputType>).");
        return project;
    }

    private static GameProject Read(string path, string configuration)
    {
        string json = Tool.Run(Tool.Dotnet, ["msbuild", path, $"-p:Configuration={configuration}", .. Names.Select(n => $"-getProperty:{n}")],
            System.IO.Path.GetDirectoryName(path), "reading the project with MSBuild");
        Dictionary<string, string> values;
        try
        {
            using var doc = JsonDocument.Parse(json);
            values = doc.RootElement.GetProperty("Properties").EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString() ?? "");
        }
        catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new CliException($"could not read the properties of {path}: MSBuild printed something unexpected ({e.Message}).");
        }
        return new GameProject(path, values);
    }

    [GeneratedRegex(@"^[A-Za-z][A-Za-z0-9_]*(\.[A-Za-z][A-Za-z0-9_]*)+$")]
    private static partial Regex IdPattern();

    /// <summary>Checks an application id the way Android and Apple do.</summary>
    public static bool IsValidId(string id) => IdPattern().IsMatch(id);

    /// <summary>The application id, which <paramref name="target"/> needs; fails with what to add when it is missing or invalid.</summary>
    public string RequireId(string target)
    {
        var id = ApplicationId ?? throw new CliException(
            $"{target} packages need an application id. Add one to {System.IO.Path.GetFileName(Path)}, for example:\n  <ApplicationId>com.example.{Id.ToLowerInvariant()}</ApplicationId>\nUse a domain you control: stores and phones identify the game by it, and it must never change after the first release.");
        if (!IsValidId(id)) throw new CliException($"ApplicationId '{id}' is not valid: use two or more dot-separated parts, each starting with a letter and made of letters, digits and underscores (for example com.example.mygame).");
        return id;
    }

    /// <summary>Checks the declared icon; returns its path, or null when there is none.</summary>
    public string? CheckIcon()
    {
        if (Icon is not { } icon) return null;
        if (!File.Exists(icon)) throw new CliException($"MicroEngineIcon '{this["MicroEngineIcon"]}' does not exist (looked for {icon}).");
        Icons.Check(icon);
        return icon;
    }

    /// <summary>The factory expression, which hosts that create the game themselves need.</summary>
    public string RequireFactory(string target)
    {
        if (GameFactory.Length == 0)
            throw new CliException($"{target} packages create the game themselves: add <MicroEngineGameFactory>new MyGame.MyGame()</MicroEngineGameFactory> to {System.IO.Path.GetFileName(Path)}.");
        return GameFactory;
    }
}
