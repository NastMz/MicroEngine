# Class SpriteAtlas

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.dll  
Guide: [Graphics](../manual/rendering.md)  

Named sprites packed into one texture, built by [Build](MicroEngine.Graphics.SpriteAtlasBuilder.md#MicroEngine_Graphics_SpriteAtlasBuilder_Build_MicroEngine_Graphics_IGraphicsDevice_). The atlas owns the texture:
dispose the atlas (usually in <code>Game.Unload</code>) when its sprites are no longer drawn.

```csharp
public sealed class SpriteAtlas : IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SpriteAtlas](MicroEngine.Graphics.SpriteAtlas.md)

#### Implements

[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Properties

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

## Methods

### <a id="MicroEngine_Graphics_SpriteAtlas_Contains_System_String_"></a> Contains\(string\)

Whether a sprite named <code class="paramref">name</code> exists.

```csharp
public bool Contains(string name)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_SpriteAtlas_Dispose"></a> Dispose\(\)

Disposes the texture; sprites of this atlas must not be drawn afterwards.

```csharp
public void Dispose()
```

### <a id="MicroEngine_Graphics_SpriteAtlas_Frames_System_String_"></a> Frames\(string\)

Collects "name0".."nameN" frames added through [AddFrames](MicroEngine.Graphics.SpriteAtlasBuilder.md#MicroEngine_Graphics_SpriteAtlasBuilder_AddFrames_System_String_System_Collections_Generic_IEnumerable_MicroEngine_Graphics_PixelImage__System_Nullable_System_Numerics_Vector2__).

```csharp
public Sprite[] Frames(string name)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [Sprite](MicroEngine.Graphics.Sprite.md)\[\]

</div>
