# Struct DeviceId

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

Identifies one input device for as long as it is connected: a keyboard, a mouse, a touch screen, one gamepad. Ids
are chosen by whoever reports the device (a backend's own ids are positive; the engine's polled sources and touch
screen use negative ones) and are never reused while the game runs.

```csharp
public readonly struct DeviceId : IEquatable<DeviceId>
```

#### Implements

[IEquatable<DeviceId\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Input_DeviceId__ctor_System_Int32_"></a> DeviceId\(int\)

Identifies one input device for as long as it is connected: a keyboard, a mouse, a touch screen, one gamepad. Ids
are chosen by whoever reports the device (a backend's own ids are positive; the engine's polled sources and touch
screen use negative ones) and are never reused while the game runs.

```csharp
public DeviceId(int Value)
```

#### Parameters

`Value` [int](https://learn.microsoft.com/dotnet/api/system.int32)

The id; 0 is [None](MicroEngine.Input.DeviceId.md#MicroEngine_Input_DeviceId_None).

## Properties

### <a id="MicroEngine_Input_DeviceId_None"></a> None

No device (scripted actions, sources that are not devices).

```csharp
public static DeviceId None { get; }
```

#### Property Value

 [DeviceId](MicroEngine.Input.DeviceId.md)

### <a id="MicroEngine_Input_DeviceId_TouchScreen"></a> TouchScreen

The touch screen the platform offers (its fingers become pointers of this device).

```csharp
public static DeviceId TouchScreen { get; }
```

#### Property Value

 [DeviceId](MicroEngine.Input.DeviceId.md)

### <a id="MicroEngine_Input_DeviceId_Value"></a> Value

The id; 0 is [None](MicroEngine.Input.DeviceId.md#MicroEngine_Input_DeviceId_None).

```csharp
public int Value { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Input_DeviceId_Deconstruct_System_Int32__"></a> Deconstruct\(out int\)

```csharp
public void Deconstruct(out int Value)
```

#### Parameters

`Value` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Input_DeviceId_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_DeviceId_Equals_MicroEngine_Input_DeviceId_"></a> Equals\(DeviceId\)

```csharp
public bool Equals(DeviceId other)
```

#### Parameters

`other` [DeviceId](MicroEngine.Input.DeviceId.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_DeviceId_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Input_DeviceId_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Input_DeviceId_op_Equality_MicroEngine_Input_DeviceId_MicroEngine_Input_DeviceId_"></a> operator ==\(DeviceId, DeviceId\)

```csharp
public static bool operator ==(DeviceId left, DeviceId right)
```

#### Parameters

`left` [DeviceId](MicroEngine.Input.DeviceId.md)

`right` [DeviceId](MicroEngine.Input.DeviceId.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_DeviceId_op_Inequality_MicroEngine_Input_DeviceId_MicroEngine_Input_DeviceId_"></a> operator \!=\(DeviceId, DeviceId\)

```csharp
public static bool operator !=(DeviceId left, DeviceId right)
```

#### Parameters

`left` [DeviceId](MicroEngine.Input.DeviceId.md)

`right` [DeviceId](MicroEngine.Input.DeviceId.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
