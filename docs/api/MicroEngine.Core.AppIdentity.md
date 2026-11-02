# Class AppIdentity

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Name, id, version and icon](../guide/app-identity.md)  

The application identity a game declares once, in its project file (docs/guide/app-identity.md):
<code>ApplicationTitle</code>, <code>ApplicationId</code>, <code>Version</code>, <code>MicroEngineIcon</code> and
<code>MicroEngineOrientation</code>. The MicroEngine build targets write it into the game assembly; packaging turns it into
each system's metadata, and platforms read it at run time (application name, window icon, phone orientation).

```csharp
public sealed record AppIdentity : IEquatable<AppIdentity>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AppIdentity](MicroEngine.Core.AppIdentity.md)

#### Implements

[IEquatable<AppIdentity\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Core_AppIdentity__ctor_System_String_System_String_System_String_MicroEngine_Core_AppOrientation_"></a> AppIdentity\(string, string?, string, AppOrientation\)

The application identity a game declares once, in its project file (docs/guide/app-identity.md):
<code>ApplicationTitle</code>, <code>ApplicationId</code>, <code>Version</code>, <code>MicroEngineIcon</code> and
<code>MicroEngineOrientation</code>. The MicroEngine build targets write it into the game assembly; packaging turns it into
each system's metadata, and platforms read it at run time (application name, window icon, phone orientation).

```csharp
public AppIdentity(string Title, string? Id, string Version, AppOrientation Orientation)
```

#### Parameters

`Title` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Id` [string](https://learn.microsoft.com/dotnet/api/system.string)?

`Version` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Orientation` [AppOrientation](MicroEngine.Core.AppOrientation.md)

## Fields

### <a id="MicroEngine_Core_AppIdentity_DefaultBackground"></a> DefaultBackground

The background used when nothing gives one.

```csharp
public static readonly Color DefaultBackground
```

#### Field Value

 [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Core_AppIdentity_IconResource"></a> IconResource

Name of the embedded PNG resource holding the icon.

```csharp
public const string IconResource = "MicroEngine.AppIcon.png"
```

#### Field Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Core_AppIdentity_SplashImageResource"></a> SplashImageResource

Name of the embedded PNG resource holding the start screen's image (<code>MicroEngineSplashImage</code>).

```csharp
public const string SplashImageResource = "MicroEngine.SplashImage.png"
```

#### Field Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Properties

### <a id="MicroEngine_Core_AppIdentity_Icon"></a> Icon

The game's icon, decoded from the assembly; null when the project declares none.

```csharp
public PixelImage? Icon { get; init; }
```

#### Property Value

 [PixelImage](MicroEngine.Graphics.PixelImage.md)?

### <a id="MicroEngine_Core_AppIdentity_Id"></a> Id

```csharp
public string? Id { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Core_AppIdentity_Orientation"></a> Orientation

```csharp
public AppOrientation Orientation { get; init; }
```

#### Property Value

 [AppOrientation](MicroEngine.Core.AppOrientation.md)

### <a id="MicroEngine_Core_AppIdentity_Splash"></a> Splash

Whether the game opens on its start screen (<code>MicroEngineSplash</code>, true by default): the system's launch
screen on phones and the loading screen in browsers show [SplashImage](MicroEngine.Core.AppIdentity.md#MicroEngine_Core_AppIdentity_SplashImage) on [SplashColor](MicroEngine.Core.AppIdentity.md#MicroEngine_Core_AppIdentity_SplashColor),
and the engine keeps showing it while the game loads, so no black screen comes before the first frame.

```csharp
public bool Splash { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_AppIdentity_SplashBackground"></a> SplashBackground

The start screen's background (<code>MicroEngineSplashColor</code>); null: [EdgeColor](MicroEngine.Core.AppIdentity.md#MicroEngine_Core_AppIdentity_EdgeColor_MicroEngine_Graphics_PixelImage_) of the image.

```csharp
public Color? SplashBackground { get; init; }
```

#### Property Value

 [Color](MicroEngine.Color.md)?

### <a id="MicroEngine_Core_AppIdentity_SplashBranding"></a> SplashBranding

Whether the start screen says "Made with MicroEngine" (<code>MicroEngineSplashBranding</code>, false by default).

```csharp
public bool SplashBranding { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_AppIdentity_SplashColor"></a> SplashColor

The start screen's background colour: [SplashBackground](MicroEngine.Core.AppIdentity.md#MicroEngine_Core_AppIdentity_SplashBackground), else the edge of its image, else near black.

```csharp
public Color SplashColor { get; }
```

#### Property Value

 [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Core_AppIdentity_SplashImage"></a> SplashImage

The image the start screen shows: [SplashImageOverride](MicroEngine.Core.AppIdentity.md#MicroEngine_Core_AppIdentity_SplashImageOverride), else the icon (null when there is neither).

```csharp
public PixelImage? SplashImage { get; }
```

#### Property Value

 [PixelImage](MicroEngine.Graphics.PixelImage.md)?

### <a id="MicroEngine_Core_AppIdentity_SplashImageOverride"></a> SplashImageOverride

The start screen's own image (<code>MicroEngineSplashImage</code>); null: the icon.

```csharp
public PixelImage? SplashImageOverride { get; init; }
```

#### Property Value

 [PixelImage](MicroEngine.Graphics.PixelImage.md)?

### <a id="MicroEngine_Core_AppIdentity_Title"></a> Title

```csharp
public string Title { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Core_AppIdentity_Version"></a> Version

```csharp
public string Version { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Core_AppIdentity__Clone__"></a> <Clone\>$\(\)

```csharp
public AppIdentity <Clone>$()
```

#### Returns

 [AppIdentity](MicroEngine.Core.AppIdentity.md)

### <a id="MicroEngine_Core_AppIdentity_Deconstruct_System_String__System_String__System_String__MicroEngine_Core_AppOrientation__"></a> Deconstruct\(out string, out string?, out string, out AppOrientation\)

```csharp
public void Deconstruct(out string Title, out string? Id, out string Version, out AppOrientation Orientation)
```

#### Parameters

`Title` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Id` [string](https://learn.microsoft.com/dotnet/api/system.string)?

`Version` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Orientation` [AppOrientation](MicroEngine.Core.AppOrientation.md)

### <a id="MicroEngine_Core_AppIdentity_EdgeColor_MicroEngine_Graphics_PixelImage_"></a> EdgeColor\(PixelImage\)

The average opaque colour along an image's border: a background that continues a full-bleed icon (Android's
adaptive icon, launch screens). Transparent borders give [DefaultBackground](MicroEngine.Core.AppIdentity.md#MicroEngine_Core_AppIdentity_DefaultBackground).

```csharp
public static Color EdgeColor(PixelImage image)
```

#### Parameters

`image` [PixelImage](MicroEngine.Graphics.PixelImage.md)

#### Returns

 [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Core_AppIdentity_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_AppIdentity_Equals_MicroEngine_Core_AppIdentity_"></a> Equals\(AppIdentity?\)

```csharp
public bool Equals(AppIdentity? other)
```

#### Parameters

`other` [AppIdentity](MicroEngine.Core.AppIdentity.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_AppIdentity_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_AppIdentity_Of_MicroEngine_Core_Game_"></a> Of\(Game\)

The identity of the assembly that defines <code class="paramref">game</code>'s type.

```csharp
public static AppIdentity? Of(Game game)
```

#### Parameters

`game` [Game](MicroEngine.Core.Game.md)

#### Returns

 [AppIdentity](MicroEngine.Core.AppIdentity.md)?

### <a id="MicroEngine_Core_AppIdentity_Of_System_Reflection_Assembly_"></a> Of\(Assembly\)

Reads the identity the build targets wrote into <code class="paramref">assembly</code>; null for an assembly built without
them (a test or a game that predates them), so callers keep their defaults.

```csharp
public static AppIdentity? Of(Assembly assembly)
```

#### Parameters

`assembly` [Assembly](https://learn.microsoft.com/dotnet/api/system.reflection.assembly)

#### Returns

 [AppIdentity](MicroEngine.Core.AppIdentity.md)?

### <a id="MicroEngine_Core_AppIdentity_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Core_AppIdentity_op_Equality_MicroEngine_Core_AppIdentity_MicroEngine_Core_AppIdentity_"></a> operator ==\(AppIdentity?, AppIdentity?\)

```csharp
public static bool operator ==(AppIdentity? left, AppIdentity? right)
```

#### Parameters

`left` [AppIdentity](MicroEngine.Core.AppIdentity.md)?

`right` [AppIdentity](MicroEngine.Core.AppIdentity.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_AppIdentity_op_Inequality_MicroEngine_Core_AppIdentity_MicroEngine_Core_AppIdentity_"></a> operator \!=\(AppIdentity?, AppIdentity?\)

```csharp
public static bool operator !=(AppIdentity? left, AppIdentity? right)
```

#### Parameters

`left` [AppIdentity](MicroEngine.Core.AppIdentity.md)?

`right` [AppIdentity](MicroEngine.Core.AppIdentity.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
