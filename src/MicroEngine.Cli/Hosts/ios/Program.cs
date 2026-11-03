using System.Runtime.InteropServices;
using Foundation;
using MicroEngine.Core;
using MicroEngine.Sdl;
using static SDL.SDL3;

namespace MicroEngine.iOS;

/// <summary>
/// iOS host: SDL owns the application. <see cref="Main"/> hands control to SDL's UIKit delegate (<c>SDL_RunApp</c>),
/// which creates the window and calls <see cref="RunGame"/> on the main thread once the app has launched; the game then
/// runs on <see cref="SdlPlatform"/> (Metal) with on-screen touch controls, the same platform as on desktop and Android.
/// <para>Arguments after the app's own (as <c>xcrun simctl launch --console-pty ... -- --script smoke.script --env
/// "NAME=value;..."</c> passes them) select an input script from the bundle and developer environment variables.
/// Results go to standard output, captures to <c>Library/Caches/captures/&lt;script&gt;</c>, and a scripted run ends
/// the app, so a test can read them.</para>
/// </summary>
public static unsafe class Program
{
    private static void Main(string[] args)
    {
        // On iOS SDL is a framework inside the app (Frameworks/SDL3.framework), not a library .NET finds by the name
        // "SDL3", so the bindings' imports are pointed at it before the first SDL call.
        string sdl = Path.Combine(NSBundle.MainBundle.PrivateFrameworksPath!, "SDL3.framework", "SDL3");
        NativeLibrary.SetDllImportResolver(typeof(SDL.SDL3).Assembly,
            (name, assembly, path) => name == "SDL3" ? NativeLibrary.Load(sdl) : IntPtr.Zero);
        SDL_RunApp(0, null, &RunGame, IntPtr.Zero);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static int RunGame(int argc, byte** argv)
    {
        string[] args = NSProcessInfo.ProcessInfo.Arguments.Skip(1).ToArray();
        string? Option(string name) { int i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
        int code = Run(Option("--script"), Option("--env"));
        Console.WriteLine($"microengine: exit {code}");
        // SDL keeps the app alive after this function returns; a scripted run ends the process so the test sees it end.
        if (Option("--script") is not null) Environment.Exit(code);
        return code;
    }

    private static int Run(string? scriptName, string? env)
    {
        string library = NSSearchPath.GetDirectories(NSSearchPathDirectory.LibraryDirectory, NSSearchPathDomain.User)[0];
        // Saves go to the app's Library/Application Support (SaveStore's rule under XDG_DATA_HOME); the sandbox's home
        // folder itself is not writable. Content files are bundle resources.
        Environment.SetEnvironmentVariable("XDG_DATA_HOME", Path.Combine(library, "Application Support"));
        string content = Path.Combine(NSBundle.MainBundle.BundlePath, "Content");
        if (Directory.Exists(content)) ContentFolder.DefaultRoot = content;
        MobileLaunch launch;
        try
        {
            string? text = scriptName is null ? null : File.ReadAllText(Path.Combine(NSBundle.MainBundle.BundlePath, "scripts", Path.GetFileName(scriptName)));
            string captures = Path.Combine(library, "Caches", "captures", Path.GetFileNameWithoutExtension(scriptName ?? "play"));
            if (Directory.Exists(captures)) Directory.Delete(captures, recursive: true);
            launch = new MobileLaunch(scriptName, text, MobileLaunch.ParseEnvironment(env), captures);
        }
        catch (Exception e) when (e is FormatException or IOException)
        {
            Console.WriteLine($"microengine: error: {e.Message}");
            return 2;
        }
        return MobileRun.Run(launch,
            (game, scripted) => SdlPlatform.Profile.Create(new PlatformRequest(game.Settings) { WaitForDisplay = !scripted, StartGamepadsAfterFirstFrame = !scripted && AppIdentity.Of(game) is { Splash: true }, AudioOutput = AudioOutput.Device, Identity = AppIdentity.Of(game) }),
            Console.WriteLine);
    }
}
