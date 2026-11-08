using System.Security;
using System.Text;
using System.Text.Json;
using MicroEngine.Core;
using MicroEngine.Graphics;

namespace MicroEngine.Cli;

/// <summary>
/// Packages that need a host program around the game: the browser page (SDL 3 in WebAssembly), the Android activity
/// and the iOS app (SDL 3). The tool copies a host template (Hosts/&lt;kind&gt;) into the game's
/// <c>obj/microengine/&lt;kind&gt;</c> folder and writes what is particular to the game: Game.props (identity and paths),
/// GameEntry.cs (a call to the game's <c>Configure</c> entry point, <see cref="EntryPoint"/>), the icon and names. The host references the game project,
/// so the engine comes from wherever the game gets it (packages, or project references in MicroEngine's repository).
/// </summary>
public static class Hosts
{
    public static async Task<string> Web(PackageRequest request)
    {
        var game = request.Game;
        RequireBackend(game, "MicroEngine.Sdl", "Web packages run on SDL 3: reference MicroEngine.Sdl (or MicroEngine.Desktop, which includes it).");
        string sdl = request.SdlWasm ?? game.WebSdlFromPackage ?? Path.Combine(WebTools, "sdl-web", "browser-wasm", "SDL3.a");
        if (!File.Exists(sdl))
            throw new CliException($"SDL for the web is missing ({sdl}). It comes with the MicroEngine.Sdl package; with project references to a MicroEngine checkout run its web/setup.sh, or pass --sdl-wasm <path>.");
        string? icon = game.CheckIcon();
        string host = Stage("web", game, new()
        {
            ["WebDevHooks"] = request.Dev ? "true" : "false",
            ["SdlWasmLib"] = sdl,
        }, $"    public const bool DevHooks = {(request.Dev ? "true" : "false")};");
        string root = Path.Combine(host, "wwwroot");
        File.WriteAllText(Path.Combine(root, "game.json"), JsonSerializer.Serialize(new { id = game.Id, title = game.Title, dev = request.Dev, touch = true }) + "\n");
        var content = game.ContentDir is { } dir ? Packaging.Walk(dir).Where(f => !f.EndsWith(".cs", StringComparison.Ordinal)).ToList() : [];
        File.WriteAllText(Path.Combine(root, "content.txt"), string.Concat(content.Select(f => f + Environment.NewLine)));
        File.Copy(icon ?? Icons.Default, Path.Combine(root, "icon.png"), overwrite: true);
        // The start screen is in the page itself, so the game's colour and image show before any script loads.
        var (splash, background) = SplashOf(game, PixelImage.Load(icon ?? Icons.Default));
        string color = Icons.Hex(background);
        File.Delete(Path.Combine(root, "splash.png"));
        if (game.Splash)
        {
            var (sw, sh) = Fit(splash, 256);
            Icons.WritePng(splash.Resized(sw, sh), Path.Combine(root, "splash.png"));
        }
        string index = Path.Combine(root, "index.html");
        string page = File.ReadAllText(index)
            .Replace("<title>MicroEngine</title>", $"<title>{SecurityElement.Escape(game.Title)}</title>")
            .Replace("<html lang=\"en\">", $"<html lang=\"en\" style=\"--splash: {color}\">")
            .Replace("<meta name=\"theme-color\" content=\"#000000\">", $"<meta name=\"theme-color\" content=\"{color}\">")
            .Replace("<img id=\"splash\" src=\"splash.png\" alt=\"\" hidden>", game.Splash ? "<img id=\"splash\" src=\"splash.png\" alt=\"\">" : "")
            .Replace("<p id=\"brand\" hidden>", game.Splash && game.SplashBranding ? "<p id=\"brand\">" : "<p id=\"brand\" hidden>");
        File.WriteAllText(index, page);
        // Installing the page on a phone's home screen keeps the game's name, icon, colour and orientation.
        File.WriteAllText(Path.Combine(root, "manifest.webmanifest"), JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["name"] = game.Title, ["short_name"] = game.Title, ["id"] = "./", ["start_url"] = "./", ["display"] = "fullscreen",
            ["orientation"] = game.Orientation == "any" ? "any" : game.Orientation, ["background_color"] = color, ["theme_color"] = color,
            ["icons"] = new[] { new { src = "icon.png", type = "image/png", sizes = $"{Icons.Check(icon ?? Icons.Default)}x{Icons.Check(icon ?? Icons.Default)}", purpose = "any" } },
        }, new JsonSerializerOptions { WriteIndented = true }) + "\n");
        string publish = host + "-publish";
        if (Directory.Exists(publish)) Directory.Delete(publish, recursive: true);
        Console.WriteLine($"Building {game.Title} for browsers (WebAssembly; the first build takes a few minutes)...");
        Tool.Run(Tool.Dotnet, ["publish", Path.Combine(host, "Host.csproj"), "-c", "Release", "-p:MicroEngineWebPackage=true", "-o", publish, "-nologo"], host, "the browser build");
        string stage = Directory.CreateTempSubdirectory("microengine-package-").FullName;
        try
        {
            string top = Path.Combine(stage, game.Id);
            CopyTree(Path.Combine(publish, "wwwroot"), top);
            var info = Packaging.BuildInfo(request, "none");
            WebNotices(top, game);
            File.WriteAllText(Path.Combine(top, "build-info.json"), JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true }) + "\n");
            Directory.CreateDirectory(request.Out);
            // The folder to upload to any static web server, and the same files as one archive.
            string folder = Path.Combine(request.Out, Path.GetFileNameWithoutExtension(request.FileName));
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
            CopyTree(top, folder);
            string file = Path.Combine(request.Out, request.FileName);
            await Packaging.Archive(stage, game.Id, file, request.Target, "");
            Console.WriteLine($"Serve the folder with any static web server: {folder}");
            return file;
        }
        finally { Directory.Delete(stage, recursive: true); }
    }

    /// <summary>The private web toolchain folder of web/setup.sh (MICROENGINE_WEB_TOOLS, default ~/.microengine/web-tools).</summary>
    private static string WebTools => Environment.GetEnvironmentVariable("MICROENGINE_WEB_TOOLS") is { Length: > 0 } w
        ? w : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".microengine", "web-tools");

    private static void WebNotices(string top, GameProject game)
    {
        string dotnetRoot = game.NetCoreRoot;
        string runtime = Path.Combine(dotnetRoot, "packs", "Microsoft.NETCore.App.Runtime.Mono.browser-wasm", game.DotnetVersion);
        string? emscripten = Directory.Exists(Path.Combine(dotnetRoot, "packs"))
            ? Directory.GetDirectories(Path.Combine(dotnetRoot, "packs"), "Microsoft.NET.Runtime.Emscripten.*.Sdk.*")
                .SelectMany(d => Directory.GetFiles(d, "LICENSE", SearchOption.AllDirectories)).FirstOrDefault(f => f.Replace('\\', '/').EndsWith("tools/emscripten/LICENSE", StringComparison.Ordinal))
            : null;
        Directory.CreateDirectory(Path.Combine(top, "licenses"));
        if (game.LicenseFile is { } licence) File.Copy(licence, Path.Combine(top, "LICENSE.txt"), overwrite: true);
        File.Copy(game.ThirdPartyNotices, Path.Combine(top, "THIRD-PARTY-NOTICES.txt"), overwrite: true);
        File.Copy(Path.Combine(Tool.Home, "Notices", "microengine-LICENSE.txt"), Path.Combine(top, "licenses", "microengine-LICENSE.txt"), overwrite: true);
        // SDL, and raylib's licence wherever its binding is in the package (a game that references MicroEngine.Desktop
        // carries Raylib-cs in its page, trimmed but present).
        var engines = new List<string> { "SDL-LICENSE.txt" };
        if (Directory.GetFiles(Path.Combine(top, "_framework"), "Raylib-cs.*").Length > 0) engines.Add("raylib-LICENSE.txt");
        // HarfBuzz is linked into the page's WebAssembly when the game shapes text (its binding is then in the package).
        if (Directory.GetFiles(Path.Combine(top, "_framework"), "HarfBuzzSharp.*").Length > 0) engines.Add(EngineNotices.HarfBuzz);
        foreach (string engine in engines) File.Copy(Path.Combine(Tool.Home, "Notices", engine), Path.Combine(top, "licenses", engine), overwrite: true);
        foreach (var (from, to) in new[] { (Path.Combine(runtime, "LICENSE.TXT"), "dotnet-LICENSE.txt"), (Path.Combine(runtime, "THIRD-PARTY-NOTICES.TXT"), "dotnet-THIRD-PARTY-NOTICES.txt"), (emscripten, "emscripten-LICENSE.txt") })
        {
            if (from is null || !File.Exists(from)) throw new CliException($"a licence file of the browser toolchain is missing ({from ?? "emscripten's LICENSE"}); is the wasm-tools workload installed in {dotnetRoot}?");
            File.Copy(from, Path.Combine(top, "licenses", to), overwrite: true);
        }
    }

    public static Task<string> Android(PackageRequest request, int versionCode)
    {
        var game = request.Game;
        string id = game.RequireId("Android");
        RequireBackend(game, "MicroEngine.Sdl", "Android packages run on SDL 3: reference MicroEngine.Sdl (or MicroEngine.Desktop, which includes it).");
        var signing = AndroidSigning.Resolve(request.DebugKey);
        string icon = IconOrDefault(game, "Android");
        string host = Stage("android", game, new()
        {
            ["GameAndroidMinApi"] = game.AndroidMinApi,
            ["GameVersionCode"] = versionCode.ToString(System.Globalization.CultureInfo.InvariantCulture),
        }, $"    public const global::Android.Content.PM.ScreenOrientation ScreenOrientation = global::Android.Content.PM.ScreenOrientation.{game.Orientation switch { "portrait" => "SensorPortrait", "any" => "FullSensor", _ => "SensorLandscape" }};");
        string res = Path.Combine(host, "Resources");
        // Resources of an earlier package of this game (a removed splash image, an older icon layout) must not linger.
        if (Directory.Exists(res)) Directory.Delete(res, recursive: true);
        Directory.CreateDirectory(Path.Combine(res, "values"));
        File.WriteAllText(Path.Combine(res, "values", "strings.xml"),
            $"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<resources>\n  <string name=\"app_name\">{SecurityElement.Escape(game.Title).Replace("'", "\\'")}</string>\n</resources>\n");
        var iconImage = PixelImage.Load(icon);
        Icons.WriteAndroid(icon, res, game.IconBackground is { } iconBackground ? Color.Hex(iconBackground) : AppIdentity.EdgeColor(iconImage));
        AndroidSplash(game, res, iconImage);
        string publish = host + "-publish";
        if (Directory.Exists(publish)) Directory.Delete(publish, recursive: true);
        var args = new List<string> { "publish", Path.Combine(host, "Host.csproj"), "-c", "Release", "-p:MicroEngineNativePackage=true", "-o", publish, "-nologo" };
        if (Environment.GetEnvironmentVariable("ANDROID_HOME") is { Length: > 0 } sdk) args.Add($"-p:AndroidSdkDirectory={sdk}");
        if (Environment.GetEnvironmentVariable("JAVA_HOME") is { Length: > 0 } java) args.Add($"-p:JavaSdkDirectory={java}");
        args.AddRange(signing.Arguments);
        Console.WriteLine($"Building {game.Title} for Android ({signing.Kind} signing)...");
        Tool.Run(Tool.Dotnet, args, host, "the Android build", secrets: signing.Secrets);
        string apk = Directory.GetFiles(publish, "*-Signed.apk").FirstOrDefault() ?? throw new CliException("the Android build produced no signed APK.");
        Directory.CreateDirectory(request.Out);
        string file = Path.Combine(request.Out, request.FileName);
        File.Copy(apk, file, overwrite: true);
        var info = Packaging.BuildInfo(request, signing.Kind);
        info["versionCode"] = versionCode.ToString(System.Globalization.CultureInfo.InvariantCulture);
        File.WriteAllText(Path.ChangeExtension(file, ".build-info.json"), JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true }) + "\n");
        if (signing.Kind == "debug") Packaging.Warn("this APK is signed with this machine's debug key: install it for testing, never distribute it (docs/guide/signing.md).");
        return Task.FromResult(file);
    }

    public static async Task<string> Ios(PackageRequest request, int versionCode)
    {
        var game = request.Game;
        bool device = request.Target.Name == "ios";
        if (!OperatingSystem.IsMacOS()) throw new CliException("iOS apps are built on a Mac with Xcode (Apple's tools exist only there). Use a Mac or a macOS CI runner.");
        string id = game.RequireId("iOS");
        RequireBackend(game, "MicroEngine.Sdl", "iOS packages run on SDL 3: reference MicroEngine.Sdl (or MicroEngine.Desktop, which includes it).");
        string? key = Environment.GetEnvironmentVariable("MICROENGINE_IOS_CODESIGN_KEY");
        string? profile = Environment.GetEnvironmentVariable("MICROENGINE_IOS_PROVISIONING_PROFILE");
        if (device && (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(profile)))
            throw new CliException("an iPhone or iPad installs only apps signed for it. Set MICROENGINE_IOS_CODESIGN_KEY (your Apple Development or Distribution certificate's name) and MICROENGINE_IOS_PROVISIONING_PROFILE (a provisioning profile for "
                + $"{id}); both come from an Apple Developer account (docs/guide/signing.md). To try the game without one, package ios-simulator, which runs only in Xcode's Simulator.");
        string icon = IconOrDefault(game, "iOS");
        string host = Stage("ios", game, new() { ["GameVersionCode"] = versionCode.ToString(System.Globalization.CultureInfo.InvariantCulture) }, "");
        string set = Path.Combine(host, "Assets.xcassets", "AppIcon.appiconset");
        Directory.CreateDirectory(set);
        // iOS takes one 1024-pixel image and makes every other size from it; smaller pixel art is enlarged without blur.
        var image = PixelImage.Load(icon);
        int factor = 1024 / image.Width;
        File.WriteAllBytes(Path.Combine(set, "icon.png"), PngEncoder.Encode(image.ToRgba(), image.Width, image.Height, factor));
        File.WriteAllText(Path.Combine(set, "Contents.json"), """{ "images": [ { "filename": "icon.png", "idiom": "universal", "platform": "ios", "size": "1024x1024" } ], "info": { "author": "microengine", "version": 1 } }""");
        File.WriteAllText(Path.Combine(host, "Assets.xcassets", "Contents.json"), """{ "info": { "author": "microengine", "version": 1 } }""");
        IosLaunchScreen(game, host, image);
        string plist = Path.Combine(host, "Info.plist");
        string orientations = game.Orientation switch
        {
            "portrait" => "    <string>UIInterfaceOrientationPortrait</string>\n",
            "any" => "    <string>UIInterfaceOrientationLandscapeLeft</string>\n    <string>UIInterfaceOrientationLandscapeRight</string>\n    <string>UIInterfaceOrientationPortrait</string>\n",
            _ => "    <string>UIInterfaceOrientationLandscapeLeft</string>\n    <string>UIInterfaceOrientationLandscapeRight</string>\n",
        };
        File.WriteAllText(plist, File.ReadAllText(plist).ReplaceLineEndings("\n").Replace("    <string>UIInterfaceOrientationLandscapeLeft</string>\n    <string>UIInterfaceOrientationLandscapeRight</string>\n", orientations));
        string publish = host + "-publish";
        if (Directory.Exists(publish)) Directory.Delete(publish, recursive: true);
        var args = new List<string> { device ? "publish" : "build", Path.Combine(host, "Host.csproj"), "-c", "Release", "-r", request.Target.Rid, "-p:MicroEngineNativePackage=true", "-nologo" };
        if (device) args.AddRange(["-o", publish, $"-p:CodesignKey={key}", $"-p:CodesignProvision={profile}"]);
        Console.WriteLine($"Building {game.Title} for {request.Target.Label}...");
        Tool.Run(Tool.Dotnet, args, host, "the iOS build");
        Directory.CreateDirectory(request.Out);
        string file = Path.Combine(request.Out, request.FileName);
        var info = Packaging.BuildInfo(request, device ? "apple" : "simulator");
        info["versionCode"] = versionCode.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (device)
        {
            string ipa = Directory.GetFiles(publish, "*.ipa").FirstOrDefault() ?? throw new CliException("the iOS build produced no .ipa.");
            File.Copy(ipa, file, overwrite: true);
        }
        else
        {
            string app = Directory.GetDirectories(Path.Combine(host, "bin", "Release"), "*.app", SearchOption.AllDirectories).FirstOrDefault()
                ?? throw new CliException("the iOS build produced no .app.");
            string stage = Directory.CreateTempSubdirectory("microengine-package-").FullName;
            try
            {
                string top = Path.Combine(stage, game.Id);
                Tool.Run("ditto", [app, Path.Combine(top, Path.GetFileName(app))], what: "ditto");
                File.WriteAllText(Path.Combine(top, "README.txt"), $"{game.Title} {game.Version} for the iOS Simulator.\nThis build runs only in Xcode's Simulator on a Mac (xcrun simctl install booted \"{Path.GetFileName(app)}\"); it cannot be installed on an iPhone or iPad.\n");
                File.WriteAllText(Path.Combine(top, "build-info.json"), JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true }) + "\n");
                await Packaging.Archive(stage, game.Id, file, request.Target, "");
            }
            finally { Directory.Delete(stage, recursive: true); }
        }
        File.WriteAllText(Path.ChangeExtension(file, ".build-info.json"), JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true }) + "\n");
        return file;
    }

    /// <summary>The start screen's image (the game's own, else the icon) and background colour (#RRGGBB).</summary>
    private static (PixelImage Image, Color Background) SplashOf(GameProject game, PixelImage icon)
    {
        if (!game.Splash) return (icon, Color.Black);
        PixelImage image = icon;
        if (game.SplashImage is { } file)
        {
            if (!File.Exists(file)) throw new CliException($"MicroEngineSplashImage '{file}' does not exist.");
            image = PixelImage.Load(file);
        }
        return (image, game.SplashColor is { } color ? Color.Hex(color) : AppIdentity.EdgeColor(image));
    }

    /// <summary>The largest size of <paramref name="image"/> that fits a <paramref name="box"/>-pixel square, keeping its shape.</summary>
    private static (int Width, int Height) Fit(PixelImage image, int box) => image.FitInside(box, box);

    /// <summary>
    /// The activity's theme: the start screen's colour and image while Android starts the game (Android 12+ draws its
    /// splash screen from windowSplashScreen*, older versions show the window background), so the screen the engine
    /// draws while the game loads continues it without a black frame. Without a start screen the window stays black.
    /// </summary>
    private static void AndroidSplash(GameProject game, string res, PixelImage icon)
    {
        var (image, background) = SplashOf(game, icon);
        string color = Icons.Hex(background);
        Directory.CreateDirectory(Path.Combine(res, "drawable"));
        Directory.CreateDirectory(Path.Combine(res, "values-v31"));
        string imageItem = "";
        if (game.Splash)
        {
            // 96 dp on older Android (xxhdpi: 3 pixels per dp).
            var (w, h) = Fit(image, 288);
            Icons.WritePng(image.Resized(w, h), Path.Combine(res, "drawable-xxhdpi", "splash_image.png"));
            imageItem = "\n  <item><bitmap android:gravity=\"center\" android:src=\"@drawable/splash_image\" /></item>";
        }
        File.WriteAllText(Path.Combine(res, "drawable", "splash.xml"),
            $"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<layer-list xmlns:android=\"http://schemas.android.com/apk/res/android\">\n  <item android:drawable=\"@color/splash_background\" />{imageItem}\n</layer-list>\n");
        const string parent = "@android:style/Theme.NoTitleBar.Fullscreen";
        File.WriteAllText(Path.Combine(res, "values", "splash.xml"),
            $"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<resources>\n  <color name=\"splash_background\">{color}</color>\n"
            + $"  <style name=\"GameTheme\" parent=\"{parent}\">\n    <item name=\"android:windowBackground\">@drawable/splash</item>\n  </style>\n</resources>\n");
        // Android 12+: the system splash shows an icon (the adaptive icon unless the game has its own image) in a circle
        // of 160 dp inside a 240 dp square.
        string icon31 = "";
        if (game.Splash && game.SplashImage is not null)
        {
            var (w, h) = Fit(image, 640);
            var square = new PixelImage(960, 960).Blit(image.Resized(w, h), (960 - w) / 2, (960 - h) / 2);
            Icons.WritePng(square, Path.Combine(res, "drawable-xxxhdpi", "splash_icon.png"));
            icon31 = "\n    <item name=\"android:windowSplashScreenAnimatedIcon\">@drawable/splash_icon</item>";
        }
        File.WriteAllText(Path.Combine(res, "values-v31", "splash.xml"),
            $"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<resources>\n  <style name=\"GameTheme\" parent=\"{parent}\">\n"
            + "    <item name=\"android:windowBackground\">@drawable/splash</item>\n"
            + $"    <item name=\"android:windowSplashScreenBackground\">@color/splash_background</item>{icon31}\n  </style>\n</resources>\n");
    }

    /// <summary>
    /// iOS's launch screen, declared in Info.plist (<c>UILaunchScreen</c>, iOS 14+): a named colour and image of the
    /// asset catalog, shown by the system until the game draws; the engine's own start screen continues it.
    /// </summary>
    private static void IosLaunchScreen(GameProject game, string host, PixelImage icon)
    {
        var (image, background) = SplashOf(game, icon);
        string assets = Path.Combine(host, "Assets.xcassets");
        string colors = Path.Combine(assets, "LaunchBackground.colorset");
        Directory.CreateDirectory(colors);
        string F(byte v) => (v / 255.0).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        File.WriteAllText(Path.Combine(colors, "Contents.json"),
            $$"""{ "colors": [ { "idiom": "universal", "color": { "color-space": "srgb", "components": { "red": "{{F(background.R)}}", "green": "{{F(background.G)}}", "blue": "{{F(background.B)}}", "alpha": "1.000" } } } ], "info": { "author": "microengine", "version": 1 } }""");
        string entries = "";
        if (game.Splash)
        {
            // 128 points: @1x, @2x and @3x images, each enlarged or reduced from the game's image.
            string images = Path.Combine(assets, "LaunchImage.imageset");
            Directory.CreateDirectory(images);
            foreach (int scale in new[] { 1, 2, 3 })
            {
                var (w, h) = Fit(image, 128 * scale);
                Icons.WritePng(image.Resized(w, h), Path.Combine(images, $"launch@{scale}x.png"));
            }
            File.WriteAllText(Path.Combine(images, "Contents.json"),
                """{ "images": [ { "filename": "launch@1x.png", "idiom": "universal", "scale": "1x" }, { "filename": "launch@2x.png", "idiom": "universal", "scale": "2x" }, { "filename": "launch@3x.png", "idiom": "universal", "scale": "3x" } ], "info": { "author": "microengine", "version": 1 } }""");
            entries = "\n    <key>UIImageName</key>\n    <string>LaunchImage</string>\n    <key>UIImageRespectsSafeAreaInsets</key>\n    <true/>";
        }
        string plist = Path.Combine(host, "Info.plist");
        File.WriteAllText(plist, File.ReadAllText(plist).ReplaceLineEndings("\n").Replace("</dict>\n</plist>",
            $"  <key>UILaunchScreen</key>\n  <dict>\n    <key>UIColorName</key>\n    <string>LaunchBackground</string>{entries}\n  </dict>\n</dict>\n</plist>"));
    }

    private static string IconOrDefault(GameProject game, string target)
    {
        if (game.CheckIcon() is { } icon) return icon;
        Packaging.Warn($"{game.Id} declares no MicroEngineIcon: the {target} app gets MicroEngine's default icon. Add <MicroEngineIcon>icon.png</MicroEngineIcon> (a square PNG, 1024 pixels is best).");
        return Icons.Default;
    }

    /// <summary>Fails when the game's build output lacks an engine assembly the host needs.</summary>
    private static void RequireBackend(GameProject game, string assembly, string message)
    {
        Tool.Run(Tool.Dotnet, ["build", game.Path, "-c", "Release", "-nologo"], game.Directory, "dotnet build");
        if (!File.Exists(Path.Combine(game.TargetDir, assembly + ".dll"))) throw new CliException(message);
    }

    /// <summary>
    /// The notices a host of <paramref name="kind"/> packs as its GameNotice items (the APK's assets/licenses/, the iOS
    /// app's licenses/): the game's licence when it has one, its third-party notices, MicroEngine's licence and, in the
    /// phone hosts, which always carry SDL's native library, SDL's licence, and HarfBuzz's when the game shapes text
    /// (<paramref name="shaping"/>: it references MicroEngine.Text.Shaping).
    /// </summary>
    public static List<(string File, string Name)> Notices(string kind, string? licence, string thirdParty, bool shaping = false)
    {
        var notices = new List<(string File, string Name)>();
        if (licence is not null) notices.Add((licence, "LICENSE.txt"));
        notices.Add((thirdParty, "THIRD-PARTY-NOTICES.txt"));
        notices.Add((Path.Combine(Tool.Home, "Notices", "microengine-LICENSE.txt"), "microengine-LICENSE.txt"));
        if (kind is "android" or "ios") notices.Add((EngineNotices.PathOf(EngineNotices.Sdl), EngineNotices.Sdl));
        if (kind is "android" or "ios" && shaping) notices.Add((EngineNotices.PathOf(EngineNotices.HarfBuzz), EngineNotices.HarfBuzz));
        return notices;
    }

    /// <summary>Copies a host template into the game's obj folder and writes the game's part; returns the folder.</summary>
    private static string Stage(string kind, GameProject game, Dictionary<string, string> extra, string entryExtra)
    {
        string host = Path.Combine(game.Directory, "obj", "microengine", kind);
        Directory.CreateDirectory(host);
        // Fresh sources every time (the build's own obj and bin folders stay, so builds are incremental).
        foreach (var f in Directory.GetFiles(host, "*.cs")) File.Delete(f);
        string template = Path.Combine(Tool.Home, "Hosts", kind);
        foreach (var file in Directory.GetFiles(template, "*", SearchOption.AllDirectories))
        {
            string to = Path.Combine(host, Path.GetRelativePath(template, file));
            Directory.CreateDirectory(Path.GetDirectoryName(to)!);
            File.Copy(file, to, overwrite: true);
        }
        if (kind is "android" or "ios")
            foreach (var file in Directory.GetFiles(Path.Combine(Tool.Home, "Hosts", "mobile"), "*.cs")) File.Copy(file, Path.Combine(host, Path.GetFileName(file)), overwrite: true);
        // The host is its own project: settings of the folders above (the game's repository) do not apply to it.
        File.WriteAllText(Path.Combine(host, "Directory.Build.props"), "<Project />\n");
        File.WriteAllText(Path.Combine(host, "Directory.Build.targets"), "<Project />\n");
        File.WriteAllText(Path.Combine(host, "Directory.Packages.props"), "<Project>\n  <PropertyGroup>\n    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>\n  </PropertyGroup>\n</Project>\n");
        bool raylib = File.Exists(Path.Combine(game.TargetDir, "Raylib-cs.dll"));
        var props = new Dictionary<string, string>
        {
            ["GameProject"] = game.Path, ["GameId"] = game.Id, ["GameTitle"] = game.Title, ["GameApplicationId"] = game.ApplicationId ?? "",
            ["GameVersion"] = Packaging.ShortVersion(game.Version), ["GameOrientation"] = game.Orientation,
            ["GameContentDir"] = game.ContentDir ?? "", ["GameScriptsDir"] = game.ScriptsDir ?? "", ["GameUsesRaylib"] = raylib ? "true" : "false",
        };
        foreach (var (k, v) in extra) props[k] = v;
        var notices = Notices(kind, game.LicenseFile, game.ThirdPartyNotices, game.ShapesText);
        var sb = new StringBuilder("<Project>\n  <!-- Written by microengine from ").Append(Escape(Path.GetFileName(game.Path))).Append("; do not edit. -->\n  <PropertyGroup>\n");
        foreach (var (k, v) in props) sb.Append($"    <{k}>{Escape(v)}</{k}>\n");
        sb.Append("  </PropertyGroup>\n  <ItemGroup>\n");
        foreach (var (file, name) in notices) sb.Append($"    <GameNotice Include=\"{Escape(file)}\" Name=\"{Escape(name)}\" />\n");
        sb.Append("  </ItemGroup>\n</Project>\n");
        File.WriteAllText(Path.Combine(host, "Game.props"), sb.ToString());
        string configure = EntryPoint.Call(game, kind switch { "web" => "Web", "android" => "Android", _ => "iOS" });
        File.WriteAllText(Path.Combine(host, "GameEntry.cs"), $$"""
            // Written by microengine from {{Path.GetFileName(game.Path)}}: the game's id, title and Configure entry point.
            internal static class GameEntry
            {
                public const string Id = {{Literal(game.Id)}};
                public const string Title = {{Literal(game.Title)}};
            {{entryExtra}}
                /// <summary>The game's composition, the same one its desktop Program.cs hands to the launcher.</summary>
                public static void Configure(MicroEngine.Hosting.HostBuilder host) => {{configure}};
            }

            """);
        return host;
    }

    /// <summary>Escapes a value for an MSBuild project: XML, then MSBuild's own special characters.</summary>
    private static string Escape(string value) => SecurityElement.Escape(value)!
        .Replace("%", "%25").Replace("$", "%24").Replace("@", "%40").Replace(";", "%3B").Replace("*", "%2A").Replace("?", "%3F");

    private static string Literal(string value) => JsonSerializer.Serialize(value);

    private static void CopyTree(string from, string to)
    {
        Directory.CreateDirectory(to);
        foreach (var file in Directory.GetFiles(from, "*", SearchOption.AllDirectories))
        {
            string target = Path.Combine(to, Path.GetRelativePath(from, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }
}

/// <summary>
/// Android signing (docs/guide/signing.md): the developer's own key, read from ANDROID_KEYSTORE (the keystore file),
/// ANDROID_KEYSTORE_PASSWORD, ANDROID_KEY_ALIAS and ANDROID_KEY_PASSWORD (default: the store password). Passwords
/// reach the Android build through the environment (env:NAME), never through a command line or the output. Without a
/// key the build stops, unless --debug-key asks for this machine's debug key (a test build, never for players). The
/// tool never creates a key: a release key identifies the game for its lifetime.
/// </summary>
public sealed record AndroidSigning(string Kind, IReadOnlyList<string> Arguments, IReadOnlyList<string> Secrets)
{
    public static AndroidSigning Resolve(bool debugKey)
    {
        string? keystore = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE");
        if (string.IsNullOrEmpty(keystore))
        {
            if (debugKey) return new("debug", [], []);
            throw new CliException("Android packages for players are signed with your own key. Set ANDROID_KEYSTORE (the keystore file), ANDROID_KEYSTORE_PASSWORD and ANDROID_KEY_ALIAS "
                + "(and ANDROID_KEY_PASSWORD if it differs); docs/guide/signing.md shows how to create a key once with keytool. For a test build on your own device, add --debug-key.");
        }
        if (debugKey) throw new CliException("--debug-key and ANDROID_KEYSTORE were both given; choose one (unset ANDROID_KEYSTORE for a debug build).", 2);
        if (!File.Exists(keystore)) throw new CliException($"ANDROID_KEYSTORE points to {keystore}, which does not exist.");
        string? store = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PASSWORD");
        string? alias = Environment.GetEnvironmentVariable("ANDROID_KEY_ALIAS");
        if (string.IsNullOrEmpty(store) || string.IsNullOrEmpty(alias))
            throw new CliException("ANDROID_KEYSTORE is set, but ANDROID_KEYSTORE_PASSWORD or ANDROID_KEY_ALIAS is missing.");
        string? keyPassword = Environment.GetEnvironmentVariable("ANDROID_KEY_PASSWORD");
        return new("release",
        [
            "-p:AndroidKeyStore=true", $"-p:AndroidSigningKeyStore={Path.GetFullPath(keystore)}", $"-p:AndroidSigningKeyAlias={alias}",
            "-p:AndroidSigningStorePass=env:ANDROID_KEYSTORE_PASSWORD", $"-p:AndroidSigningKeyPass=env:{(string.IsNullOrEmpty(keyPassword) ? "ANDROID_KEYSTORE_PASSWORD" : "ANDROID_KEY_PASSWORD")}",
        ], new[] { store, keyPassword ?? "" });
    }
}
