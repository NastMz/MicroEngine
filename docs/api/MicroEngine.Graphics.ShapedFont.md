# Class ShapedFont

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Text.Shaping.dll  
Guide: [Graphics](../manual/rendering.md)  

A TrueType or OpenType font that shapes its text: letters take the forms their neighbours call for (Arabic's joined
forms, Indic conjuncts, ligatures), marks sit where the font places them, and right-to-left scripts (Arabic, Hebrew)
run right to left. HarfBuzz chooses the glyphs and their positions; each glyph is rasterized the first time it is
drawn at a size and kept in glyph pages (textures), so later frames only draw quads.

```csharp
public sealed class ShapedFont : IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ShapedFont](MicroEngine.Graphics.ShapedFont.md)

#### Implements

[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Examples

```csharp
var font = ShapedFont.FromBytes(Graphics, Assets.Bytes("fonts/Inter-Regular.ttf"))
    .AddFallback(Assets.Bytes("fonts/NotoSansArabic-Regular.ttf"));
font.At(32).Draw(renderer, "مرحبا", new Vector2(24, 24), Color.White);
```

## Remarks

<p>
Use it in place of a [ScalableFont](MicroEngine.Graphics.ScalableFont.md) for text in a script that needs shaping; [At](MicroEngine.Graphics.ShapedFont.md#MicroEngine_Graphics_ShapedFont_At_System_Single_) gives an
[IFont](MicroEngine.Graphics.IFont.md), so widgets, layout and wrapping work with it unchanged. Faces added with
[AddFallback](MicroEngine.Graphics.ShapedFont.md#MicroEngine_Graphics_ShapedFont_AddFallback_System_Byte___System_String_) supply the letters the main face lacks, a run of text at a time, so a Latin face with an
Arabic and a Hebrew one after it draws all three. Letters no face has draw as the last face's missing-glyph box.
</p>
<p>
Lines that mix directions follow Unicode's bidirectional algorithm (UAX #9): a line reads in the direction of its
first letter that has one ([IsRightToLeft](MicroEngine.Graphics.ShapedFontSize.md#MicroEngine_Graphics_ShapedFontSize_IsRightToLeft_System_String_)), and runs of the other direction inside it,
a name or a number in an Arabic sentence, read in theirs. Explicit direction marks (embeddings, overrides, isolates)
are ignored and brackets are not paired across directions.
</p>

## Properties

### <a id="MicroEngine_Graphics_ShapedFont_Faces"></a> Faces

How many faces the font has: the main one, then its fallbacks in the order they were added.

```csharp
public int Faces { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_ShapedFont_Filter"></a> Filter

How the glyph pages are sampled; applies to existing and future pages.

```csharp
public TextureFilter Filter { get; set; }
```

#### Property Value

 [TextureFilter](MicroEngine.Graphics.TextureFilter.md)

### <a id="MicroEngine_Graphics_ShapedFont_GlyphsRasterized"></a> GlyphsRasterized

Glyphs rasterized so far, across every size (a measure of font work done).

```csharp
public int GlyphsRasterized { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_ShapedFont_Name"></a> Name

The file name (or the name given), for messages.

```csharp
public string Name { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Graphics_ShapedFont_Options"></a> Options

The options the font was opened with ([Kerning](MicroEngine.Graphics.FontOptions.md#MicroEngine_Graphics_FontOptions_Kerning) does not apply: the font's own positioning always does).

```csharp
public FontOptions Options { get; }
```

#### Property Value

 [FontOptions](MicroEngine.Graphics.FontOptions.md)

### <a id="MicroEngine_Graphics_ShapedFont_Pages"></a> Pages

Glyph pages created so far (each [PageSize](MicroEngine.Graphics.FontOptions.md#MicroEngine_Graphics_FontOptions_PageSize) squared, 4 bytes per pixel).

```csharp
public int Pages { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_ShapedFont_TextureBytes"></a> TextureBytes

Device memory of the glyph pages, in bytes.

```csharp
public long TextureBytes { get; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

## Methods

### <a id="MicroEngine_Graphics_ShapedFont_AddFallback_System_Byte___System_String_"></a> AddFallback\(byte\[\], string?\)

Adds another TTF/OTF face for the letters the faces before it lack (another script, symbols). Text already
shaped is shaped again, so add fallbacks while loading.

```csharp
public ShapedFont AddFallback(byte[] data, string? name = null)
```

#### Parameters

`data` [byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\]

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Returns

 [ShapedFont](MicroEngine.Graphics.ShapedFont.md)

#### Exceptions

 [InvalidDataException](https://learn.microsoft.com/dotnet/api/system.io.invaliddataexception)

The data is not a font the engine reads.

### <a id="MicroEngine_Graphics_ShapedFont_At_System_Single_"></a> At\(float\)

The font at <code class="paramref">size</code> (the same size as a [ScalableFont](MicroEngine.Graphics.ScalableFont.md)'s), as an [IFont](MicroEngine.Graphics.IFont.md); the same object for the same size.

```csharp
public ShapedFontSize At(float size)
```

#### Parameters

`size` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [ShapedFontSize](MicroEngine.Graphics.ShapedFontSize.md)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">size</code> is not positive.

### <a id="MicroEngine_Graphics_ShapedFont_Dispose"></a> Dispose\(\)

Releases the glyph pages and the faces. Text cannot be drawn afterwards.

```csharp
public void Dispose()
```

### <a id="MicroEngine_Graphics_ShapedFont_FromBytes_MicroEngine_Graphics_IGraphicsDevice_System_Byte___MicroEngine_Graphics_FontOptions_System_String_"></a> FromBytes\(IGraphicsDevice, byte\[\], FontOptions?, string?\)

Opens a font from the bytes of a TTF or OTF file.

```csharp
public static ShapedFont FromBytes(IGraphicsDevice device, byte[] data, FontOptions? options = null, string? name = null)
```

#### Parameters

`device` [IGraphicsDevice](MicroEngine.Graphics.IGraphicsDevice.md)

Creates the glyph pages.

`data` [byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\]

The whole font file.

`options` [FontOptions](MicroEngine.Graphics.FontOptions.md)?

Rasterization and sampling; null for [Default](MicroEngine.Graphics.FontOptions.md#MicroEngine_Graphics_FontOptions_Default).

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)?

A name for messages and diagnostics.

#### Returns

 [ShapedFont](MicroEngine.Graphics.ShapedFont.md)

#### Exceptions

 [InvalidDataException](https://learn.microsoft.com/dotnet/api/system.io.invaliddataexception)

The data is not a font the engine reads.

### <a id="MicroEngine_Graphics_ShapedFont_Load_MicroEngine_Graphics_IGraphicsDevice_System_String_MicroEngine_Graphics_FontOptions_"></a> Load\(IGraphicsDevice, string, FontOptions?\)

Opens a TTF or OTF file.

```csharp
public static ShapedFont Load(IGraphicsDevice device, string path, FontOptions? options = null)
```

#### Parameters

`device` [IGraphicsDevice](MicroEngine.Graphics.IGraphicsDevice.md)

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

`options` [FontOptions](MicroEngine.Graphics.FontOptions.md)?

#### Returns

 [ShapedFont](MicroEngine.Graphics.ShapedFont.md)

#### Exceptions

 [FileNotFoundException](https://learn.microsoft.com/dotnet/api/system.io.filenotfoundexception)

The file does not exist.

 [InvalidDataException](https://learn.microsoft.com/dotnet/api/system.io.invaliddataexception)

The file is not a font the engine reads; the message names it.

### <a id="MicroEngine_Graphics_ShapedFont_Reset"></a> Reset\(\)

Drops every rasterized glyph and its pages (freeing their memory); glyphs drawn later are rasterized again.

```csharp
public void Reset()
```

</div>
