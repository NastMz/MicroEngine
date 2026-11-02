# Class WindowSizing

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

The window sizes desktop platforms open and allow, shared by every backend so they agree.

```csharp
public static class WindowSizing
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[WindowSizing](MicroEngine.Core.WindowSizing.md)

## Fields

### <a id="MicroEngine_Core_WindowSizing_MaximumDisplayShare"></a> MaximumDisplayShare

Share of the display's usable area a window may take when it opens.

```csharp
public const float MaximumDisplayShare = 0.9
```

#### Field Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_Core_WindowSizing_Initial_MicroEngine_Core_GameSettings_System_Int32_System_Int32_System_Int32_"></a> Initial\(GameSettings, int, int, int\)

The size a window opens at: the canvas times <code class="paramref">scale</code>, or, when that does not fit in
[MaximumDisplayShare](MicroEngine.Core.WindowSizing.md#MicroEngine_Core_WindowSizing_MaximumDisplayShare) of the display's usable area, the largest size that does: a whole factor of the
canvas for [Pixel](MicroEngine.Graphics.CanvasScaling.md) when one fits, otherwise the canvas's shape scaled to fit (a
1920×1080 canvas on a 1366×768 laptop).

```csharp
public static (int Width, int Height) Initial(GameSettings settings, int scale, int usableWidth, int usableHeight)
```

#### Parameters

`settings` [GameSettings](MicroEngine.Core.GameSettings.md)

The game's canvas and scaling.

`scale` [int](https://learn.microsoft.com/dotnet/api/system.int32)

The requested multiple of the canvas ([Scale](MicroEngine.Core.GameSettings.md#MicroEngine_Core_GameSettings_Scale) or <code>--scale</code>).

`usableWidth` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Width of the display's usable area; 0 or less when unknown, which keeps the requested size.

`usableHeight` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Height of the display's usable area; 0 or less when unknown.

#### Returns

 \([int](https://learn.microsoft.com/dotnet/api/system.int32) [Width](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32\-.width), [int](https://learn.microsoft.com/dotnet/api/system.int32) [Height](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32\-.height)\)

### <a id="MicroEngine_Core_WindowSizing_Minimum_MicroEngine_Core_GameSettings_"></a> Minimum\(GameSettings\)

The smallest window size: the canvas itself, or a quarter of it for canvases wider than 640 pixels (an HD canvas
would otherwise forbid windows smaller than a laptop screen).

```csharp
public static (int Width, int Height) Minimum(GameSettings settings)
```

#### Parameters

`settings` [GameSettings](MicroEngine.Core.GameSettings.md)

#### Returns

 \([int](https://learn.microsoft.com/dotnet/api/system.int32) [Width](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32\-.width), [int](https://learn.microsoft.com/dotnet/api/system.int32) [Height](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32\-.height)\)

</div>
