using System.Runtime.InteropServices;
using System.Text;
using SDL;

namespace MicroEngine.Sdl;

/// <summary>
/// The engine's own declarations of the SDL functions whose SDL3-CS binding .NET's WebAssembly interpreter cannot call:
/// SDL3-CS types <c>SDL_WindowFlags</c> as a 64-bit enum, and the interpreter passes a 64-bit enum parameter or return
/// value in a 32-bit slot (<c>RuntimeError: function signature mismatch</c>); the same entry point declared with a plain
/// <see cref="ulong"/> is called correctly. Function-pointer parameters (<c>delegate* unmanaged</c>) have the same kind
/// of defect; the platform calls none in a browser (its lifecycle event watch is for phones). Used only in browsers:
/// elsewhere the platform calls SDL3-CS directly. The browser build links SDL statically, so "SDL3" is the archive
/// SDL3.a (src/MicroEngine.Sdl/native/build.sh).
/// </summary>
internal static unsafe partial class SdlWeb
{
    /// <summary><c>SDL_CreateWindow</c> with the flags as a plain 64-bit integer.</summary>
    public static SDL_Window* CreateWindow(string title, int width, int height, SDL_WindowFlags flags)
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(title + "\0");
        fixed (byte* p = utf8) return SDL_CreateWindow(p, width, height, (ulong)flags);
    }

    /// <summary><c>SDL_GetWindowFlags</c> with the result as a plain 64-bit integer.</summary>
    public static SDL_WindowFlags GetWindowFlags(SDL_Window* window) => (SDL_WindowFlags)SDL_GetWindowFlags(window);

    [LibraryImport("SDL3", EntryPoint = "SDL_CreateWindow")]
    private static partial SDL_Window* SDL_CreateWindow(byte* title, int w, int h, ulong flags);

    [LibraryImport("SDL3", EntryPoint = "SDL_GetWindowFlags")]
    private static partial ulong SDL_GetWindowFlags(SDL_Window* window);
}
