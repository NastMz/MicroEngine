# Struct RectF

Namespace: [MicroEngine](MicroEngine.md)  
Assembly: MicroEngine.dll  
Guide: [Graphics](../manual/rendering.md)  

Axis-aligned rectangle in world or screen units (Y grows downward).

```csharp
public readonly struct RectF : IEquatable<RectF>
```

#### Implements

[IEquatable<RectF\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_RectF__ctor_System_Single_System_Single_System_Single_System_Single_"></a> RectF\(float, float, float, float\)

Axis-aligned rectangle in world or screen units (Y grows downward).

```csharp
public RectF(float X, float Y, float Width, float Height)
```

#### Parameters

`X` [float](https://learn.microsoft.com/dotnet/api/system.single)

Left edge.

`Y` [float](https://learn.microsoft.com/dotnet/api/system.single)

Top edge.

`Width` [float](https://learn.microsoft.com/dotnet/api/system.single)

Width; rectangles with zero or negative size contain nothing.

`Height` [float](https://learn.microsoft.com/dotnet/api/system.single)

Height.

## Properties

### <a id="MicroEngine_RectF_Bottom"></a> Bottom

The bottom edge, <code>Y + Height</code> (exclusive).

```csharp
public float Bottom { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_RectF_Center"></a> Center

The centre point.

```csharp
public Vector2 Center { get; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_RectF_Height"></a> Height

Height.

```csharp
public float Height { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_RectF_Left"></a> Left

The left edge ([X](MicroEngine.RectF.md#MicroEngine_RectF_X)).

```csharp
public float Left { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_RectF_Position"></a> Position

The top-left corner.

```csharp
public Vector2 Position { get; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_RectF_Right"></a> Right

The right edge, <code>X + Width</code> (exclusive).

```csharp
public float Right { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_RectF_Size"></a> Size

Width and height as a vector.

```csharp
public Vector2 Size { get; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_RectF_Top"></a> Top

The top edge ([Y](MicroEngine.RectF.md#MicroEngine_RectF_Y)).

```csharp
public float Top { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_RectF_Width"></a> Width

Width; rectangles with zero or negative size contain nothing.

```csharp
public float Width { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_RectF_X"></a> X

Left edge.

```csharp
public float X { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_RectF_Y"></a> Y

Top edge.

```csharp
public float Y { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_RectF_Contains_System_Numerics_Vector2_"></a> Contains\(Vector2\)

Whether <code class="paramref">point</code> is inside; the left and top edges are inside, the right and bottom edges are not.

```csharp
public bool Contains(Vector2 point)
```

#### Parameters

`point` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_RectF_Deconstruct_System_Single__System_Single__System_Single__System_Single__"></a> Deconstruct\(out float, out float, out float, out float\)

```csharp
public void Deconstruct(out float X, out float Y, out float Width, out float Height)
```

#### Parameters

`X` [float](https://learn.microsoft.com/dotnet/api/system.single)

`Y` [float](https://learn.microsoft.com/dotnet/api/system.single)

`Width` [float](https://learn.microsoft.com/dotnet/api/system.single)

`Height` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_RectF_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_RectF_Equals_MicroEngine_RectF_"></a> Equals\(RectF\)

```csharp
public bool Equals(RectF other)
```

#### Parameters

`other` [RectF](MicroEngine.RectF.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_RectF_FromCenter_System_Numerics_Vector2_System_Single_System_Single_"></a> FromCenter\(Vector2, float, float\)

A rectangle of the given size centred on <code class="paramref">center</code>.

```csharp
public static RectF FromCenter(Vector2 center, float width, float height)
```

#### Parameters

`center` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`width` [float](https://learn.microsoft.com/dotnet/api/system.single)

`height` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [RectF](MicroEngine.RectF.md)

### <a id="MicroEngine_RectF_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_RectF_Inflate_System_Single_"></a> Inflate\(float\)

A copy grown by <code class="paramref">amount</code> on every side (shrunk when negative).

```csharp
public RectF Inflate(float amount)
```

#### Parameters

`amount` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [RectF](MicroEngine.RectF.md)

### <a id="MicroEngine_RectF_Intersection_MicroEngine_RectF_"></a> Intersection\(RectF\)

The overlapping area of both rectangles, or <code>default</code> (all zeros) when they do not overlap.

```csharp
public RectF Intersection(RectF other)
```

#### Parameters

`other` [RectF](MicroEngine.RectF.md)

#### Returns

 [RectF](MicroEngine.RectF.md)

### <a id="MicroEngine_RectF_Intersects_MicroEngine_RectF_"></a> Intersects\(RectF\)

Strict overlap: rectangles that only share an edge do not intersect.

```csharp
public bool Intersects(RectF other)
```

#### Parameters

`other` [RectF](MicroEngine.RectF.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_RectF_Offset_System_Single_System_Single_"></a> Offset\(float, float\)

A copy moved by (<code class="paramref">dx</code>, <code class="paramref">dy</code>).

```csharp
public RectF Offset(float dx, float dy)
```

#### Parameters

`dx` [float](https://learn.microsoft.com/dotnet/api/system.single)

`dy` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [RectF](MicroEngine.RectF.md)

### <a id="MicroEngine_RectF_Offset_System_Numerics_Vector2_"></a> Offset\(Vector2\)

A copy moved by <code class="paramref">delta</code>.

```csharp
public RectF Offset(Vector2 delta)
```

#### Parameters

`delta` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [RectF](MicroEngine.RectF.md)

### <a id="MicroEngine_RectF_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_RectF_op_Equality_MicroEngine_RectF_MicroEngine_RectF_"></a> operator ==\(RectF, RectF\)

```csharp
public static bool operator ==(RectF left, RectF right)
```

#### Parameters

`left` [RectF](MicroEngine.RectF.md)

`right` [RectF](MicroEngine.RectF.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_RectF_op_Inequality_MicroEngine_RectF_MicroEngine_RectF_"></a> operator \!=\(RectF, RectF\)

```csharp
public static bool operator !=(RectF left, RectF right)
```

#### Parameters

`left` [RectF](MicroEngine.RectF.md)

`right` [RectF](MicroEngine.RectF.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)
