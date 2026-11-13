# Class TiledObject

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Tiled](MicroEngine.Tiled.md)  
Assembly: MicroEngine.Tiled.dll  
Guide: [Maps from Tiled](../manual/world.md#maps-from-tiled)  

An object placed on an object layer, or a collision shape of a tile, with its data.

```csharp
public sealed record TiledObject : IEquatable<TiledObject>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[TiledObject](MicroEngine.Tiled.TiledObject.md)

#### Implements

[IEquatable<TiledObject\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Tiled_TiledObject__ctor_System_Int32_System_String_System_String_MicroEngine_Tiled_TiledShape_System_Numerics_Vector2_System_Numerics_Vector2_System_Single_System_Boolean_System_Collections_Generic_IReadOnlyList_System_Numerics_Vector2__System_Int32_MicroEngine_World_TileOrientation_System_String_MicroEngine_Tiled_TiledProperties_"></a> TiledObject\(int, string, string, TiledShape, Vector2, Vector2, float, bool, IReadOnlyList<Vector2\>, int, TileOrientation, string?, TiledProperties\)

An object placed on an object layer, or a collision shape of a tile, with its data.

```csharp
public TiledObject(int Id, string Name, string Class, TiledShape Shape, Vector2 Position, Vector2 Size, float Rotation, bool Visible, IReadOnlyList<Vector2> Points, int Gid, TileOrientation Orientation, string? Text, TiledProperties Properties)
```

#### Parameters

`Id` [int](https://learn.microsoft.com/dotnet/api/system.int32)

The object's id, unique in the map (0 for a tile's collision shapes).

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)

Its name; empty when unset.

`Class` [string](https://learn.microsoft.com/dotnet/api/system.string)

Its class (Tiled's "Class", formerly "Type"); for a tile object without one, the tile's class.

`Shape` [TiledShape](MicroEngine.Tiled.TiledShape.md)

What kind of object it is.

`Position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

Where it is in map pixels, its layer's offset included: the top-left of its rectangle, the point, or the origin of its points.

`Size` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

Its width and height in pixels (zero for points, polygons and polylines).

`Rotation` [float](https://learn.microsoft.com/dotnet/api/system.single)

Clockwise rotation in radians about <code class="paramref">Position</code>.

`Visible` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether Tiled shows it.

`Points` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)\>

The points of a polygon or polyline, relative to <code class="paramref">Position</code>; empty otherwise.

`Gid` [int](https://learn.microsoft.com/dotnet/api/system.int32)

For a tile object, the global id of its tile; 0 otherwise.

`Orientation` [TileOrientation](MicroEngine.World.TileOrientation.md)

For a tile object, how its tile is mirrored.

`Text` [string](https://learn.microsoft.com/dotnet/api/system.string)?

For a text object, its text; null otherwise.

`Properties` [TiledProperties](MicroEngine.Tiled.TiledProperties.md)

Its custom properties; a tile object's include its tile's, its own winning.

## Properties

### <a id="MicroEngine_Tiled_TiledObject_Bounds"></a> Bounds

The rectangle the object covers without rotation: its rectangle, the box around its points, or an empty
rectangle at a point. A tile object's rectangle goes up from its position, as Tiled draws it.

```csharp
public RectF Bounds { get; }
```

#### Property Value

 [RectF](MicroEngine.RectF.md)

### <a id="MicroEngine_Tiled_TiledObject_Center"></a> Center

The centre of [Bounds](MicroEngine.Tiled.TiledObject.md#MicroEngine_Tiled_TiledObject_Bounds).

```csharp
public Vector2 Center { get; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Tiled_TiledObject_Class"></a> Class

Its class (Tiled's "Class", formerly "Type"); for a tile object without one, the tile's class.

```csharp
public string Class { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Tiled_TiledObject_Gid"></a> Gid

For a tile object, the global id of its tile; 0 otherwise.

```csharp
public int Gid { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Tiled_TiledObject_Id"></a> Id

The object's id, unique in the map (0 for a tile's collision shapes).

```csharp
public int Id { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Tiled_TiledObject_Name"></a> Name

Its name; empty when unset.

```csharp
public string Name { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Tiled_TiledObject_Orientation"></a> Orientation

For a tile object, how its tile is mirrored.

```csharp
public TileOrientation Orientation { get; init; }
```

#### Property Value

 [TileOrientation](MicroEngine.World.TileOrientation.md)

### <a id="MicroEngine_Tiled_TiledObject_Points"></a> Points

The points of a polygon or polyline, relative to <code class="paramref">Position</code>; empty otherwise.

```csharp
public IReadOnlyList<Vector2> Points { get; init; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)\>

### <a id="MicroEngine_Tiled_TiledObject_Position"></a> Position

Where it is in map pixels, its layer's offset included: the top-left of its rectangle, the point, or the origin of its points.

```csharp
public Vector2 Position { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Tiled_TiledObject_Properties"></a> Properties

Its custom properties; a tile object's include its tile's, its own winning.

```csharp
public TiledProperties Properties { get; init; }
```

#### Property Value

 [TiledProperties](MicroEngine.Tiled.TiledProperties.md)

### <a id="MicroEngine_Tiled_TiledObject_Rotation"></a> Rotation

Clockwise rotation in radians about <code class="paramref">Position</code>.

```csharp
public float Rotation { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Tiled_TiledObject_Shape"></a> Shape

What kind of object it is.

```csharp
public TiledShape Shape { get; init; }
```

#### Property Value

 [TiledShape](MicroEngine.Tiled.TiledShape.md)

### <a id="MicroEngine_Tiled_TiledObject_Size"></a> Size

Its width and height in pixels (zero for points, polygons and polylines).

```csharp
public Vector2 Size { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Tiled_TiledObject_Text"></a> Text

For a text object, its text; null otherwise.

```csharp
public string? Text { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Tiled_TiledObject_TurnedBounds"></a> TurnedBounds

The axis-aligned rectangle around the object as Tiled shows it turned by [Rotation](MicroEngine.Tiled.TiledObject.md#MicroEngine_Tiled_TiledObject_Rotation) about
[Position](MicroEngine.Tiled.TiledObject.md#MicroEngine_Tiled_TiledObject_Position); [Bounds](MicroEngine.Tiled.TiledObject.md#MicroEngine_Tiled_TiledObject_Bounds) when it is not turned. A quarter turn of a box gives its exact box.

```csharp
public RectF TurnedBounds { get; }
```

#### Property Value

 [RectF](MicroEngine.RectF.md)

### <a id="MicroEngine_Tiled_TiledObject_Visible"></a> Visible

Whether Tiled shows it.

```csharp
public bool Visible { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="MicroEngine_Tiled_TiledObject__Clone__"></a> <Clone\>$\(\)

```csharp
public TiledObject <Clone>$()
```

#### Returns

 [TiledObject](MicroEngine.Tiled.TiledObject.md)

### <a id="MicroEngine_Tiled_TiledObject_Deconstruct_System_Int32__System_String__System_String__MicroEngine_Tiled_TiledShape__System_Numerics_Vector2__System_Numerics_Vector2__System_Single__System_Boolean__System_Collections_Generic_IReadOnlyList_System_Numerics_Vector2___System_Int32__MicroEngine_World_TileOrientation__System_String__MicroEngine_Tiled_TiledProperties__"></a> Deconstruct\(out int, out string, out string, out TiledShape, out Vector2, out Vector2, out float, out bool, out IReadOnlyList<Vector2\>, out int, out TileOrientation, out string?, out TiledProperties\)

```csharp
public void Deconstruct(out int Id, out string Name, out string Class, out TiledShape Shape, out Vector2 Position, out Vector2 Size, out float Rotation, out bool Visible, out IReadOnlyList<Vector2> Points, out int Gid, out TileOrientation Orientation, out string? Text, out TiledProperties Properties)
```

#### Parameters

`Id` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Class` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Shape` [TiledShape](MicroEngine.Tiled.TiledShape.md)

`Position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`Size` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`Rotation` [float](https://learn.microsoft.com/dotnet/api/system.single)

`Visible` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`Points` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)\>

`Gid` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Orientation` [TileOrientation](MicroEngine.World.TileOrientation.md)

`Text` [string](https://learn.microsoft.com/dotnet/api/system.string)?

`Properties` [TiledProperties](MicroEngine.Tiled.TiledProperties.md)

### <a id="MicroEngine_Tiled_TiledObject_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Tiled_TiledObject_Equals_MicroEngine_Tiled_TiledObject_"></a> Equals\(TiledObject?\)

```csharp
public bool Equals(TiledObject? other)
```

#### Parameters

`other` [TiledObject](MicroEngine.Tiled.TiledObject.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Tiled_TiledObject_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Tiled_TiledObject_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Tiled_TiledObject_op_Equality_MicroEngine_Tiled_TiledObject_MicroEngine_Tiled_TiledObject_"></a> operator ==\(TiledObject?, TiledObject?\)

```csharp
public static bool operator ==(TiledObject? left, TiledObject? right)
```

#### Parameters

`left` [TiledObject](MicroEngine.Tiled.TiledObject.md)?

`right` [TiledObject](MicroEngine.Tiled.TiledObject.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Tiled_TiledObject_op_Inequality_MicroEngine_Tiled_TiledObject_MicroEngine_Tiled_TiledObject_"></a> operator \!=\(TiledObject?, TiledObject?\)

```csharp
public static bool operator !=(TiledObject? left, TiledObject? right)
```

#### Parameters

`left` [TiledObject](MicroEngine.Tiled.TiledObject.md)?

`right` [TiledObject](MicroEngine.Tiled.TiledObject.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
