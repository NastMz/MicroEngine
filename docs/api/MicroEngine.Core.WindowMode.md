# Enum WindowMode

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

How the game's window covers the screen ([WindowMode](MicroEngine.Core.GraphicsSettings.md#MicroEngine_Core_GraphicsSettings_WindowMode), [WindowMode](MicroEngine.Core.GameScreen.md#MicroEngine_Core_GameScreen_WindowMode)).

```csharp
public enum WindowMode
```

## Fields

`ExclusiveFullscreen = 2` 

Exclusive full screen at the display's desktop resolution and refresh rate, where the backend offers it (SDL and
raylib on desktop); elsewhere the same as [Fullscreen](MicroEngine.Core.WindowMode.md).



`Fullscreen = 1` 

A borderless window covering its display at the display's current resolution: switching is instant and other windows can show above it.



`Windowed = 0` 

A window the player can move and resize (the default on desktop).

</div>
