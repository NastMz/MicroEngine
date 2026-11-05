# Class FontOptions

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Text.dll  
Guide: [Graphics](../manual/rendering.md)  

How a [ScalableFont](MicroEngine.Graphics.ScalableFont.md) turns outlines into pixels.

```csharp
public sealed record FontOptions : IEquatable<FontOptions>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[FontOptions](MicroEngine.Graphics.FontOptions.md)

#### Implements

[IEquatable<FontOptions\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_FontOptions__ctor_System_Boolean_System_Nullable_MicroEngine_Graphics_TextureFilter__System_Int32_System_Boolean_System_Boolean_"></a> FontOptions\(bool, TextureFilter?, int, bool, bool\)

How a [ScalableFont](MicroEngine.Graphics.ScalableFont.md) turns outlines into pixels.

```csharp
public FontOptions(bool Antialiasing = true, TextureFilter? Filter = null, int PageSize = 512, bool Kerning = true, bool SnapToPixels = true)
```

#### Parameters

`Antialiasing` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Smooth glyph edges (true, the default: HD text). False gives hard edges, for pixel fonts distributed as TTF and drawn at
their design size or whole multiples of it.

`Filter` TextureFilter?

How glyph pages are sampled when text is scaled or rotated; null follows the game's default
([DefaultFilter](MicroEngine.Graphics.IGraphicsDevice.md#MicroEngine_Graphics_IGraphicsDevice_DefaultFilter), from its presentation's default sampler), whether the text is
antialiased or not.

`PageSize` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Width and height of each glyph page texture: 512 by default (1 MB), enough for the printable ASCII of a few sizes;
another page is added when one fills up.

`Kerning` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Applies the font's kerning pairs (true by default).

`SnapToPixels` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Places each glyph on whole canvas pixels (true by default) so unscaled text stays sharp; false keeps fractional
positions, smoother for slowly moving text.

## Properties

### <a id="MicroEngine_Graphics_FontOptions_Antialiasing"></a> Antialiasing

Smooth glyph edges (true, the default: HD text). False gives hard edges, for pixel fonts distributed as TTF and drawn at
their design size or whole multiples of it.

```csharp
public bool Antialiasing { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_FontOptions_Default"></a> Default

The default: antialiased, sampled with the game's default filter, kerned, snapped to pixels.

```csharp
public static FontOptions Default { get; }
```

#### Property Value

 [FontOptions](MicroEngine.Graphics.FontOptions.md)

### <a id="MicroEngine_Graphics_FontOptions_Filter"></a> Filter

How glyph pages are sampled when text is scaled or rotated; null follows the game's default
([DefaultFilter](MicroEngine.Graphics.IGraphicsDevice.md#MicroEngine_Graphics_IGraphicsDevice_DefaultFilter), from its presentation's default sampler), whether the text is
antialiased or not.

```csharp
public TextureFilter? Filter { get; init; }
```

#### Property Value

 TextureFilter?

### <a id="MicroEngine_Graphics_FontOptions_Kerning"></a> Kerning

Applies the font's kerning pairs (true by default).

```csharp
public bool Kerning { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_FontOptions_PageSize"></a> PageSize

Width and height of each glyph page texture: 512 by default (1 MB), enough for the printable ASCII of a few sizes;
another page is added when one fills up.

```csharp
public int PageSize { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_FontOptions_Pixel"></a> Pixel

For pixel fonts in TTF/OTF files: no antialiasing, nearest sampling whatever the game's default.

```csharp
public static FontOptions Pixel { get; }
```

#### Property Value

 [FontOptions](MicroEngine.Graphics.FontOptions.md)

### <a id="MicroEngine_Graphics_FontOptions_SnapToPixels"></a> SnapToPixels

Places each glyph on whole canvas pixels (true by default) so unscaled text stays sharp; false keeps fractional
positions, smoother for slowly moving text.

```csharp
public bool SnapToPixels { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="MicroEngine_Graphics_FontOptions__Clone__"></a> <Clone\>$\(\)

```csharp
public FontOptions <Clone>$()
```

#### Returns

 [FontOptions](MicroEngine.Graphics.FontOptions.md)

### <a id="MicroEngine_Graphics_FontOptions_Deconstruct_System_Boolean__System_Nullable_MicroEngine_Graphics_TextureFilter___System_Int32__System_Boolean__System_Boolean__"></a> Deconstruct\(out bool, out TextureFilter?, out int, out bool, out bool\)

```csharp
public void Deconstruct(out bool Antialiasing, out TextureFilter? Filter, out int PageSize, out bool Kerning, out bool SnapToPixels)
```

#### Parameters

`Antialiasing` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`Filter` TextureFilter?

`PageSize` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Kerning` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`SnapToPixels` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_FontOptions_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_FontOptions_Equals_MicroEngine_Graphics_FontOptions_"></a> Equals\(FontOptions?\)

```csharp
public bool Equals(FontOptions? other)
```

#### Parameters

`other` [FontOptions](MicroEngine.Graphics.FontOptions.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_FontOptions_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_FontOptions_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_FontOptions_op_Equality_MicroEngine_Graphics_FontOptions_MicroEngine_Graphics_FontOptions_"></a> operator ==\(FontOptions?, FontOptions?\)

```csharp
public static bool operator ==(FontOptions? left, FontOptions? right)
```

#### Parameters

`left` [FontOptions](MicroEngine.Graphics.FontOptions.md)?

`right` [FontOptions](MicroEngine.Graphics.FontOptions.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_FontOptions_op_Inequality_MicroEngine_Graphics_FontOptions_MicroEngine_Graphics_FontOptions_"></a> operator \!=\(FontOptions?, FontOptions?\)

```csharp
public static bool operator !=(FontOptions? left, FontOptions? right)
```

#### Parameters

`left` [FontOptions](MicroEngine.Graphics.FontOptions.md)?

`right` [FontOptions](MicroEngine.Graphics.FontOptions.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
