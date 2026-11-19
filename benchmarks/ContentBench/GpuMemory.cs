using System.Runtime.InteropServices;

namespace ContentBench;

/// <summary>
/// The video memory this process holds, as the operating system accounts it: on Windows the sum over every adapter of
/// DXGI's <c>QueryVideoMemoryInfo</c> (the local and the non-local segment groups), which counts what Direct3D 11,
/// Direct3D 12 and Vulkan allocate for the process. −1 where it cannot be asked (other systems).
/// </summary>
internal static unsafe class GpuMemory
{
    private static readonly Guid Factory1 = new("770aae78-f26f-4dba-a829-253c83d1b387");
    private static readonly Guid Adapter3 = new("645967A4-1392-4310-A798-8053CE3E93FD");

    [StructLayout(LayoutKind.Sequential)]
    private struct VideoMemoryInfo { public ulong Budget, CurrentUsage, AvailableForReservation, CurrentReservation; }

    [DllImport("dxgi.dll")]
    private static extern int CreateDXGIFactory1(Guid* iid, void** factory);

    public static long CurrentUsage()
    {
        if (!OperatingSystem.IsWindows()) return -1;
        try
        {
            void* factory;
            var factoryId = Factory1;
            if (CreateDXGIFactory1(&factoryId, &factory) < 0) return -1;
            long total = 0;
            bool any = false;
            for (uint index = 0; ; index++)
            {
                void* adapter;
                // IDXGIFactory1::EnumAdapters1 is slot 12.
                if (((delegate* unmanaged[Stdcall]<void*, uint, void**, int>)(*(void***)factory)[12])(factory, index, &adapter) < 0) break;
                void* adapter3;
                var adapterId = Adapter3;
                if (((delegate* unmanaged[Stdcall]<void*, Guid*, void**, int>)(*(void***)adapter)[0])(adapter, &adapterId, &adapter3) >= 0)
                {
                    for (int group = 0; group < 2; group++)
                    {
                        VideoMemoryInfo info;
                        // IDXGIAdapter3::QueryVideoMemoryInfo is slot 14.
                        if (((delegate* unmanaged[Stdcall]<void*, uint, int, VideoMemoryInfo*, int>)(*(void***)adapter3)[14])(adapter3, 0, group, &info) >= 0)
                        {
                            total += (long)info.CurrentUsage;
                            any = true;
                        }
                    }
                    Release(adapter3);
                }
                Release(adapter);
            }
            Release(factory);
            return any ? total : -1;
        }
        catch (DllNotFoundException) { return -1; }
        catch (EntryPointNotFoundException) { return -1; }
    }

    private static void Release(void* unknown) => ((delegate* unmanaged[Stdcall]<void*, uint>)(*(void***)unknown)[2])(unknown);
}
