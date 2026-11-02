// ARCH-1 P3: SDL_GPU depth-tested 3D composited with 2D on one device.
//
//   P3Gpu math                                      System.Numerics conventions (no GPU)
//   P3Gpu gpu      --driver direct3d12|vulkan       variant 1: pure SDL_GPU 3D + 2D passes
//   P3Gpu renderer --driver direct3d12|vulkan       variant 2: SDL_Renderer "gpu" 2D + SDL_GPU 3D on its device
//   P3Gpu all                                       everything, on every driver this machine has
//   P3Gpu compare a.png b.png [c.png d.png ...]     differing pixels per pair (edge vs interior)
//
// Options: --hidden (no presentation, capture frames only), --frames N (rotating frames before the captures,
//          default 120), --debug (SDL_GPU debug mode, SDL log at verbose), --low-power (prefer the integrated
//          adapter; pure SDL_GPU variant only), --out DIR (PNG captures, default ./out).
// Exit code: number of failed checks (0 = all passed).
using P3Gpu;
using SDL;
using static SDL.SDL3;

System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
Console.OutputEncoding = System.Text.Encoding.UTF8;
var command = args.Length > 0 ? args[0] : "all";
var options = Options.Parse(args.Skip(1).ToArray());
Console.WriteLine($"P3 {command}: SDL {Version(SDL_GetVersion())} (ppy.SDL3-CS 2026.1002.1), .NET {Environment.Version}, {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");

if (command == "math") return Conventions.Run();
if (command == "compare") return Compare.Run(options.Rest);

SdlLog.Install(options.Debug);
if (!SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO)) { Console.Error.WriteLine($"SDL_Init: {SDL_GetError()}"); return 100; }
var drivers = Enumerable.Range(0, SDL_GetNumGPUDrivers()).Select(i => SDL_GetGPUDriver(i)!).ToArray();
Console.WriteLine($"SDL_GPU drivers compiled in: {string.Join(", ", drivers)}");

var failures = 0;
try
{
    switch (command)
    {
        case "gpu": failures += Guard(() => PureGpu.Run(options)); break;
        case "renderer":
            failures += Guard(() => RendererInterop.Run(options, deviceFirst: false));
            failures += Guard(() => RendererInterop.Run(options, deviceFirst: true));
            break;
        case "all":
            failures += Conventions.Run();
            PureGpu.HintPrecedence();
            foreach (var driver in new[] { "direct3d12", "vulkan" }.Where(drivers.Contains))
            {
                var o = options with { Driver = driver };
                failures += Guard(() => PureGpu.Run(o));
                failures += Guard(() => RendererInterop.Run(o, deviceFirst: false));
                failures += Guard(() => RendererInterop.Run(o, deviceFirst: true));
            }
            break;
        default:
            Console.Error.WriteLine($"unknown command {command}");
            return 100;
    }
}
finally
{
    SDL_Quit();
}
Console.WriteLine(failures == 0 ? "ALL CHECKS PASSED" : $"{failures} CHECK(S) FAILED");
return failures;

static int Guard(Func<int> run)
{
    try { return run(); }
    catch (Exception e) { Console.WriteLine($"  FAIL exception: {e.Message}"); return 1; }
}

static string Version(int v) => $"{v / 1000000}.{v / 1000 % 1000}.{v % 1000}";

public sealed record Options(string Driver, bool Hidden, int Frames, bool Debug, bool LowPower, string Out, string[] Rest)
{
    public string Suffix => LowPower ? "-lowpower" : "";

    public static Options Parse(string[] args)
    {
        var o = new Options("vulkan", false, 120, false, false, Path.GetFullPath("out"), []);
        if (args.Length > 0 && !args[0].StartsWith("--", StringComparison.Ordinal)) return o with { Rest = args };
        for (var i = 0; i < args.Length; i++)
        {
            o = args[i] switch
            {
                "--driver" => o with { Driver = args[++i] },
                "--hidden" => o with { Hidden = true },
                "--frames" => o with { Frames = int.Parse(args[++i]) },
                "--debug" => o with { Debug = true },
                "--low-power" => o with { LowPower = true },
                "--out" => o with { Out = Path.GetFullPath(args[++i]) },
                _ => throw new ArgumentException($"unknown option {args[i]}"),
            };
        }
        return o;
    }
}
