# Class StartScreen

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

The screen a game opens on while it loads: its start-screen image (<code>MicroEngineSplashImage</code>, by default the
icon) centred on its colour (<code>MicroEngineSplashColor</code>), and "Made with MicroEngine" below when the game asks
for it (<code>MicroEngineSplashBranding</code>). Phones and browsers show the same image and colour before the engine
starts (launch screens, the loading page), so the player sees one continuous screen from the tap to the first frame.
[GameLoop](MicroEngine.Core.GameLoop.md) draws it once, before [Load](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Load), on platforms that run in real time; it adds no
delay, and an error while loading is reported as before.

```csharp
public static class StartScreen
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[StartScreen](MicroEngine.Core.StartScreen.md)

## Fields

### <a id="MicroEngine_Core_StartScreen_Branding"></a> Branding

The text shown when the game turns branding on.

```csharp
public const string Branding = "Made with MicroEngine"
```

#### Field Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Core_StartScreen_Draw_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_IGraphicsDevice_MicroEngine_Core_AppIdentity_"></a> Draw\(IRenderer, IGraphicsDevice, AppIdentity\)

Draws the start screen of <code class="paramref">identity</code> into <code class="paramref">r</code>; textures made from <code class="paramref">graphics</code> are released before it returns.

```csharp
public static void Draw(IRenderer r, IGraphicsDevice graphics, AppIdentity identity)
```

#### Parameters

`r` IRenderer

`graphics` IGraphicsDevice

`identity` [AppIdentity](MicroEngine.Core.AppIdentity.md)

### <a id="MicroEngine_Core_StartScreen_ImageSize_MicroEngine_Graphics_PixelImage_System_Int32_System_Int32_"></a> ImageSize\(PixelImage, int, int\)

The image size on a <code class="paramref">canvasWidth</code> by <code class="paramref">canvasHeight</code> canvas: half the shorter
side, in whole pixels, keeping the image's shape.

```csharp
public static (int Width, int Height) ImageSize(PixelImage image, int canvasWidth, int canvasHeight)
```

#### Parameters

`image` PixelImage

`canvasWidth` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`canvasHeight` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 \([int](https://learn.microsoft.com/dotnet/api/system.int32) [Width](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32\-.width), [int](https://learn.microsoft.com/dotnet/api/system.int32) [Height](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32\-.height)\)

</div>
