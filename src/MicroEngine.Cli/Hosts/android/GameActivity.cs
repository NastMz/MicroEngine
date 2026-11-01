using Android.App;
using Android.Content.PM;
using Android.Util;
using MicroEngine.Core;
using MicroEngine.Sdl;
using Org.Libsdl.App;

namespace MicroEngine.Android;

/// <summary>
/// The game's only activity: SDL's <see cref="SDLActivity"/>, which owns the surface, input, audio and the activity
/// lifecycle, and runs <see cref="Main"/> on its own thread once the window exists. The game then runs on
/// <see cref="SdlPlatform"/>, the same platform as on desktop, with on-screen touch controls.
/// <para>Launch extras (for tests, as <c>adb shell am start</c> passes them): <c>script</c> (a file in assets/scripts),
/// <c>env</c> ("NAME=value;..."). Captures go to the app's external files folder, <c>captures/&lt;script&gt;</c>.</para>
/// </summary>
[Activity(Name = "dev.microengine.GameActivity", Label = "@string/app_name", Icon = "@mipmap/appicon", MainLauncher = true, Exported = true, LaunchMode = LaunchMode.SingleTask,
    ScreenOrientation = GameEntry.ScreenOrientation, Theme = "@android:style/Theme.NoTitleBar.Fullscreen",
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout | ConfigChanges.KeyboardHidden
        | ConfigChanges.Keyboard | ConfigChanges.Navigation | ConfigChanges.UiMode)]
public sealed class GameActivity : SDLActivity
{
    private const string Tag = "MicroEngine";

    /// <summary>Only SDL itself: the game is .NET code, not a native "main" library.</summary>
    protected override string[] GetLibraries() => ["SDL3"];

    protected override void Main()
    {
        MobileLaunch launch;
        try
        {
            launch = ReadLaunch();
            ContentFolder.DefaultRoot = UnpackContent();
        }
        catch (Exception e) when (e is FormatException or IOException or Java.IO.IOException)
        {
            Log.Error(Tag, $"microengine: error: {e.Message}");
            return;
        }
        // Saves go to the app's private files folder (SaveStore's Linux rule under XDG_DATA_HOME).
        System.Environment.SetEnvironmentVariable("XDG_DATA_HOME", FilesDir!.AbsolutePath);
        int code = MobileRun.Run(launch,
            (game, scripted) => SdlPlatform.Profile.Create(new PlatformRequest(game.Settings) { FixedStep = scripted, Identity = AppIdentity.Of(game) }),
            line => Log.Info(Tag, line));
        Log.Info(Tag, $"microengine: exit {code}");
    }

    private MobileLaunch ReadLaunch()
    {
        var extras = Intent?.Extras;
        string? name = extras?.GetString("script");
        string? text = null;
        if (!string.IsNullOrEmpty(name))
        {
            using var stream = Assets!.Open($"scripts/{Path.GetFileName(name)}");
            using var reader = new StreamReader(stream);
            text = reader.ReadToEnd();
        }
        string captures = Path.Combine(GetExternalFilesDir(null)!.AbsolutePath, "captures", Path.GetFileNameWithoutExtension(name ?? "play"));
        if (Directory.Exists(captures)) Directory.Delete(captures, recursive: true);
        return new MobileLaunch(name, text, MobileLaunch.ParseEnvironment(extras?.GetString("env")), captures);
    }

    /// <summary>
    /// The game's Content files travel in the APK's assets, which .NET file APIs cannot open; they are copied once per
    /// installed version into the app's files folder, where <see cref="ContentFolder"/> reads them as on desktop.
    /// </summary>
    private string? UnpackContent()
    {
        if (Assets!.List("Content") is not { Length: > 0 }) return null;
        var package = PackageManager!.GetPackageInfo(PackageName!, 0)!;
        // An update or reinstall changes the install time, so a new version never reads the previous one's files.
        string stamp = $"{package.LastUpdateTime}";
        string root = Path.Combine(FilesDir!.AbsolutePath, "content");
        string marker = Path.Combine(FilesDir!.AbsolutePath, "content.stamp");
        if (File.Exists(marker) && File.ReadAllText(marker) == stamp && Directory.Exists(root)) return root;
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        Copy("Content", root);
        File.WriteAllText(marker, stamp);
        return root;
    }

    private void Copy(string asset, string target)
    {
        var children = Assets!.List(asset);
        if (children is { Length: > 0 })
        {
            Directory.CreateDirectory(target);
            foreach (var child in children) Copy($"{asset}/{child}", Path.Combine(target, child));
            return;
        }
        using var input = Assets.Open(asset);
        using var output = File.Create(target);
        input.CopyTo(output);
    }
}
