# Class GameSettings

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

Window/backbuffer settings a game declares. The virtual size is the canvas everything is drawn on: 320×180 pixel art
by default, or a high-resolution canvas such as 1280×720 or 1920×1080 with [HighResolution2D](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_HighResolution2D).

```csharp
public sealed record GameSettings : IEquatable<GameSettings>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[GameSettings](MicroEngine.Core.GameSettings.md)

#### Implements

[IEquatable<GameSettings\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Core_GameSettings__ctor_System_String_System_Int32_System_Int32_System_Int32_"></a> GameSettings\(string, int, int, int\)

Window/backbuffer settings a game declares. The virtual size is the canvas everything is drawn on: 320×180 pixel art
by default, or a high-resolution canvas such as 1280×720 or 1920×1080 with [HighResolution2D](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_HighResolution2D).

```csharp
public GameSettings(string Title, int VirtualWidth = 320, int VirtualHeight = 180, int Scale = 4)
```

#### Parameters

`Title` [string](https://learn.microsoft.com/dotnet/api/system.string)

Window title and application name shown by the system.

`VirtualWidth` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Width of the virtual canvas in pixels (320 by default). Everything is drawn in these pixels and scaled to the window.

`VirtualHeight` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Height of the virtual canvas in pixels (180 by default).

`Scale` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Initial window size as a multiple of the virtual size on desktop (4 by default); the launcher's <code>--scale</code> overrides
it. A window that would not fit the display opens at the largest size that does.

## Properties

### <a id="MicroEngine_Core_GameSettings_Background"></a> Background

Colour the canvas is cleared to before the scenes draw each frame (black by default).

```csharp
public Color Background { get; init; }
```

#### Property Value

 Color

### <a id="MicroEngine_Core_GameSettings_DefaultTextureFilter"></a> DefaultTextureFilter

The filter textures get when they do not choose one: [TextureFilter](MicroEngine.Core.GameSettings.md#MicroEngine_Core_GameSettings_TextureFilter), or the magnification filter of the
presentation's default sampler.

```csharp
public TextureFilter DefaultTextureFilter { get; }
```

#### Property Value

 TextureFilter

### <a id="MicroEngine_Core_GameSettings_Graphics"></a> Graphics

Graphics options: platform, graphics API, VSync, frame limit, GPU, window mode and display (all automatic by default).

```csharp
public GraphicsSettings Graphics { get; init; }
```

#### Property Value

 [GraphicsSettings](MicroEngine.Core.GraphicsSettings.md)

### <a id="MicroEngine_Core_GameSettings_Letterbox"></a> Letterbox

Colour of the window around the frame when the window's shape differs from the canvas (black by default).

```csharp
public Color Letterbox { get; init; }
```

#### Property Value

 Color

### <a id="MicroEngine_Core_GameSettings_Presentation"></a> Presentation

How the canvas reaches the window, one choice per field (scale rounding, aspect, the canvas's upscale sampler, the
textures' default sampler, the capture scale, whole-factor windows, camera snapping, the UI scale).
[PixelArt](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_PixelArt) by default; a high-resolution game sets
[HighResolution2D](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_HighResolution2D), and a game that needs another combination starts from a preset
and changes what it needs: <code>Presentation = Presentation.PixelArt with { Upscale = SamplerDescription.LinearClamp }</code>.

```csharp
public Presentation Presentation { get; init; }
```

#### Property Value

 Presentation

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

The value is null.

### <a id="MicroEngine_Core_GameSettings_Scale"></a> Scale

Initial window size as a multiple of the virtual size on desktop (4 by default); the launcher's <code>--scale</code> overrides
it. A window that would not fit the display opens at the largest size that does.

```csharp
public int Scale { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_GameSettings_TextureFilter"></a> TextureFilter

The filter of textures created without one (images, atlases). Null (the default) follows the presentation's
default sampler ([DefaultSampler](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_DefaultSampler)): nearest for pixel art, linear for
high-resolution 2D. Each texture can still choose its own. Glyph pages of TTF/OTF fonts do not follow it: their
filter is [Filter](MicroEngine.Graphics.FontOptions.md#MicroEngine_Graphics_FontOptions_Filter), or linear for antialiased text and nearest otherwise.

```csharp
public TextureFilter? TextureFilter { get; init; }
```

#### Property Value

 TextureFilter?

### <a id="MicroEngine_Core_GameSettings_Title"></a> Title

Window title and application name shown by the system.

```csharp
public string Title { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Core_GameSettings_UpdateRate"></a> UpdateRate

Fixed simulation rate in updates per second.

```csharp
public int UpdateRate { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_GameSettings_VirtualHeight"></a> VirtualHeight

Height of the virtual canvas in pixels (180 by default).

```csharp
public int VirtualHeight { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_GameSettings_VirtualWidth"></a> VirtualWidth

Width of the virtual canvas in pixels (320 by default). Everything is drawn in these pixels and scaled to the window.

```csharp
public int VirtualWidth { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Core_GameSettings__Clone__"></a> <Clone\>$\(\)

```csharp
public GameSettings <Clone>$()
```

#### Returns

 [GameSettings](MicroEngine.Core.GameSettings.md)

### <a id="MicroEngine_Core_GameSettings_Deconstruct_System_String__System_Int32__System_Int32__System_Int32__"></a> Deconstruct\(out string, out int, out int, out int\)

```csharp
public void Deconstruct(out string Title, out int VirtualWidth, out int VirtualHeight, out int Scale)
```

#### Parameters

`Title` [string](https://learn.microsoft.com/dotnet/api/system.string)

`VirtualWidth` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`VirtualHeight` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Scale` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_GameSettings_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_GameSettings_Equals_MicroEngine_Core_GameSettings_"></a> Equals\(GameSettings?\)

```csharp
public bool Equals(GameSettings? other)
```

#### Parameters

`other` [GameSettings](MicroEngine.Core.GameSettings.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_GameSettings_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_GameSettings_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Core_GameSettings_op_Equality_MicroEngine_Core_GameSettings_MicroEngine_Core_GameSettings_"></a> operator ==\(GameSettings?, GameSettings?\)

```csharp
public static bool operator ==(GameSettings? left, GameSettings? right)
```

#### Parameters

`left` [GameSettings](MicroEngine.Core.GameSettings.md)?

`right` [GameSettings](MicroEngine.Core.GameSettings.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_GameSettings_op_Inequality_MicroEngine_Core_GameSettings_MicroEngine_Core_GameSettings_"></a> operator \!=\(GameSettings?, GameSettings?\)

```csharp
public static bool operator !=(GameSettings? left, GameSettings? right)
```

#### Parameters

`left` [GameSettings](MicroEngine.Core.GameSettings.md)?

`right` [GameSettings](MicroEngine.Core.GameSettings.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
