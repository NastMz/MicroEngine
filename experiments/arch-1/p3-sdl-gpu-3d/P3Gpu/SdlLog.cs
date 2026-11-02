using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SDL;
using static SDL.SDL3;

namespace P3Gpu;

/// <summary>Routes SDL's log (GPU validation and driver warnings included) to the console, so a run shows them.</summary>
public static unsafe class SdlLog
{
    public static void Install(bool verbose)
    {
        SDL_SetLogPriorities(verbose ? SDL_LogPriority.SDL_LOG_PRIORITY_VERBOSE : SDL_LogPriority.SDL_LOG_PRIORITY_WARN);
        SDL_SetLogOutputFunction(&Output, IntPtr.Zero);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void Output(IntPtr userdata, int category, SDL_LogPriority priority, byte* message)
        => Console.WriteLine($"  SDL LOG {((SDL_LogCategory)category).ToString().Replace("SDL_LOG_CATEGORY_", "")} {priority.ToString().Replace("SDL_LOG_PRIORITY_", "")}: {Marshal.PtrToStringUTF8((IntPtr)message)}");
}
