# Class SpriteAtlas

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

Named sprites packed into one texture, built by [Build](MicroEngine.Graphics.SpriteAtlasBuilder.md#MicroEngine_Graphics_SpriteAtlasBuilder_Build_MicroEngine_Graphics_IGraphicsDevice_System_Nullable_MicroEngine_Graphics_TextureFilter__). The atlas owns the texture:
dispose the atlas (usually in <code>Game.Unload</code>) when its sprites are no longer drawn.

```csharp
public sealed class SpriteAtlas : IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SpriteAtlas](MicroEngine.Graphics.SpriteAtlas.md)

#### Implements

[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Fields

### <a id="MicroEngine_Graphics_SpriteAtlas_DefaultFrameDuration"></a> DefaultFrameDuration

The duration of tagged frames whose file gives none, in seconds (a tenth of a second).

```csharp
public const float DefaultFrameDuration = 0.1
```

#### Field Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Properties

### <a id="MicroEngine_Graphics_SpriteAtlas_Animations"></a> Animations

Names of the animations the atlas's data gives (Aseprite frame tags, the clips of a sheet description), in its order.

```csharp
public IReadOnlyCollection<string> Animations { get; }
```

#### Property Value

 [IReadOnlyCollection](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlycollection\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Graphics_SpriteAtlas_Names"></a> Names

Names of every sprite in the atlas.

```csharp
public IReadOnlyCollection<string> Names { get; }
```

#### Property Value

 [IReadOnlyCollection](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlycollection\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Graphics_SpriteAtlas_Texture"></a> Texture

The packed texture shared by every sprite of the atlas.

```csharp
public Texture Texture { get; }
```

#### Property Value

 [Texture](MicroEngine.Graphics.Texture.md)

### <a id="MicroEngine_Graphics_SpriteAtlas_Item_System_String_"></a> this\[string\]

The sprite named <code class="paramref">name</code>.

```csharp
public Sprite this[string name] { get; }
```

#### Property Value

 [Sprite](MicroEngine.Graphics.Sprite.md)

#### Exceptions

 [KeyNotFoundException](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)

No sprite has that name; check with [Contains](MicroEngine.Graphics.SpriteAtlas.md#MicroEngine_Graphics_SpriteAtlas_Contains_System_String_).

### <a id="MicroEngine_Graphics_SpriteAtlas_Item_System_ReadOnlySpan_System_Char__"></a> this\[ReadOnlySpan<char\>\]

The sprite named by text that is not a string: found without making one, so a name built from numbers costs
nothing when it is looked up every frame.

```csharp
public Sprite this[ReadOnlySpan<char> name] { get; }
```

#### Property Value

 [Sprite](MicroEngine.Graphics.Sprite.md)

#### Exceptions

 [KeyNotFoundException](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)

No sprite has that name.

### <a id="MicroEngine_Graphics_SpriteAtlas_Item_MicroEngine_Graphics_InterpolatedText_"></a> this\[InterpolatedText\]

The sprite named by an interpolated string (<code>atlas[$"water{mask}_{frame}"]</code>), which is written into a reused
buffer instead of a string.

```csharp
public Sprite this[InterpolatedText name] { get; }
```

#### Property Value

 [Sprite](MicroEngine.Graphics.Sprite.md)

#### Exceptions

 [KeyNotFoundException](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)

No sprite has that name.

## Methods

### <a id="MicroEngine_Graphics_SpriteAtlas_Clip_System_String_System_Nullable_System_Boolean__"></a> Clip\(string, bool?\)

The clip named <code class="paramref">name</code> in the atlas's data (an Aseprite frame tag, a clip of a sheet description),
with its timing (a duration per frame or a rate) and its direction (forward, reverse, ping-pong) applied. Built
once per name and loop setting, then shared.

```csharp
public AnimationClip Clip(string name, bool? loop = null)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

The clip's name ([Animations](MicroEngine.Graphics.SpriteAtlas.md#MicroEngine_Graphics_SpriteAtlas_Animations)).

`loop` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)?

Whether it repeats; null for what the data says (a tag loops unless it says it plays a number of times).

#### Returns

 [AnimationClip](MicroEngine.Graphics.AnimationClip.md)

#### Exceptions

 [KeyNotFoundException](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)

The atlas has no animation of that name.

### <a id="MicroEngine_Graphics_SpriteAtlas_Clip_System_String_System_Single_System_Boolean_"></a> Clip\(string, float, bool\)

A clip of the numbered frames "name0", "name1"... (or "name1", "name2"... when the numbering starts at 1, as many
packs do) at <code class="paramref">framesPerSecond</code>.

```csharp
public AnimationClip Clip(string name, float framesPerSecond, bool loop = true)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`framesPerSecond` [float](https://learn.microsoft.com/dotnet/api/system.single)

`loop` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Returns

 [AnimationClip](MicroEngine.Graphics.AnimationClip.md)

#### Exceptions

 [KeyNotFoundException](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)

No frames have that name.

### <a id="MicroEngine_Graphics_SpriteAtlas_Contains_System_String_"></a> Contains\(string\)

Whether a sprite named <code class="paramref">name</code> exists.

```csharp
public bool Contains(string name)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_SpriteAtlas_Create_MicroEngine_Graphics_Texture_MicroEngine_Graphics_AtlasData_System_Boolean_"></a> Create\(Texture, AtlasData, bool\)

