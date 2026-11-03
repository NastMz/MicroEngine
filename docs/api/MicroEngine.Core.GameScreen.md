# Class GameScreen

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

The game's screen while it runs ([Screen](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Screen)): the displays, which one the game is on, and the options
that can change without restarting. Changes apply where the platform allows; read a property back to see what applies
(a browser or phone keeps its own window, a headless run has none).

```csharp
public sealed class GameScreen
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[GameScreen](MicroEngine.Core.GameScreen.md)

## Properties

### <a id="MicroEngine_Core_GameScreen_Adapter"></a> Adapter

The graphics adapter (GPU) in use where the platform can identify it; null otherwise.

```csharp
public string? Adapter { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Core_GameScreen_CanChange"></a> CanChange

Whether this platform can change the window and VSync while running (false headless, in browsers and on phones).

```csharp
public bool CanChange { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_GameScreen_Current"></a> Current

The options in effect, to save and return from [Settings](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Settings) at the next start: the start-only
options as the game started, the others as they are now.

```csharp
public GraphicsSettings Current { get; }
```

#### Property Value

 [GraphicsSettings](MicroEngine.Core.GraphicsSettings.md)

### <a id="MicroEngine_Core_GameScreen_Display"></a> Display

The display the game is on, or null when unknown.

```csharp
public DisplayInfo? Display { get; }
```

#### Property Value

 [DisplayInfo](MicroEngine.Core.DisplayInfo.md)?

### <a id="MicroEngine_Core_GameScreen_Displays"></a> Displays

The displays the system reports; empty where the platform cannot tell.

```csharp
public IReadOnlyList<DisplayInfo> Displays { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[DisplayInfo](MicroEngine.Core.DisplayInfo.md)\>

### <a id="MicroEngine_Core_GameScreen_FrameRateLimit"></a> FrameRateLimit

The most frames per second to draw; 0 for no limit. Applies at once on every platform that runs in real time.

```csharp
public int FrameRateLimit { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The value is negative.

### <a id="MicroEngine_Core_GameScreen_RefreshRate"></a> RefreshRate

The refresh rate frames are paced to (the display's while VSync is on), in hertz; 0 when nothing paces them to a
display (VSync off, scripted and headless runs).

```csharp
public double RefreshRate { get; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Core_GameScreen_Renderer"></a> Renderer

The graphics API in use ("direct3d11", "opengl"...) where the platform reports it; null otherwise.

```csharp
public string? Renderer { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Core_GameScreen_VSync"></a> VSync

Whether frames wait for the display's refresh. Setting it applies where [CanChange](MicroEngine.Core.GameScreen.md#MicroEngine_Core_GameScreen_CanChange); read it back.

```csharp
public bool VSync { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_GameScreen_WindowMode"></a> WindowMode

The window's mode. Setting it applies where [CanChange](MicroEngine.Core.GameScreen.md#MicroEngine_Core_GameScreen_CanChange); read it back.

```csharp
public WindowMode WindowMode { get; set; }
```

#### Property Value

 [WindowMode](MicroEngine.Core.WindowMode.md)

## Methods

### <a id="MicroEngine_Core_GameScreen_MoveToDisplay_System_Int32_"></a> MoveToDisplay\(int\)

Moves the game to display <code class="paramref">index</code> of [Displays](MicroEngine.Core.GameScreen.md#MicroEngine_Core_GameScreen_Displays); returns whether it applied.

```csharp
public bool MoveToDisplay(int index)
```

#### Parameters

`index` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_GameScreen_ToggleFullscreen"></a> ToggleFullscreen\(\)

Switches between a window and full screen: through the platform's display control where it has one, otherwise by
asking the platform ([ToggleFullscreen](MicroEngine.Core.IPlatform.md#MicroEngine_Core_IPlatform_ToggleFullscreen); the headless platform counts the request). The
engine calls it when the full-screen action is pressed ([Fullscreen](MicroEngine.Input.ActionNames.md#MicroEngine_Input_ActionNames_Fullscreen)).

```csharp
public bool ToggleFullscreen()
```

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether the window's mode changed through the display control.

</div>
