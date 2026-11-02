# Enum GpuPreference

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

Which GPU a game prefers on machines with an integrated and a dedicated one. The system has the last word: on
Windows, Direct3D 11 and OpenGL use the adapter Windows assigns to the application (Settings, Display, Graphics), which
no backend overrides; SDL passes the preference to its Metal (macOS, iOS) and GPU (Direct3D 12, Vulkan, Metal)
renderers; raylib's OpenGL ignores it.

```csharp
public enum GpuPreference
```

## Fields

`Automatic = 0` 

The backend's and system's default (the high-performance GPU for SDL's Metal and GPU renderers).



`HighPerformance = 1` 

Ask for the high-performance (usually dedicated) GPU where the backend can choose.



`PowerSaving = 2` 

Ask for the low-power (usually integrated) GPU where the backend can choose: light games on laptops.

</div>
