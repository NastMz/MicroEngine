using System.Formats.Tar;
using System.IO.Compression;
using System.Security;
using System.Text;
using System.Text.Json;

namespace MicroEngine.Cli;

/// <summary>A target <c>microengine package</c> builds: its name on the command line, the id in file names, and the
/// .NET runtime it is published for.</summary>
public sealed record PackageTarget(string Name, string Id, string Label, string Rid, string Archive)
{
    public static readonly PackageTarget[] All =
    [
        new("windows", "windows-x64", "Windows 64-bit (x64)", "win-x64", "zip"),
        new("linux", "linux-x64", "Linux 64-bit (x86_64, glibc 2.34+)", "linux-x64", "tar.gz"),
        new("macos", "macos-arm64", "macOS on Apple silicon", "osx-arm64", "zip"),
        new("android", "android", "Android 7.0+ (arm64-v8a, x86_64)", "android-arm64;android-x64", "apk"),
        new("ios", "ios", "iPhone and iPad (device)", "ios-arm64", "ipa"),
        new("ios-simulator", "ios-simulator", "iOS Simulator on Apple silicon (not installable on an iPhone)", "iossimulator-arm64", "zip"),
        new("web", "web", "Web browsers (WebAssembly)", "browser-wasm", "zip"),
    ];

    /// <summary>The target named <paramref name="name"/> (its name or id).</summary>
    public static PackageTarget Find(string name) =>
        All.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase) || t.Id.Equals(name, StringComparison.OrdinalIgnoreCase))
        ?? throw new CliException($"unknown target '{name}'. Targets: {string.Join(", ", All.Select(t => t.Name))}.", 2);

    public bool Desktop => Name is "windows" or "linux" or "macos";
}

/// <summary>What every package build is told: where packages go, signing choices and an optional label for its
/// build-info (a content key in MicroEngine's own release pipeline).</summary>
public sealed record PackageRequest(GameProject Game, PackageTarget Target, string Out)
{
    public string Key { get; init; } = "local";
    public bool DebugKey { get; init; }
    public bool Dev { get; init; }
    public string? RaylibWasm { get; init; }
    /// <summary>File name of the package: &lt;Game&gt;-&lt;version&gt;-&lt;target&gt;.&lt;ext&gt;.</summary>
    public string FileName => $"{Game.Id}-{Game.Version}-{Target.Id}.{Target.Archive}";
}

