# Class TiledSprites

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Tiled](MicroEngine.Tiled.md)  
Assembly: MicroEngine.Tiled.dll  
Guide: [Maps from Tiled](../manual/world.md#maps-from-tiled)  

The sprites of a [TiledMap](MicroEngine.Tiled.TiledMap.md)'s tiles: each tileset image loaded as a texture through the game's assets
(shared and released with them), and a draw call for tile layers.

```csharp
public sealed class TiledSprites
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[TiledSprites](MicroEngine.Tiled.TiledSprites.md)

## Constructors

### <a id="MicroEngine_Tiled_TiledSprites__ctor_MicroEngine_Tiled_TiledMap_MicroEngine_Core_AssetCache_System_Nullable_MicroEngine_Graphics_TextureFilter__"></a> TiledSprites\(TiledMap, AssetCache, TextureFilter?\)

Loads every tileset image of <code class="paramref">map</code> through <code class="paramref">assets</code>.

```csharp
public TiledSprites(TiledMap map, AssetCache assets, TextureFilter? filter = null)
```

#### Parameters

`map` [TiledMap](MicroEngine.Tiled.TiledMap.md)

The map.

`assets` [AssetCache](MicroEngine.Core.AssetCache.md)

The game's assets (<code>Game.Assets</code>) or the scope the level's textures belong to.

`filter` [TextureFilter](MicroEngine.Graphics.TextureFilter.md)?

The textures' sampling; null for the assets' default (the game's presentation).

#### Exceptions

 [AssetLoadException](MicroEngine.Core.AssetLoadException.md)

A tileset image is missing or cannot be decoded.

## Properties

### <a id="MicroEngine_Tiled_TiledSprites_Map"></a> Map

The map the sprites are for.

```csharp
public TiledMap Map { get; }
```

#### Property Value

 [TiledMap](MicroEngine.Tiled.TiledMap.md)

### <a id="MicroEngine_Tiled_TiledSprites_Item_System_Int32_"></a> this\[int\]

The sprite of the tile with global id <code class="paramref">gid</code>, or null (0, unknown ids and tiles without an image).

```csharp
public Sprite? this[int gid] { get; }
```

#### Property Value

 [Sprite](MicroEngine.Graphics.Sprite.md)?

## Methods

### <a id="MicroEngine_Tiled_TiledSprites_At_System_Int32_System_Double_"></a> At\(int, double\)

The sprite of <code class="paramref">gid</code> <code class="paramref">seconds</code> into its animation (its own sprite when it does not animate).

```csharp
public Sprite? At(int gid, double seconds)
```

#### Parameters

`gid` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`seconds` [double](https://learn.microsoft.com/dotnet/api/system.double)

#### Returns

 [Sprite](MicroEngine.Graphics.Sprite.md)?

### <a id="MicroEngine_Tiled_TiledSprites_Draw_MicroEngine_Graphics_IRenderer_MicroEngine_Tiled_TiledTileLayer_MicroEngine_Graphics_Camera2D_System_Double_System_Numerics_Vector2_"></a> Draw\(IRenderer, TiledTileLayer, Camera2D?, double, Vector2\)

Draws the cells of <code class="paramref">layer</code> the camera shows, each tile turned as the map says and animated at
<code class="paramref">seconds</code>, at the layer's offset, with its opacity and tint. A hidden layer draws nothing;
parallax is not applied.

```csharp
public void Draw(IRenderer renderer, TiledTileLayer layer, Camera2D? camera = null, double seconds = 0, Vector2 shift = default)
```

#### Parameters

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

The renderer; push <code class="paramref">camera</code> on it first when there is one.

`layer` [TiledTileLayer](MicroEngine.Tiled.TiledTileLayer.md)

A tile layer of [Map](MicroEngine.Tiled.TiledSprites.md#MicroEngine_Tiled_TiledSprites_Map).

`camera` [Camera2D](MicroEngine.Graphics.Camera2D.md)?

The camera, used to skip what it does not show; null draws over the screen area.

`seconds` [double](https://learn.microsoft.com/dotnet/api/system.double)

The time animations are at (for example the scene's elapsed time).

`shift` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

Pixels added to the layer's offset, for example its parallax ([ParallaxShift](MicroEngine.Tiled.TiledMap.md#MicroEngine_Tiled_TiledMap_ParallaxShift_MicroEngine_Tiled_TiledLayer_System_Numerics_Vector2_)).

### <a id="MicroEngine_Tiled_TiledSprites_Draw_MicroEngine_Graphics_IRenderer_MicroEngine_Tiled_TiledObjectLayer_MicroEngine_Graphics_Camera2D_System_Double_System_Numerics_Vector2_"></a> Draw\(IRenderer, TiledObjectLayer, Camera2D?, double, Vector2\)

Draws the visible tile objects of <code class="paramref">layer</code> that the camera shows, in the layer's order, with
the layer's opacity and tint. Other objects (rectangles, points, text...) are data and draw nothing.

```csharp
public void Draw(IRenderer renderer, TiledObjectLayer layer, Camera2D? camera = null, double seconds = 0, Vector2 shift = default)
```

#### Parameters

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

The renderer; push <code class="paramref">camera</code> on it first when there is one.

`layer` [TiledObjectLayer](MicroEngine.Tiled.TiledObjectLayer.md)

An object layer of [Map](MicroEngine.Tiled.TiledSprites.md#MicroEngine_Tiled_TiledSprites_Map).

`camera` [Camera2D](MicroEngine.Graphics.Camera2D.md)?

The camera, used to skip what it does not show; null draws every object.

`seconds` [double](https://learn.microsoft.com/dotnet/api/system.double)

The time animations are at.

`shift` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

Pixels added to every object's position, for example the layer's parallax.

### <a id="MicroEngine_Tiled_TiledSprites_Draw_MicroEngine_Graphics_IRenderer_MicroEngine_Tiled_TiledObject_System_Double_System_Numerics_Vector2_System_Nullable_MicroEngine_Color__"></a> Draw\(IRenderer, TiledObject, double, Vector2, Color?\)

Draws a tile object as Tiled shows it: its tile stretched to its size, standing on its position (the
bottom-left corner), mirrored as it says and turned about that corner. Draws nothing for other objects.

```csharp
public void Draw(IRenderer renderer, TiledObject tileObject, double seconds = 0, Vector2 shift = default, Color? tint = null)
```

#### Parameters

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

The renderer.

`tileObject` [TiledObject](MicroEngine.Tiled.TiledObject.md)

A tile object of [Map](MicroEngine.Tiled.TiledSprites.md#MicroEngine_Tiled_TiledSprites_Map).

`seconds` [double](https://learn.microsoft.com/dotnet/api/system.double)

The time animations are at.

`shift` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

Pixels added to its position.

`tint` [Color](MicroEngine.Color.md)?

Colour it is multiplied by; null for white.

### <a id="MicroEngine_Tiled_TiledSprites_DrawAll_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_Camera2D_System_Double_"></a> DrawAll\(IRenderer, Camera2D?, double\)

Draws every visible layer of the map that has something to draw, bottom to top: tile layers and the tile
objects of object layers, each shifted by its parallax when a camera is given.

```csharp
public void DrawAll(IRenderer renderer, Camera2D? camera = null, double seconds = 0)
```

#### Parameters

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`camera` [Camera2D](MicroEngine.Graphics.Camera2D.md)?

`seconds` [double](https://learn.microsoft.com/dotnet/api/system.double)

</div>
