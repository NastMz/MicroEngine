using Foundation;
using MicroEngine.Core;
using UIKit;

namespace MicroEngine.iOS;

public static class Program
{
    private static void Main(string[] args) => UIApplication.Main(args, null, typeof(AppDelegate));
}

/// <summary>
/// Runs the selected game (<see cref="GameEntry"/>) once on <see cref="HeadlessPlatform"/>: the game's real code,
/// simulated without drawing, because raylib 6.0 has no iOS platform. Arguments after the app's own (as
/// <c>xcrun simctl launch --console ... -- --script smoke.script --env "NAME=value;..."</c> passes them) select an
/// input script from the bundle and developer environment variables. Results go to standard output, and a scripted
/// run ends the app, so a test can read them. The app shows no view: there is nothing to draw it with yet.
/// </summary>
[Register("AppDelegate")]
public sealed class AppDelegate : UIApplicationDelegate
{
    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        string[] args = NSProcessInfo.ProcessInfo.Arguments.Skip(1).ToArray();
        string? Option(string name) { int i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
        new Thread(() =>
        {
            int code = Run(Option("--script"), Option("--env"));
            if (Option("--script") is not null) Environment.Exit(code);
        }) { IsBackground = true, Name = "game" }.Start();
        return true;
    }

    private static int Run(string? scriptName, string? env)
    {
        string library = NSSearchPath.GetDirectories(NSSearchPathDirectory.LibraryDirectory, NSSearchPathDomain.User)[0];
        // Saves go to the app's Library/Application Support (SaveStore's rule under XDG_DATA_HOME); the sandbox's home
        // folder itself is not writable.
        Environment.SetEnvironmentVariable("XDG_DATA_HOME", Path.Combine(library, "Application Support"));
        MobileLaunch launch;
        try
        {
            string? text = scriptName is null ? null : File.ReadAllText(Path.Combine(NSBundle.MainBundle.BundlePath, "scripts", Path.GetFileName(scriptName)));
            launch = new MobileLaunch(scriptName, text, MobileLaunch.ParseEnvironment(env), Path.Combine(library, "Caches", "captures"));
        }
        catch (Exception e) when (e is FormatException or IOException)
        {
            Console.WriteLine($"microengine: error: {e.Message}");
            return 2;
        }
        int code = MobileRun.Run(launch, (game, _) => HeadlessPlatform.For(game.Settings), Console.WriteLine);
        Console.WriteLine($"microengine: exit {code}");
        return code;
    }
}