/// <summary>Builds desktop packages (docs/guide/packaging.md): a self-contained single-file executable with its native
/// libraries, content, notices, README.txt and build-info.json in one top folder named after the game; on macOS an
/// application bundle.</summary>
public static class Packaging
{
    public static async Task<string> Desktop(PackageRequest request)
    {
        var (game, t) = (request.Game, request.Target);
        if (t.Name == "macos" && !OperatingSystem.IsMacOS())
            throw new CliException("macOS packages are built on a Mac: the application must be signed with Apple's codesign, which exists only there. Use a Mac or a macOS CI runner.");
        if (t.Name == "windows" && !OperatingSystem.IsWindows())
            throw new CliException("Windows packages are built on Windows: only there can the .NET SDK write the executable's icon and version information. Use a Windows machine or CI runner (Linux packages can be built anywhere).");
        string? icon = game.CheckIcon();
        if (icon is null) Warn($"{game.Id} declares no MicroEngineIcon, so the package uses the system's default icon.");
        string stage = Directory.CreateTempSubdirectory("microengine-package-").FullName;
        try
        {
            string publish = Path.Combine(stage, "publish");
            Console.WriteLine($"Publishing {game.Title} for {t.Label}...");
            Tool.Run(Tool.Dotnet, ["publish", game.Path, "-c", "Release", "-r", t.Rid, "--self-contained", "-p:PublishSingleFile=true",
                "-p:EnableCompressionInSingleFile=true", "-p:MicroEngineNativePackage=true", "-p:DebugType=none", "-p:DebugSymbols=false",
                "-p:InvariantGlobalization=true", "-p:PublishDocumentationFiles=false", "-p:PublishReferencesDocumentationFiles=false", "-o", publish, "-nologo"], game.Directory, "dotnet publish");
            string top = Path.Combine(stage, game.Id);
            Directory.CreateDirectory(top);
            string exe = t.Name == "windows" ? $"{game.Id}.exe" : game.Id;
            if (!File.Exists(Path.Combine(publish, exe))) throw new CliException($"dotnet publish produced no {exe} in its output; is the project's AssemblyName {game.Id}?");
            // Debug files never ship: a package must not name the build machine.
            foreach (var pdb in Directory.GetFiles(publish, "*.pdb", SearchOption.AllDirectories)) File.Delete(pdb);
            string signing = "none";
            if (t.Name == "macos")
            {
                string app = Path.Combine(top, $"{game.Title}.app");
                string contents = Path.Combine(app, "Contents");
                Directory.CreateDirectory(Path.Combine(contents, "MacOS"));
                Directory.CreateDirectory(Path.Combine(contents, "Resources"));
                foreach (var f in Directory.GetFiles(publish)) File.Copy(f, Path.Combine(contents, "MacOS", Path.GetFileName(f)));
                if (Directory.Exists(Path.Combine(publish, "Content"))) CopyTree(Path.Combine(publish, "Content"), Path.Combine(contents, "Resources", "Content"));
                if (icon is not null) Icons.WriteIcns(icon, Path.Combine(contents, "Resources", "AppIcon.icns"));
                File.WriteAllText(Path.Combine(contents, "Info.plist"), InfoPlist(game, game.RequireId("macOS"), icon is not null));
                signing = MacSigning.Sign(app, Directory.GetFiles(Path.Combine(contents, "MacOS")).Where(f => f.EndsWith(".dylib", StringComparison.Ordinal)));
            }
            else
            {
                CopyTree(publish, top);
                if (t.Name == "linux")
                {
                    if (icon is not null) File.Copy(icon, Path.Combine(top, "icon.png"));
                    string desktopId = game.ApplicationId ?? $"io.microengine.{game.Id.ToLowerInvariant()}";
                    File.WriteAllText(Path.Combine(top, $"{desktopId}.desktop"), DesktopEntry(game, icon is not null));
                }
            }
            var info = BuildInfo(request, signing);
            WriteCommon(top, game, t, info);
            Directory.CreateDirectory(request.Out);
            string file = Path.Combine(request.Out, request.FileName);
            if (t.Name == "macos" && MacSigning.NotaryProfile is { } profile)
            {
                // Apple notarizes the signed app; the ticket is then stapled to it, so Gatekeeper accepts it offline.
                string upload = Path.Combine(stage, "notarize.zip");
                Tool.Run("ditto", ["-c", "-k", "--keepParent", Path.Combine(top, $"{game.Title}.app"), upload], what: "ditto");
                Console.WriteLine("Notarizing with Apple (this can take minutes)...");
                Tool.Run("xcrun", ["notarytool", "submit", upload, "--keychain-profile", profile, "--wait"], what: "xcrun notarytool");
                Tool.Run("xcrun", ["stapler", "staple", Path.Combine(top, $"{game.Title}.app")], what: "xcrun stapler");
            }
            await Archive(stage, game.Id, file, t, exe);
            return file;
        }
        finally
        {
            Directory.Delete(stage, recursive: true);
        }
    }

