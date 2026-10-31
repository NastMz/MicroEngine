using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Util;
using Java.Interop;
using MicroEngine.Raylib;

namespace MicroEngine.Android;

/// <summary>
/// The game's only activity. It is a <see cref="NativeActivity"/> whose library is raylib: raylib's android_main
/// owns the window, EGL context and input queue, and calls main() (native/microengine_main.c), which calls
/// <see cref="RunGame"/> on the native-activity thread. The game then runs on <see cref="RaylibPlatform"/>, the same
/// backend as on desktop and in the browser.
/// <para>Launch extras (for tests, as <c>adb shell am start</c> passes them): <c>script</c> (a file in assets/scripts),
/// <c>env</c> ("NAME=value;..."). Captures go to the app's external files folder, <c>captures/&lt;script&gt;</c>.</para>
/// </summary>
[Activity(Name = "dev.microengine.GameActivity", Label = "@string/app_name", MainLauncher = true, Exported = true, LaunchMode = LaunchMode.SingleTask,
    ScreenOrientation = ScreenOrientation.SensorLandscape, Theme = "@android:style/Theme.NoTitleBar.Fullscreen",
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.KeyboardHidden | ConfigChanges.Keyboard)]
[MetaData("android.app.lib_name", Value = "raylib")]
public sealed class GameActivity : NativeActivity
{
    private const string Tag = "MicroEngine";

    [Export("runGame")]
    public void RunGame()
    {
        MobileLaunch launch;
        try { launch = ReadLaunch(); }
        catch (Exception e) when (e is FormatException or IOException or Java.IO.IOException)
        {
            Log.Error(Tag, $"microengine: error: {e.Message}");
            return;
        }
        // Saves go to the app's private files folder (SaveStore's Linux rule under XDG_DATA_HOME).
        System.Environment.SetEnvironmentVariable("XDG_DATA_HOME", FilesDir!.AbsolutePath);
        int code = MobileRun.Run(launch,
            (game, scripted) => new RaylibPlatform(game.Settings, new DesktopOptions(FixedStep: scripted)),
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
}
