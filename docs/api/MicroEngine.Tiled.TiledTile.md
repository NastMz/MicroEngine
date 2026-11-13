# Class TiledTile

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Tiled](MicroEngine.Tiled.md)  
Assembly: MicroEngine.Tiled.dll  
Guide: [Maps from Tiled](../manual/world.md#maps-from-tiled)  

A tile of a tileset: where its pixels are and its data.

```csharp
public sealed record TiledTile : IEquatable<TiledTile>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[TiledTile](MicroEngine.Tiled.TiledTile.md)

#### Implements

[IEquatable<TiledTile\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Tiled_TiledTile__ctor_MicroEngine_Tiled_TiledTileset_System_Int32_System_Int32_System_String_MicroEngine_RectF_System_String_MicroEngine_Tiled_TiledProperties_System_Collections_Generic_IReadOnlyList_MicroEngine_Tiled_TiledFrame__System_Collections_Generic_IReadOnlyList_MicroEngine_Tiled_TiledObject__"></a> TiledTile\(TiledTileset, int, int, string, RectF, string, TiledProperties, IReadOnlyList<TiledFrame\>, IReadOnlyList<TiledObject\>\)

A tile of a tileset: where its pixels are and its data.

```csharp
public TiledTile(TiledTileset Tileset, int Id, int Gid, string Image, RectF Source, string Class, TiledProperties Properties, IReadOnlyList<TiledFrame> Animation, IReadOnlyList<TiledObject> Shapes)
```

#### Parameters

`Tileset` [TiledTileset](MicroEngine.Tiled.TiledTileset.md)

The tileset holding it.

`Id` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Its id in the tileset.

`Gid` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Its global id in the map.

`Image` [string](https://learn.microsoft.com/dotnet/api/system.string)

The content path of the image its pixels are in.

`Source` [RectF](MicroEngine.RectF.md)

Its rectangle in that image, in pixels.

`Class` [string](https://learn.microsoft.com/dotnet/api/system.string)

Its class; empty when unset.

`Properties` [TiledProperties](MicroEngine.Tiled.TiledProperties.md)

Its custom properties.

`Animation` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[TiledFrame](MicroEngine.Tiled.TiledFrame.md)\>

Its animation frames; empty when it does not animate.

`Shapes` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[TiledObject](MicroEngine.Tiled.TiledObject.md)\>

Its collision shapes drawn in Tiled's tile collision editor, in tile pixels; empty when none.

## Properties

### <a id="MicroEngine_Tiled_TiledTile_Animation"></a> Animation

Its animation frames; empty when it does not animate.

```csharp
public IReadOnlyList<TiledFrame> Animation { get; init; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[TiledFrame](MicroEngine.Tiled.TiledFrame.md)\>

### <a id="MicroEngine_Tiled_TiledTile_AnimationLength"></a> AnimationLength

The length of one loop of [Animation](MicroEngine.Tiled.TiledTile.md#MicroEngine_Tiled_TiledTile_Animation) in seconds (0 when it does not animate).

```csharp
public float AnimationLength { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Tiled_TiledTile_Class"></a> Class

Its class; empty when unset.

```csharp
public string Class { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Tiled_TiledTile_Gid"></a> Gid

Its global id in the map.

```csharp
public int Gid { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Tiled_TiledTile_Id"></a> Id

Its id in the tileset.

```csharp
public int Id { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Tiled_TiledTile_Image"></a> Image

The content path of the image its pixels are in.

```csharp
public string Image { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Tiled_TiledTile_Properties"></a> Properties

Its custom properties.

```csharp
public TiledProperties Properties { get; init; }
```

#### Property Value

 [TiledProperties](MicroEngine.Tiled.TiledProperties.md)

### <a id="MicroEngine_Tiled_TiledTile_Shapes"></a> Shapes

Its collision shapes drawn in Tiled's tile collision editor, in tile pixels; empty when none.

```csharp
public IReadOnlyList<TiledObject> Shapes { get; init; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[TiledObject](MicroEngine.Tiled.TiledObject.md)\>

### <a id="MicroEngine_Tiled_TiledTile_Source"></a> Source

Its rectangle in that image, in pixels.

```csharp
public RectF Source { get; init; }
```

#### Property Value

 [RectF](MicroEngine.RectF.md)

### <a id="MicroEngine_Tiled_TiledTile_Tileset"></a> Tileset

The tileset holding it.

```csharp
public TiledTileset Tileset { get; init; }
```

#### Property Value

 [TiledTileset](MicroEngine.Tiled.TiledTileset.md)

## Methods

### <a id="MicroEngine_Tiled_TiledTile__Clone__"></a> <Clone\>$\(\)

```csharp
public TiledTile <Clone>$()
```

#### Returns

 [TiledTile](MicroEngine.Tiled.TiledTile.md)

### <a id="MicroEngine_Tiled_TiledTile_Deconstruct_MicroEngine_Tiled_TiledTileset__System_Int32__System_Int32__System_String__MicroEngine_RectF__System_String__MicroEngine_Tiled_TiledProperties__System_Collections_Generic_IReadOnlyList_MicroEngine_Tiled_TiledFrame___System_Collections_Generic_IReadOnlyList_MicroEngine_Tiled_TiledObject___"></a> Deconstruct\(out TiledTileset, out int, out int, out string, out RectF, out string, out TiledProperties, out IReadOnlyList<TiledFrame\>, out IReadOnlyList<TiledObject\>\)

```csharp
public void Deconstruct(out TiledTileset Tileset, out int Id, out int Gid, out string Image, out RectF Source, out string Class, out TiledProperties Properties, out IReadOnlyList<TiledFrame> Animation, out IReadOnlyList<TiledObject> Shapes)
```

#### Parameters

`Tileset` [TiledTileset](MicroEngine.Tiled.TiledTileset.md)

`Id` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Gid` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Image` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Source` [RectF](MicroEngine.RectF.md)

`Class` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Properties` [TiledProperties](MicroEngine.Tiled.TiledProperties.md)

`Animation` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[TiledFrame](MicroEngine.Tiled.TiledFrame.md)\>

`Shapes` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[TiledObject](MicroEngine.Tiled.TiledObject.md)\>

### <a id="MicroEngine_Tiled_TiledTile_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Tiled_TiledTile_Equals_MicroEngine_Tiled_TiledTile_"></a> Equals\(TiledTile?\)

```csharp
public bool Equals(TiledTile? other)
```

#### Parameters

`other` [TiledTile](MicroEngine.Tiled.TiledTile.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Tiled_TiledTile_FrameAt_System_Double_"></a> FrameAt\(double\)

The global id shown <code class="paramref">seconds</code> into the animation (looping), or [Gid](MicroEngine.Tiled.TiledTile.md#MicroEngine_Tiled_TiledTile_Gid) when it does not animate.

```csharp
public int FrameAt(double seconds)
```

#### Parameters

`seconds` [double](https://learn.microsoft.com/dotnet/api/system.double)

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Tiled_TiledTile_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Tiled_TiledTile_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Tiled_TiledTile_op_Equality_MicroEngine_Tiled_TiledTile_MicroEngine_Tiled_TiledTile_"></a> operator ==\(TiledTile?, TiledTile?\)

```csharp
public static bool operator ==(TiledTile? left, TiledTile? right)
```

#### Parameters

`left` [TiledTile](MicroEngine.Tiled.TiledTile.md)?

`right` [TiledTile](MicroEngine.Tiled.TiledTile.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Tiled_TiledTile_op_Inequality_MicroEngine_Tiled_TiledTile_MicroEngine_Tiled_TiledTile_"></a> operator \!=\(TiledTile?, TiledTile?\)

```csharp
public static bool operator !=(TiledTile? left, TiledTile? right)
```

#### Parameters

`left` [TiledTile](MicroEngine.Tiled.TiledTile.md)?

`right` [TiledTile](MicroEngine.Tiled.TiledTile.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
