# Class TileGrid

Namespace: [MicroEngine.World](MicroEngine.World.md)  
Assembly: MicroEngine.dll  
Guide: [Tile maps and collision](../manual/world.md)  

A collision grid backed by a [TileLayer](MicroEngine.World.TileLayer.md) and a per-id flag table.
Cells outside the map use [OutsideFlags](MicroEngine.World.TileGrid.md#MicroEngine_World_TileGrid_OutsideFlags) (solid by default, so levels are closed).

```csharp
public sealed class TileGrid : ICollisionGrid
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[TileGrid](MicroEngine.World.TileGrid.md)

#### Implements

[ICollisionGrid](MicroEngine.World.ICollisionGrid.md)

## Constructors

### <a id="MicroEngine_World_TileGrid__ctor_MicroEngine_World_TileLayer_System_Int32_System_Collections_Generic_IReadOnlyDictionary_System_Int32_MicroEngine_World_TileFlags__"></a> TileGrid\(TileLayer, int, IReadOnlyDictionary<int, TileFlags\>\)

Creates a grid; ids missing from <code class="paramref">flags</code> have no flags (empty).

```csharp
public TileGrid(TileLayer layer, int tileSize, IReadOnlyDictionary<int, TileFlags> flags)
```

#### Parameters

`layer` [TileLayer](MicroEngine.World.TileLayer.md)

The tile ids.

`tileSize` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Tile size in pixels; must be positive.

`flags` [IReadOnlyDictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlydictionary\-2)<[int](https://learn.microsoft.com/dotnet/api/system.int32), [TileFlags](MicroEngine.World.TileFlags.md)\>

Flags per tile id; copied.

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">tileSize</code> is not positive.

## Properties

### <a id="MicroEngine_World_TileGrid_Layer"></a> Layer

The tile ids the grid reads. Changing a tile in the layer changes collisions at once.

```csharp
public TileLayer Layer { get; }
```

#### Property Value

 [TileLayer](MicroEngine.World.TileLayer.md)

### <a id="MicroEngine_World_TileGrid_OutsideFlags"></a> OutsideFlags

Flags of every cell outside the map when [OutsideRule](MicroEngine.World.TileGrid.md#MicroEngine_World_TileGrid_OutsideRule) gives none (solid by default).

```csharp
public TileFlags OutsideFlags { get; set; }
```

#### Property Value

 [TileFlags](MicroEngine.World.TileFlags.md)

### <a id="MicroEngine_World_TileGrid_OutsideRule"></a> OutsideRule

Optional override for open edges, e.g. a platformer's bottom pit must not be solid.

```csharp
public Func<int, int, TileFlags?>? OutsideRule { get; set; }
```

#### Property Value

 [Func](https://learn.microsoft.com/dotnet/api/system.func\-3)<[int](https://learn.microsoft.com/dotnet/api/system.int32), [int](https://learn.microsoft.com/dotnet/api/system.int32), [TileFlags](MicroEngine.World.TileFlags.md)?\>?

### <a id="MicroEngine_World_TileGrid_PixelHeight"></a> PixelHeight

Height of the map in pixels.

```csharp
public int PixelHeight { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_World_TileGrid_PixelWidth"></a> PixelWidth

Width of the map in pixels.

```csharp
public int PixelWidth { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_World_TileGrid_TileSize"></a> TileSize

Size of a square tile in pixels.

```csharp
public int TileSize { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_World_TileGrid_FlagsAt_System_Int32_System_Int32_"></a> FlagsAt\(int, int\)

The flags of the tile at (<code class="paramref">tileX</code>, <code class="paramref">tileY</code>); outside the map, [OutsideRule](MicroEngine.World.TileGrid.md#MicroEngine_World_TileGrid_OutsideRule) or [OutsideFlags](MicroEngine.World.TileGrid.md#MicroEngine_World_TileGrid_OutsideFlags).

```csharp
public TileFlags FlagsAt(int tileX, int tileY)
```

#### Parameters

`tileX` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`tileY` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [TileFlags](MicroEngine.World.TileFlags.md)

### <a id="MicroEngine_World_TileGrid_SetFlags_System_Int32_MicroEngine_World_TileFlags_"></a> SetFlags\(int, TileFlags\)

Sets the flags of every tile with id <code class="paramref">id</code>.

```csharp
public void SetFlags(int id, TileFlags flags)
```

#### Parameters

`id` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`flags` [TileFlags](MicroEngine.World.TileFlags.md)
