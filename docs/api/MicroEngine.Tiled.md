# Namespace MicroEngine.Tiled

<div data-pagefind-weight="0.3">

Maps made in the Tiled editor (optional package): the map loaded through the assets, its tile, object, image and group layers, objects and typed properties, tilesets and tiles, collision grids from a tile property, and the sprites that draw tile layers and tile objects.

Guide: [Maps from Tiled](../manual/world.md#maps-from-tiled)  

### Classes

 [TiledAssets](MicroEngine.Tiled.TiledAssets.md)

The <code>tiled map</code> asset kind: a Tiled map (<code>.tmx</code> or <code>.tmj</code>) with the tilesets (<code>.tsx</code>,
<code>.tsj</code>) and templates (<code>.tx</code>, <code>.tj</code>) it references, read through the store's content source so it
loads from folders, archives and browser packages alike. The asset is data ([TiledMap](MicroEngine.Tiled.TiledMap.md)); its
tile images become textures with [TiledSprites](MicroEngine.Tiled.TiledSprites.md).

 [TiledGroupLayer](MicroEngine.Tiled.TiledGroupLayer.md)

A group of layers.

 [TiledImageLayer](MicroEngine.Tiled.TiledImageLayer.md)

A layer showing one image.

 [TiledLayer](MicroEngine.Tiled.TiledLayer.md)

A layer of a [TiledMap](MicroEngine.Tiled.TiledMap.md): what every kind of layer has.

 [TiledMap](MicroEngine.Tiled.TiledMap.md)

A map made in the Tiled editor, read as data: its size, layers, tilesets and objects. Load it with
[TiledMap](MicroEngine.Tiled.TiledAssets.md#MicroEngine_Tiled_TiledAssets_TiledMap_MicroEngine_Core_AssetCache_System_String_); give its tiles textures with [TiledSprites](MicroEngine.Tiled.TiledSprites.md).

 [TiledObjectLayer](MicroEngine.Tiled.TiledObjectLayer.md)

A layer of objects: placements, areas and points with their data.

 [TiledObject](MicroEngine.Tiled.TiledObject.md)

An object placed on an object layer, or a collision shape of a tile, with its data.

 [TiledProperties](MicroEngine.Tiled.TiledProperties.md)

The custom properties of a map, layer, tileset, tile or object, read as data: the engine gives no property a
meaning. Each getter returns its fallback when the property is missing, and throws when it exists with another type.

 [TiledProperty](MicroEngine.Tiled.TiledProperty.md)

One custom property: its name, type and value (see [TiledPropertyType](MicroEngine.Tiled.TiledPropertyType.md) for the value's type).

 [TiledSprites](MicroEngine.Tiled.TiledSprites.md)

The sprites of a [TiledMap](MicroEngine.Tiled.TiledMap.md)'s tiles: each tileset image loaded as a texture through the game's assets
(shared and released with them), and a draw call for tile layers.

 [TiledTileLayer](MicroEngine.Tiled.TiledTileLayer.md)

A layer of tiles: one global tile id per cell (0 for none) and how each cell's tile is turned.

 [TiledTile](MicroEngine.Tiled.TiledTile.md)

A tile of a tileset: where its pixels are and its data.

 [TiledTileset](MicroEngine.Tiled.TiledTileset.md)

A tileset of a [TiledMap](MicroEngine.Tiled.TiledMap.md): one image cut into tiles, or a collection of images.

### Structs

 [TiledFrame](MicroEngine.Tiled.TiledFrame.md)

One frame of a tile animation.

### Enums

 [TiledPropertyType](MicroEngine.Tiled.TiledPropertyType.md)

The type of a custom property, as Tiled names it.

 [TiledShape](MicroEngine.Tiled.TiledShape.md)

The shape of a [TiledObject](MicroEngine.Tiled.TiledObject.md).

</div>
