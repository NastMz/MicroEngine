# Struct ShapeDescription

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics worlds](../manual/world.md#physics-worlds)  

What [AddShape](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_AddShape_MicroEngine_Physics_BodyId_MicroEngine_Physics_ShapeDescription__) attaches to a body. Build one with [Box](MicroEngine.Physics.ShapeDescription.md#MicroEngine_Physics_ShapeDescription_Box_System_Numerics_Vector2_System_Numerics_Vector2_) (common) or a capability's factory.

```csharp
public readonly struct ShapeDescription : IEquatable<ShapeDescription>
```

#### Implements

[IEquatable<ShapeDescription\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Properties

### <a id="MicroEngine_Physics_ShapeDescription_Category"></a> Category

The layers the shape is in ([CollisionLayers](MicroEngine.Physics.CollisionLayers.md)); [Default](MicroEngine.Physics.CollisionLayers.md#MicroEngine_Physics_CollisionLayers_Default) by default.

```csharp
public ulong Category { get; init; }
```

#### Property Value

 [ulong](https://learn.microsoft.com/dotnet/api/system.uint64)

### <a id="MicroEngine_Physics_ShapeDescription_Density"></a> Density

Density; null for the backend's default. A value needs [MassProperties](MicroEngine.Physics.PhysicsCapabilities.md).

```csharp
public float? Density { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)?

### <a id="MicroEngine_Physics_ShapeDescription_Friction"></a> Friction

Friction; null for the backend's default. A value needs [FrictionRestitution](MicroEngine.Physics.PhysicsCapabilities.md).

```csharp
public float? Friction { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)?

### <a id="MicroEngine_Physics_ShapeDescription_IsSensor"></a> IsSensor

A sensor: it never blocks and is never blocked; it reports the shapes that enter and leave it as
[SensorEvents](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_SensorEvents).

```csharp
public bool IsSensor { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_ShapeDescription_Kind"></a> Kind

The geometry.

```csharp
public ShapeKind Kind { get; init; }
```

#### Property Value

 [ShapeKind](MicroEngine.Physics.ShapeKind.md)

### <a id="MicroEngine_Physics_ShapeDescription_Mask"></a> Mask

The layers it collides with; [All](MicroEngine.Physics.CollisionLayers.md#MicroEngine_Physics_CollisionLayers_All) by default.

```csharp
public ulong Mask { get; init; }
```

#### Property Value

 [ulong](https://learn.microsoft.com/dotnet/api/system.uint64)

### <a id="MicroEngine_Physics_ShapeDescription_Offset"></a> Offset

The shape's centre relative to its body's origin.

```csharp
public Vector2 Offset { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Physics_ShapeDescription_OneWay"></a> OneWay

A one-way platform (needs [OneWayPlatforms](MicroEngine.Physics.PhysicsCapabilities.md)): it stops only bodies coming down onto its up-facing side.

```csharp
public bool OneWay { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_ShapeDescription_Radius"></a> Radius

The radius of a circle or a capsule's ends.

```csharp
public float Radius { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_ShapeDescription_RequiredCapabilities"></a> RequiredCapabilities

The capabilities this description needs beyond the common contract.

```csharp
public PhysicsCapabilities RequiredCapabilities { get; }
```

#### Property Value

 [PhysicsCapabilities](MicroEngine.Physics.PhysicsCapabilities.md)

### <a id="MicroEngine_Physics_ShapeDescription_Restitution"></a> Restitution

Restitution (bounciness); null for the backend's default. A value needs [FrictionRestitution](MicroEngine.Physics.PhysicsCapabilities.md).

```csharp
public float? Restitution { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)?

### <a id="MicroEngine_Physics_ShapeDescription_SensorEvents"></a> SensorEvents

Whether sensors see this shape (true by default). Sensor events need it on the visiting shape as well as a
sensor that accepts its layers; the backend enables whatever it needs from this flag.

```csharp
public bool SensorEvents { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_ShapeDescription_Size"></a> Size

A box's full width and height, in world units; both positive. Unused by circles.

```csharp
public Vector2 Size { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Physics_ShapeDescription_Vertices"></a> Vertices

The convex polygon's vertices, relative to [Offset](MicroEngine.Physics.ShapeDescription.md#MicroEngine_Physics_ShapeDescription_Offset), counter-clockwise.

```csharp
public ReadOnlyMemory<Vector2> Vertices { get; init; }
```

#### Property Value

 [ReadOnlyMemory](https://learn.microsoft.com/dotnet/api/system.readonlymemory\-1)<[Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)\>

## Methods

### <a id="MicroEngine_Physics_ShapeDescription_Box_System_Numerics_Vector2_System_Numerics_Vector2_"></a> Box\(Vector2, Vector2\)

An axis-aligned box of <code class="paramref">size</code> centred at <code class="paramref">offset</code> from its body's origin, in the default layers.

```csharp
public static ShapeDescription Box(Vector2 size, Vector2 offset = default)
```

#### Parameters

`size` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`offset` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [ShapeDescription](MicroEngine.Physics.ShapeDescription.md)

### <a id="MicroEngine_Physics_ShapeDescription_Capsule_System_Single_System_Single_System_Numerics_Vector2_"></a> Capsule\(float, float, Vector2\)

A vertical capsule <code class="paramref">height</code> tall overall with ends of <code class="paramref">radius</code> (needs [Capsules](MicroEngine.Physics.PhysicsCapabilities.md)).

```csharp
public static ShapeDescription Capsule(float radius, float height, Vector2 offset = default)
```

#### Parameters

`radius` [float](https://learn.microsoft.com/dotnet/api/system.single)

`height` [float](https://learn.microsoft.com/dotnet/api/system.single)

`offset` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [ShapeDescription](MicroEngine.Physics.ShapeDescription.md)

### <a id="MicroEngine_Physics_ShapeDescription_Circle_System_Single_System_Numerics_Vector2_"></a> Circle\(float, Vector2\)

A circle (needs [Circles](MicroEngine.Physics.PhysicsCapabilities.md)).

```csharp
public static ShapeDescription Circle(float radius, Vector2 offset = default)
```

#### Parameters

`radius` [float](https://learn.microsoft.com/dotnet/api/system.single)

`offset` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [ShapeDescription](MicroEngine.Physics.ShapeDescription.md)

### <a id="MicroEngine_Physics_ShapeDescription_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_ShapeDescription_Equals_MicroEngine_Physics_ShapeDescription_"></a> Equals\(ShapeDescription\)

```csharp
public bool Equals(ShapeDescription other)
```

#### Parameters

`other` [ShapeDescription](MicroEngine.Physics.ShapeDescription.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_ShapeDescription_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Physics_ShapeDescription_InLayers_System_UInt64_System_UInt64_"></a> InLayers\(ulong, ulong\)

The same shape in other layers.

```csharp
public ShapeDescription InLayers(ulong category, ulong mask)
```

#### Parameters

`category` [ulong](https://learn.microsoft.com/dotnet/api/system.uint64)

`mask` [ulong](https://learn.microsoft.com/dotnet/api/system.uint64)

#### Returns

 [ShapeDescription](MicroEngine.Physics.ShapeDescription.md)

### <a id="MicroEngine_Physics_ShapeDescription_Polygon_System_ReadOnlyMemory_System_Numerics_Vector2__System_Numerics_Vector2_"></a> Polygon\(ReadOnlyMemory<Vector2\>, Vector2\)

A convex polygon (needs [Polygons](MicroEngine.Physics.PhysicsCapabilities.md)).

```csharp
public static ShapeDescription Polygon(ReadOnlyMemory<Vector2> vertices, Vector2 offset = default)
```

#### Parameters

`vertices` [ReadOnlyMemory](https://learn.microsoft.com/dotnet/api/system.readonlymemory\-1)<[Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)\>

`offset` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [ShapeDescription](MicroEngine.Physics.ShapeDescription.md)

### <a id="MicroEngine_Physics_ShapeDescription_Sensor_System_Numerics_Vector2_System_Numerics_Vector2_"></a> Sensor\(Vector2, Vector2\)

A box sensor of <code class="paramref">size</code> centred at <code class="paramref">offset</code>.

```csharp
public static ShapeDescription Sensor(Vector2 size, Vector2 offset = default)
```

#### Parameters

`size` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`offset` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [ShapeDescription](MicroEngine.Physics.ShapeDescription.md)

### <a id="MicroEngine_Physics_ShapeDescription_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Physics_ShapeDescription_Validate"></a> Validate\(\)

Throws when the description is invalid whatever the backend declares.

```csharp
public void Validate()
```

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The kind is undefined, a size, radius or offset is not positive or not finite, or a box's size is not positive.

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

A sensor is also one-way.

## Operators

### <a id="MicroEngine_Physics_ShapeDescription_op_Equality_MicroEngine_Physics_ShapeDescription_MicroEngine_Physics_ShapeDescription_"></a> operator ==\(ShapeDescription, ShapeDescription\)

```csharp
public static bool operator ==(ShapeDescription left, ShapeDescription right)
```

#### Parameters

`left` [ShapeDescription](MicroEngine.Physics.ShapeDescription.md)

`right` [ShapeDescription](MicroEngine.Physics.ShapeDescription.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_ShapeDescription_op_Inequality_MicroEngine_Physics_ShapeDescription_MicroEngine_Physics_ShapeDescription_"></a> operator \!=\(ShapeDescription, ShapeDescription\)

```csharp
public static bool operator !=(ShapeDescription left, ShapeDescription right)
```

#### Parameters

`left` [ShapeDescription](MicroEngine.Physics.ShapeDescription.md)

`right` [ShapeDescription](MicroEngine.Physics.ShapeDescription.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