    /// <summary>The files every package carries next to the game: notices, README.txt and build-info.json.</summary>
    public static void WriteCommon(string top, GameProject game, PackageTarget t, Dictionary<string, object> info)
    {
        CopyNotices(top, game, t.Rid.Split(';')[0]);
        File.WriteAllText(Path.Combine(top, "README.txt"), Readme(game, t, info));
        File.WriteAllText(Path.Combine(top, "build-info.json"), JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true }) + "\n");
    }

    /// <summary>build-info.json: what a package is, so a player or a check can tell which game, version and build it holds.</summary>
    public static Dictionary<string, object> BuildInfo(PackageRequest request, string signing)
    {
        var game = request.Game;
        var info = new Dictionary<string, object>
        {
            ["schema"] = 1, ["game"] = game.Id, ["title"] = game.Title, ["gameVersion"] = game.Version, ["engineVersion"] = game.EngineVersion,
            ["target"] = request.Target.Id, ["runtime"] = request.Target.Rid, ["key"] = request.Key, ["commit"] = Commit(game.Directory),
            ["dotnet"] = game.DotnetVersion, ["signing"] = signing, ["tool"] = Tool.Version,
        };
        if (game.ApplicationId is { } id) info["applicationId"] = id;
        return info;
    }

    /// <summary>The commit the game was built from, or "unknown" outside a Git checkout.</summary>
    public static string Commit(string dir)
    {
        try { return Tool.Run("git", ["rev-parse", "HEAD"], dir, "git").Trim(); }
        catch (CliException) { return "unknown"; }
    }

    /// <summary>The .NET runtime pack of a runtime identifier in the NuGet cache: its licence and notices travel with the package.</summary>
    public static string RuntimePack(string rid, string version)
    {
        string cache = Environment.GetEnvironmentVariable("NUGET_PACKAGES") is { Length: > 0 } p ? p
            : Tool.Run(Tool.Dotnet, ["nuget", "locals", "global-packages", "--list"], what: "dotnet nuget locals").Split(':', 2)[1].Trim();
        string dir = Path.Combine(cache, $"microsoft.netcore.app.runtime.{rid}", version);
        foreach (var f in new[] { "LICENSE.TXT", "THIRD-PARTY-NOTICES.TXT" })
            if (!File.Exists(Path.Combine(dir, f))) throw new CliException($"{Path.Combine(dir, f)} is missing; the publish step should have restored the .NET runtime pack there.");
        return dir;
    }

    private static void CopyNotices(string top, GameProject game, string rid)
    {
        var pack = RuntimePack(rid, game.DotnetVersion);
        Directory.CreateDirectory(Path.Combine(top, "licenses"));
        if (game.LicenseFile is { } licence)
        {
            if (!File.Exists(licence)) throw new CliException($"MicroEngineLicenseFile {licence} does not exist.");
            File.Copy(licence, Path.Combine(top, "LICENSE.txt"));
        }
        File.Copy(game.ThirdPartyNotices, Path.Combine(top, "THIRD-PARTY-NOTICES.txt"));
        File.Copy(Path.Combine(Tool.Home, "Notices", "microengine-LICENSE.txt"), Path.Combine(top, "licenses", "microengine-LICENSE.txt"));
        File.Copy(Path.Combine(pack, "LICENSE.TXT"), Path.Combine(top, "licenses", "dotnet-LICENSE.txt"));
        File.Copy(Path.Combine(pack, "THIRD-PARTY-NOTICES.TXT"), Path.Combine(top, "licenses", "dotnet-THIRD-PARTY-NOTICES.txt"));
    }

    private static string Readme(GameProject game, PackageTarget t, Dictionary<string, object> info)
    {
        string eol = t.Name == "windows" ? "\r\n" : "\n";
        string[] steps = t.Name switch
        {
            "windows" => ["Extract the ZIP anywhere.", $"Run {game.Id}.exe. If the publisher has not signed it, Windows SmartScreen may ask: choose More info, then Run anyway."],
            "linux" => ["Extract the archive: tar -xzf <file>.", $"Run ./{game.Id}/{game.Id} from a terminal or your file manager.",
                $"Optional, to list it in your applications menu: copy the .desktop file to ~/.local/share/applications and set its Exec= and Icon= lines to where you extracted the game."],
            "macos" => ["Extract the ZIP and move the app wherever you like.",
                (string)info["signing"] == "ad-hoc" ? "The app is not notarized by Apple, so macOS blocks the first launch: open it once, then choose Open Anyway in System Settings → Privacy & Security." : "Open it."],
            _ => [],
        };
        var lines = new List<string>
        {
            $"{game.Title} {game.Version} for {t.Label}",
            game.Description.Length > 0 ? game.Description : $"Made with MicroEngine {game.EngineVersion}.",
            "",
            "Running it",
        };
        lines.AddRange(steps.Select((s, i) => $"  {i + 1}. {s}"));
        lines.AddRange([
            "",
            "Options: --list-platforms lists the platforms the game can run on and --platform <name> chooses one (for example",
            "sdl-software, SDL's CPU renderer, for machines without a working GPU driver). F11 or Alt+Enter toggles fullscreen.",
            "",
            "Licences",
            File.Exists(game.LicenseFile ?? "") ? $"  {game.Title}: LICENSE.txt." : $"  {game.Title}: no licence file was provided with it.",
            $"  MicroEngine {game.EngineVersion} (MIT): licenses/microengine-LICENSE.txt; third-party components: THIRD-PARTY-NOTICES.txt;",
            "  the .NET runtime: licenses/dotnet-LICENSE.txt and licenses/dotnet-THIRD-PARTY-NOTICES.txt.",
            "",
            $"Build: {info["target"]}, commit {info["commit"]}, package key {info["key"]}, .NET {info["dotnet"]}, made with MicroEngine (microengine {Tool.Version}).",
            "",
        ]);
        return string.Join(eol, lines);
    }

    /// <summary>Info.plist of a macOS application bundle, from the game's identity.</summary>
    public static string InfoPlist(GameProject game, string id, bool icon)
    {
        var entries = new List<(string, string)>
        {
            ("CFBundleDevelopmentRegion", "en"), ("CFBundleExecutable", game.Id), ("CFBundleIdentifier", id), ("CFBundleInfoDictionaryVersion", "6.0"),
            ("CFBundleName", game.Title), ("CFBundleDisplayName", game.Title), ("CFBundlePackageType", "APPL"),
            ("CFBundleShortVersionString", ShortVersion(game.Version)), ("CFBundleVersion", ShortVersion(game.Version)), ("LSMinimumSystemVersion", "14.0"),
            ("LSApplicationCategoryType", "public.app-category.games"),
        };
        if (icon) entries.Add(("CFBundleIconFile", "AppIcon"));
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n<plist version=\"1.0\">\n<dict>\n");
        foreach (var (k, v) in entries) sb.Append($"  <key>{k}</key>\n  <string>{SecurityElement.Escape(v)}</string>\n");
        sb.Append("  <key>NSHighResolutionCapable</key>\n  <true/>\n</dict>\n</plist>\n");
        return sb.ToString();
    }

    /// <summary>The numeric part of a version (Apple and Android accept only numbers and dots).</summary>
    public static string ShortVersion(string version) => version.Split('-', '+')[0];

    /// <summary>
    /// A freedesktop.org desktop entry for the game. StartupWMClass is the application id SDL gives the window (Wayland
    /// app id, X11 class), so an installed entry's name and icon belong to the running game in docks and task switchers.
    /// </summary>
    public static string DesktopEntry(GameProject game, bool icon) => string.Join('\n',
    [
        "[Desktop Entry]", "Type=Application", $"Name={game.Title}", $"Comment={game.Description}", $"Exec={game.Id}",
        .. icon ? new[] { "Icon=icon.png" } : [], .. game.ApplicationId is { } id ? new[] { $"StartupWMClass={id}" } : [],
        "Terminal=false", "Categories=Game;", "",
    ]);

    private static void CopyTree(string from, string to)
    {
        Directory.CreateDirectory(to);
        foreach (var dir in Directory.GetDirectories(from, "*", SearchOption.AllDirectories)) Directory.CreateDirectory(Path.Combine(to, Path.GetRelativePath(from, dir)));
        foreach (var file in Directory.GetFiles(from, "*", SearchOption.AllDirectories)) File.Copy(file, Path.Combine(to, Path.GetRelativePath(from, file)), overwrite: true);
    }

    /// <summary>Files under a folder, relative, with '/' separators, in a fixed order.</summary>
    public static List<string> Walk(string dir) => Directory.GetFiles(dir, "*", SearchOption.AllDirectories)
        .Select(f => Path.GetRelativePath(dir, f).Replace('\\', '/')).OrderBy(f => f, StringComparer.Ordinal).ToList();

    /// <summary>Archives <c>stage/top</c> into <paramref name="file"/>: a fixed file order, no timestamps; on Linux the
    /// executable and libraries 0755, everything else 0644; macOS archives keep the bundle's signature (ditto).</summary>
    public static async Task Archive(string stage, string top, string file, PackageTarget t, string exe)
    {
        File.Delete(file);
        var stamp = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        if (t.Archive == "tar.gz")
        {
            await using var output = File.Create(file);
            await using var gzip = new GZipStream(output, CompressionLevel.SmallestSize);
            await using var tar = new TarWriter(gzip, TarEntryFormat.Pax, leaveOpen: false);
            foreach (var rel in Walk(Path.Combine(stage, top)))
            {
                bool executable = rel == exe || rel.EndsWith(".so", StringComparison.Ordinal);
                var entry = new PaxTarEntry(TarEntryType.RegularFile, $"{top}/{rel}")
                {
                    Mode = executable ? (UnixFileMode)0b111_101_101 : (UnixFileMode)0b110_100_100,
                    ModificationTime = stamp, Uid = 0, Gid = 0,
                    DataStream = File.OpenRead(Path.Combine(stage, top, rel)),
                };
                await tar.WriteEntryAsync(entry);
                await entry.DataStream.DisposeAsync();
            }
        }
        else if (OperatingSystem.IsMacOS())
        {
            Tool.Run("ditto", ["-c", "-k", "--sequesterRsrc", "--keepParent", Path.Combine(stage, top), file], what: "ditto");
        }
        else
        {
            using var zip = ZipFile.Open(file, ZipArchiveMode.Create);
            foreach (var rel in Walk(Path.Combine(stage, top)))
            {
                var entry = zip.CreateEntry($"{top}/{rel}", CompressionLevel.SmallestSize);
                entry.LastWriteTime = stamp;
                using var to = entry.Open();
                using var from = File.OpenRead(Path.Combine(stage, top, rel));
                from.CopyTo(to);
            }
        }
    }

    public static void Warn(string message) => Console.Error.WriteLine($"warning: {message}");
}

