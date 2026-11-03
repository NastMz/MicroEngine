# Struct SensorEvent

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics worlds](../manual/world.md#physics-worlds)  

A shape entered or left a sensor during the latest [Step](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Step_System_Single_). The ids and owners are copied
when the event is made, so an exit caused by destroying a shape still names it.

```csharp
public readonly struct SensorEvent : IEquatable<SensorEvent>
```

#### Implements

[IEquatable<SensorEvent\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Physics_SensorEvent__ctor_MicroEngine_Physics_SensorEventKind_MicroEngine_Physics_ShapeId_MicroEngine_Physics_ShapeId_MicroEngine_Scenes_EntityId_MicroEngine_Scenes_EntityId_"></a> SensorEvent\(SensorEventKind, ShapeId, ShapeId, EntityId, EntityId\)

A shape entered or left a sensor during the latest [Step](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Step_System_Single_). The ids and owners are copied
when the event is made, so an exit caused by destroying a shape still names it.

```csharp
public SensorEvent(SensorEventKind Kind, ShapeId Sensor, ShapeId Visitor, EntityId SensorOwner, EntityId VisitorOwner)
```

#### Parameters

`Kind` [SensorEventKind](MicroEngine.Physics.SensorEventKind.md)

Entry or exit.

`Sensor` [ShapeId](MicroEngine.Physics.ShapeId.md)

The sensor shape.

`Visitor` [ShapeId](MicroEngine.Physics.ShapeId.md)

The shape that entered or left.

`SensorOwner` EntityId

The entity owning the sensor's body.

`VisitorOwner` EntityId

The entity owning the visitor's body.

## Properties

### <a id="MicroEngine_Physics_SensorEvent_Kind"></a> Kind

Entry or exit.

```csharp
public SensorEventKind Kind { get; init; }
```

#### Property Value

 [SensorEventKind](MicroEngine.Physics.SensorEventKind.md)

### <a id="MicroEngine_Physics_SensorEvent_Sensor"></a> Sensor

The sensor shape.

```csharp
public ShapeId Sensor { get; init; }
```

#### Property Value

 [ShapeId](MicroEngine.Physics.ShapeId.md)

### <a id="MicroEngine_Physics_SensorEvent_SensorOwner"></a> SensorOwner

The entity owning the sensor's body.

```csharp
public EntityId SensorOwner { get; init; }
```

#### Property Value

 EntityId

### <a id="MicroEngine_Physics_SensorEvent_Visitor"></a> Visitor

The shape that entered or left.

```csharp
public ShapeId Visitor { get; init; }
```

#### Property Value

 [ShapeId](MicroEngine.Physics.ShapeId.md)

### <a id="MicroEngine_Physics_SensorEvent_VisitorOwner"></a> VisitorOwner

The entity owning the visitor's body.

```csharp
public EntityId VisitorOwner { get; init; }
```

#### Property Value

 EntityId

## Methods

### <a id="MicroEngine_Physics_SensorEvent_Deconstruct_MicroEngine_Physics_SensorEventKind__MicroEngine_Physics_ShapeId__MicroEngine_Physics_ShapeId__MicroEngine_Scenes_EntityId__MicroEngine_Scenes_EntityId__"></a> Deconstruct\(out SensorEventKind, out ShapeId, out ShapeId, out EntityId, out EntityId\)

```csharp
public void Deconstruct(out SensorEventKind Kind, out ShapeId Sensor, out ShapeId Visitor, out EntityId SensorOwner, out EntityId VisitorOwner)
```

#### Parameters

`Kind` [SensorEventKind](MicroEngine.Physics.SensorEventKind.md)

`Sensor` [ShapeId](MicroEngine.Physics.ShapeId.md)

`Visitor` [ShapeId](MicroEngine.Physics.ShapeId.md)

`SensorOwner` EntityId

`VisitorOwner` EntityId

### <a id="MicroEngine_Physics_SensorEvent_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_SensorEvent_Equals_MicroEngine_Physics_SensorEvent_"></a> Equals\(SensorEvent\)

```csharp
public bool Equals(SensorEvent other)
```

#### Parameters

`other` [SensorEvent](MicroEngine.Physics.SensorEvent.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_SensorEvent_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Physics_SensorEvent_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Physics_SensorEvent_op_Equality_MicroEngine_Physics_SensorEvent_MicroEngine_Physics_SensorEvent_"></a> operator ==\(SensorEvent, SensorEvent\)

```csharp
public static bool operator ==(SensorEvent left, SensorEvent right)
```

#### Parameters

`left` [SensorEvent](MicroEngine.Physics.SensorEvent.md)

`right` [SensorEvent](MicroEngine.Physics.SensorEvent.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_SensorEvent_op_Inequality_MicroEngine_Physics_SensorEvent_MicroEngine_Physics_SensorEvent_"></a> operator \!=\(SensorEvent, SensorEvent\)

```csharp
public static bool operator !=(SensorEvent left, SensorEvent right)
```

#### Parameters

`left` [SensorEvent](MicroEngine.Physics.SensorEvent.md)

`right` [SensorEvent](MicroEngine.Physics.SensorEvent.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