Makes an atlas from <code class="paramref">texture</code> and the frames a packing tool described in <code class="paramref">data</code>.

```csharp
public static SpriteAtlas Create(Texture texture, AtlasData data, bool ownsTexture = true)
```

#### Parameters

`texture` [Texture](MicroEngine.Graphics.Texture.md)

The atlas image as a texture.

`data` [AtlasData](MicroEngine.Graphics.AtlasData.md)

The frames and tags ([Parse](MicroEngine.Graphics.AtlasData.md#MicroEngine_Graphics_AtlasData_Parse_System_String_MicroEngine_Graphics_AtlasOptions_)).

`ownsTexture` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether disposing the atlas disposes the texture (true by default).

#### Returns

 [SpriteAtlas](MicroEngine.Graphics.SpriteAtlas.md)

#### Exceptions

 [InvalidDataException](https://learn.microsoft.com/dotnet/api/system.io.invaliddataexception)

A frame lies outside the texture, or two frames share a name.

### <a id="MicroEngine_Graphics_SpriteAtlas_CreateAnimator_System_String_"></a> CreateAnimator\(string?\)

An [Animator](MicroEngine.Graphics.Animator.md) holding every animation of the atlas under its name ([Animations](MicroEngine.Graphics.SpriteAtlas.md#MicroEngine_Graphics_SpriteAtlas_Animations)), playing
<code class="paramref">play</code> or the first one: a character's animations with no clip built by hand.

```csharp
public Animator CreateAnimator(string? play = null)
```

#### Parameters

`play` [string](https://learn.microsoft.com/dotnet/api/system.string)?

The clip to start with; null for the first of the data.

#### Returns

 [Animator](MicroEngine.Graphics.Animator.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The atlas has no animation.

 [KeyNotFoundException](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)

The atlas has no animation named <code class="paramref">play</code>.

### <a id="MicroEngine_Graphics_SpriteAtlas_Dispose"></a> Dispose\(\)

Disposes the texture (unless the atlas was created with <code>ownsTexture: false</code>); sprites of this atlas must not be drawn afterwards.

```csharp
public void Dispose()
```

### <a id="MicroEngine_Graphics_SpriteAtlas_Frames_System_String_"></a> Frames\(string\)

The "name0".."nameN" frames added through [AddFrames](MicroEngine.Graphics.SpriteAtlasBuilder.md#MicroEngine_Graphics_SpriteAtlasBuilder_AddFrames_System_String_System_Collections_Generic_IEnumerable_MicroEngine_Graphics_PixelImage__System_Nullable_System_Numerics_Vector2__), in order; a numbering that
starts at 1 ("walk1", "walk2"... as in many packs) works too. Collected once per name and shared, read-only: it can
be called every frame.

```csharp
public IReadOnlyList<Sprite> Frames(string name)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[Sprite](MicroEngine.Graphics.Sprite.md)\>

#### Exceptions

 [KeyNotFoundException](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)

No sprite is named <code class="paramref">name</code> followed by 0 or 1.

### <a id="MicroEngine_Graphics_SpriteAtlas_Load_MicroEngine_Graphics_IGraphicsDevice_System_String_MicroEngine_Graphics_AtlasOptions_System_Nullable_MicroEngine_Graphics_TextureFilter__System_String_"></a> Load\(IGraphicsDevice, string, AtlasOptions?, TextureFilter?, string?\)

Loads an atlas made by a packing tool: its JSON or XML file and the image it names (next to the file), or
<code class="paramref">imagePath</code>. The atlas owns the texture.

```csharp
public static SpriteAtlas Load(IGraphicsDevice device, string path, AtlasOptions? options = null, TextureFilter? filter = null, string? imagePath = null)
```

#### Parameters

`device` [IGraphicsDevice](MicroEngine.Graphics.IGraphicsDevice.md)

Creates the texture.

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

The atlas file (TexturePacker or Aseprite JSON, Starling XML).

`options` [AtlasOptions](MicroEngine.Graphics.AtlasOptions.md)?

Default pivot and naming; null for [Default](MicroEngine.Graphics.AtlasOptions.md#MicroEngine_Graphics_AtlasOptions_Default).

`filter` [TextureFilter](MicroEngine.Graphics.TextureFilter.md)?

The texture's filter; null for the device's default.

`imagePath` [string](https://learn.microsoft.com/dotnet/api/system.string)?

The image, when the file names none or names it wrongly; null to use the file's.

#### Returns

 [SpriteAtlas](MicroEngine.Graphics.SpriteAtlas.md)

#### Exceptions

 [FileNotFoundException](https://learn.microsoft.com/dotnet/api/system.io.filenotfoundexception)

The atlas file or its image does not exist.

 [InvalidDataException](https://learn.microsoft.com/dotnet/api/system.io.invaliddataexception)

The file is not an atlas, or does not fit its image; the message names the file.

### <a id="MicroEngine_Graphics_SpriteAtlas_TryGet_System_ReadOnlySpan_System_Char__MicroEngine_Graphics_Sprite__"></a> TryGet\(ReadOnlySpan<char\>, out Sprite\)

The sprite named <code class="paramref">name</code>, or false when the atlas has none: no exception, no string.

```csharp
public bool TryGet(ReadOnlySpan<char> name, out Sprite sprite)
```

#### Parameters

`name` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[char](https://learn.microsoft.com/dotnet/api/system.char)\>

`sprite` [Sprite](MicroEngine.Graphics.Sprite.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