/// <summary>macOS signing: a Developer ID identity when MICROENGINE_MACOS_SIGN_IDENTITY names one (hardened runtime,
/// secure timestamp), otherwise an ad-hoc signature (Apple silicon runs only signed code). Notarization runs when
/// MICROENGINE_MACOS_NOTARY_PROFILE names a notarytool keychain profile.</summary>
public static class MacSigning
{
    public static string? Identity => Environment.GetEnvironmentVariable("MICROENGINE_MACOS_SIGN_IDENTITY") is { Length: > 0 } v ? v : null;
    public static string? NotaryProfile => Environment.GetEnvironmentVariable("MICROENGINE_MACOS_NOTARY_PROFILE") is { Length: > 0 } v ? v : null;

    /// <summary>Signs the libraries, then the bundle; returns the signing kind for build-info.</summary>
    public static string Sign(string app, IEnumerable<string> libraries)
    {
        if (NotaryProfile is not null && Identity is null)
            throw new CliException("MICROENGINE_MACOS_NOTARY_PROFILE is set, but Apple notarizes only apps signed with a Developer ID: set MICROENGINE_MACOS_SIGN_IDENTITY too (docs/guide/signing.md).");
        string[] how = Identity is { } id ? ["--sign", id, "--options", "runtime", "--timestamp"] : ["--sign", "-", "--timestamp=none"];
        foreach (var lib in libraries) Tool.Run("codesign", ["--force", .. how, lib], what: "codesign");
        if (Identity is null)
        {
            Tool.Run("codesign", ["--force", .. how, app], what: "codesign");
            return "ad-hoc";
        }
        // The hardened runtime that notarization requires blocks .NET's JIT unless the app is entitled to it.
        string entitlements = Path.Combine(Path.GetTempPath(), $"microengine-{Guid.NewGuid():N}.entitlements");
        File.WriteAllText(entitlements, """
            <?xml version="1.0" encoding="UTF-8"?>
            <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
            <plist version="1.0"><dict>
              <key>com.apple.security.cs.allow-jit</key><true/>
              <key>com.apple.security.cs.allow-unsigned-executable-memory</key><true/>
            </dict></plist>
            """);
        try { Tool.Run("codesign", ["--force", .. how, "--entitlements", entitlements, app], what: "codesign"); }
        finally { File.Delete(entitlements); }
        return NotaryProfile is null ? "developer-id" : "notarized";
    }
}
