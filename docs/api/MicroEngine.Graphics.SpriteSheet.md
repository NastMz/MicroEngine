# Class SpriteSheet

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

Frames and clips read from a description of sprite sheets, as pixels: what an artist's tool or a <code>.sheet</code> file
says about one or more images (cell size, named strips, pivots, animation timing), ready to be packed into a
[SpriteAtlas](MicroEngine.Graphics.SpriteAtlas.md) with [AddSheet](MicroEngine.Graphics.SpriteAtlasBuilder.md#MicroEngine_Graphics_SpriteAtlasBuilder_AddSheet_MicroEngine_Graphics_SpriteSheet_). <code>Assets.Atlas("art/enemies.sheet")</code>
does both steps; <code>Assets.Sheet(path)</code> gives the frames to change or to pack with others.

```csharp
public sealed class SpriteSheet
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SpriteSheet](MicroEngine.Graphics.SpriteSheet.md)

## Remarks

<p>A <code>.sheet</code> file is JSON (comments and trailing commas allowed):</p>

```csharp
{
  "pivot": [0.5, 1],                                  // anchor of every frame, as a fraction of it (or "origin": [x, y] in pixels)
  "sheets": [
    { "image": "drone.png", "name": "drone", "cell": [55, 52], "origin": [27, 26], "fps": 12 },
    { "image": "palm.png", "name": "prop_palm" },     // no cell: the whole image is one sprite
    { "image": "tiles.png", "cell": [16, 16], "margin": 0, "spacing": 0,
      "strips": { "walk": { "from": 8, "count": 6, "durations": [100, 100, 200, 100, 100, 200], "direction": "pingpong", "loop": false },
                  "ledge": { "at": [6, 11], "count": 4 },
                  "fill": { "cells": [[4, 1], [1, 1]] },
                  "odd": { "rects": [[504, 80, 16, 16]] } },
      "sprites": { "gem": [2, 3], "logo": [0, 64, 48, 16], "key": { "cell": [3, 1], "origin": [8, 15] } } }
  ],
  "clips": { "hop": { "frames": ["drone1", "drone3", "drone1"], "fps": 6, "loop": false },
             "patrol": { "strip": "drone", "fps": 8 } }
}
```

<p>
An image with a <code>name</code> and a <code>cell</code> is one strip: frames "name0", "name1"... row by row, without the fully
transparent cells at its end unless <code>count</code> says how many. <code>strips</code> name runs of cells of one image
(<code>from</code> a cell index or <code>at</code> a column and row, <code>cells</code>, or <code>rects</code> in pixels); <code>sprites</code> name
single cells (two numbers) or rectangles (four). A strip with <code>fps</code> or <code>durations</code> (milliseconds per frame)
is also a clip of that name; <code>clips</code> name more of them, from frames listed by name or a whole strip.
<code>direction</code> is forward, reverse, pingpong or pingpong_reverse. Image paths are relative to the file.
</p>

## Constructors

### <a id="MicroEngine_Graphics_SpriteSheet__ctor_System_Collections_Generic_IEnumerable_MicroEngine_Graphics_SheetFrame__System_Collections_Generic_IEnumerable_MicroEngine_Graphics_SheetClip__"></a> SpriteSheet\(IEnumerable<SheetFrame\>, IEnumerable<SheetClip\>?\)

A sheet of <code class="paramref">frames</code> and <code class="paramref">clips</code> (a reader of another format makes one).

```csharp
public SpriteSheet(IEnumerable<SheetFrame> frames, IEnumerable<SheetClip>? clips = null)
```

#### Parameters

`frames` [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[SheetFrame](MicroEngine.Graphics.SheetFrame.md)\>

`clips` [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[SheetClip](MicroEngine.Graphics.SheetClip.md)\>?

#### Exceptions

 [InvalidDataException](https://learn.microsoft.com/dotnet/api/system.io.invaliddataexception)

Two frames or two clips share a name, a clip names a frame that is missing, or its timing does not fit its frames.

## Properties

### <a id="MicroEngine_Graphics_SpriteSheet_Clips"></a> Clips

The clips, in the description's order.

```csharp
public IReadOnlyList<SheetClip> Clips { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[SheetClip](MicroEngine.Graphics.SheetClip.md)\>

### <a id="MicroEngine_Graphics_SpriteSheet_Frames"></a> Frames

The frames, in the description's order.

```csharp
public IReadOnlyList<SheetFrame> Frames { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[SheetFrame](MicroEngine.Graphics.SheetFrame.md)\>

## Methods

### <a id="MicroEngine_Graphics_SpriteSheet_Load_System_String_MicroEngine_Graphics_AtlasOptions_"></a> Load\(string, AtlasOptions?\)

Reads a <code>.sheet</code> file and the images it names, next to it.

```csharp
public static SpriteSheet Load(string path, AtlasOptions? options = null)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

`options` [AtlasOptions](MicroEngine.Graphics.AtlasOptions.md)?

#### Returns

 [SpriteSheet](MicroEngine.Graphics.SpriteSheet.md)

#### Exceptions

 [FileNotFoundException](https://learn.microsoft.com/dotnet/api/system.io.filenotfoundexception)

The file does not exist.

 [InvalidDataException](https://learn.microsoft.com/dotnet/api/system.io.invaliddataexception)

The description is wrong or an image is missing; the message names the file.

### <a id="MicroEngine_Graphics_SpriteSheet_Parse_System_String_System_Func_System_String_MicroEngine_Graphics_PixelImage__MicroEngine_Graphics_AtlasOptions_"></a> Parse\(string, Func<string, PixelImage\>, AtlasOptions?\)

Reads the text of a <code>.sheet</code> file.

```csharp
public static SpriteSheet Parse(string text, Func<string, PixelImage> image, AtlasOptions? options = null)
```

#### Parameters

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

The file's text.

`image` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [PixelImage](MicroEngine.Graphics.PixelImage.md)\>

Gives the image the description names (a path relative to the file), each asked for once.

`options` [AtlasOptions](MicroEngine.Graphics.AtlasOptions.md)?

The pivot of frames the description gives none; null for [Default](MicroEngine.Graphics.AtlasOptions.md#MicroEngine_Graphics_AtlasOptions_Default).

#### Returns

 [SpriteSheet](MicroEngine.Graphics.SpriteSheet.md)

#### Exceptions

 [InvalidDataException](https://learn.microsoft.com/dotnet/api/system.io.invaliddataexception)

The text is not a sheet description, names an image <code class="paramref">image</code> cannot give, or places a frame outside its image; the message says which.

</div>
