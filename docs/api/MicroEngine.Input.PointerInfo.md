# Struct PointerInfo

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

A pointer the game can read: the mouse while it is over the window, or a finger or pen on the screen.

```csharp
public readonly struct PointerInfo : IEquatable<PointerInfo>
```

#### Implements

[IEquatable<PointerInfo\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Input_PointerInfo__ctor_MicroEngine_Input_DeviceId_System_Int64_MicroEngine_Input_PointerKind_System_Numerics_Vector2_System_Boolean_"></a> PointerInfo\(DeviceId, long, PointerKind, Vector2, bool\)

A pointer the game can read: the mouse while it is over the window, or a finger or pen on the screen.

```csharp
public PointerInfo(DeviceId Device, long Id, PointerKind Kind, Vector2 Position, bool Down)
```

#### Parameters

`Device` [DeviceId](MicroEngine.Input.DeviceId.md)

The device it belongs to.

`Id` [long](https://learn.microsoft.com/dotnet/api/system.int64)

0 for the mouse, the finger's id for touches.

`Kind` [PointerKind](MicroEngine.Input.PointerKind.md)

Mouse, touch or pen.

`Position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

Where it is, in virtual canvas pixels.

`Down` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether its primary button is held (a finger or pen is always down while it touches). Also true in the update after
a press that started and ended within one frame, so a quick tap is never lost.

## Properties

### <a id="MicroEngine_Input_PointerInfo_Device"></a> Device

The device it belongs to.

```csharp
public DeviceId Device { get; init; }
```

#### Property Value

 [DeviceId](MicroEngine.Input.DeviceId.md)

### <a id="MicroEngine_Input_PointerInfo_Down"></a> Down

Whether its primary button is held (a finger or pen is always down while it touches). Also true in the update after
a press that started and ended within one frame, so a quick tap is never lost.

```csharp
public bool Down { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_PointerInfo_Id"></a> Id

0 for the mouse, the finger's id for touches.

```csharp
public long Id { get; init; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Input_PointerInfo_Kind"></a> Kind

Mouse, touch or pen.

```csharp
public PointerKind Kind { get; init; }
```

#### Property Value

 [PointerKind](MicroEngine.Input.PointerKind.md)

### <a id="MicroEngine_Input_PointerInfo_Position"></a> Position

Where it is, in virtual canvas pixels.

```csharp
public Vector2 Position { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

## Methods

### <a id="MicroEngine_Input_PointerInfo_Deconstruct_MicroEngine_Input_DeviceId__System_Int64__MicroEngine_Input_PointerKind__System_Numerics_Vector2__System_Boolean__"></a> Deconstruct\(out DeviceId, out long, out PointerKind, out Vector2, out bool\)

```csharp
public void Deconstruct(out DeviceId Device, out long Id, out PointerKind Kind, out Vector2 Position, out bool Down)
```

#### Parameters

`Device` [DeviceId](MicroEngine.Input.DeviceId.md)

`Id` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`Kind` [PointerKind](MicroEngine.Input.PointerKind.md)

`Position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`Down` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_PointerInfo_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_PointerInfo_Equals_MicroEngine_Input_PointerInfo_"></a> Equals\(PointerInfo\)

```csharp
public bool Equals(PointerInfo other)
```

#### Parameters

`other` [PointerInfo](MicroEngine.Input.PointerInfo.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_PointerInfo_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Input_PointerInfo_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Input_PointerInfo_op_Equality_MicroEngine_Input_PointerInfo_MicroEngine_Input_PointerInfo_"></a> operator ==\(PointerInfo, PointerInfo\)

```csharp
public static bool operator ==(PointerInfo left, PointerInfo right)
```

#### Parameters

`left` [PointerInfo](MicroEngine.Input.PointerInfo.md)

`right` [PointerInfo](MicroEngine.Input.PointerInfo.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_PointerInfo_op_Inequality_MicroEngine_Input_PointerInfo_MicroEngine_Input_PointerInfo_"></a> operator \!=\(PointerInfo, PointerInfo\)

```csharp
public static bool operator !=(PointerInfo left, PointerInfo right)
```

#### Parameters

`left` [PointerInfo](MicroEngine.Input.PointerInfo.md)

`right` [PointerInfo](MicroEngine.Input.PointerInfo.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
