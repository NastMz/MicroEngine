# Class AtlasData

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

The description of an atlas image written by a packing tool, read without any graphics device: the JSON of
TexturePacker, Free Texture Packer and Aseprite (hash or array of frames; Aseprite's frame durations and tags), and the
Starling/Sparrow XML of TexturePacker, ShoeBox and Kenney's packs. Trimmed frames keep their place in the untrimmed
frame; rotated frames are refused (turn rotation off in the tool).

```csharp
public sealed class AtlasData
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AtlasData](MicroEngine.Graphics.AtlasData.md)

## Properties

### <a id="MicroEngine_Graphics_AtlasData_Animations"></a> Animations

The frame tags (Aseprite), in the file's order; empty for formats without tags.

```csharp
public IReadOnlyList<AtlasAnimation> Animations { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[AtlasAnimation](MicroEngine.Graphics.AtlasAnimation.md)\>

### <a id="MicroEngine_Graphics_AtlasData_ImagePath"></a> ImagePath

The atlas image as the file names it (relative to the file), or null when it names none.

```csharp
public string? ImagePath { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Graphics_AtlasData_Regions"></a> Regions

The frames, in the file's order.

```csharp
public IReadOnlyList<AtlasRegion> Regions { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[AtlasRegion](MicroEngine.Graphics.AtlasRegion.md)\>

## Methods

### <a id="MicroEngine_Graphics_AtlasData_Load_System_String_MicroEngine_Graphics_AtlasOptions_"></a> Load\(string, AtlasOptions?\)

Reads an atlas file (JSON or XML).

```csharp
public static AtlasData Load(string path, AtlasOptions? options = null)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

`options` [AtlasOptions](MicroEngine.Graphics.AtlasOptions.md)?

#### Returns

 [AtlasData](MicroEngine.Graphics.AtlasData.md)

#### Exceptions

 [InvalidDataException](https://learn.microsoft.com/dotnet/api/system.io.invaliddataexception)

The file is not an atlas in a known format; the message names it.

### <a id="MicroEngine_Graphics_AtlasData_Parse_System_String_MicroEngine_Graphics_AtlasOptions_"></a> Parse\(string, AtlasOptions?\)

Reads atlas JSON or XML, recognised by its first character.

```csharp
public static AtlasData Parse(string text, AtlasOptions? options = null)
```

#### Parameters

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

The file's text.

`options` [AtlasOptions](MicroEngine.Graphics.AtlasOptions.md)?

Pivot and naming; null for [Default](MicroEngine.Graphics.AtlasOptions.md#MicroEngine_Graphics_AtlasOptions_Default).

#### Returns

 [AtlasData](MicroEngine.Graphics.AtlasData.md)

#### Exceptions

 [InvalidDataException](https://learn.microsoft.com/dotnet/api/system.io.invaliddataexception)

The text is not an atlas in a known format; the message says what is wrong.

</div>
