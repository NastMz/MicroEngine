# Struct BufferDescription

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

A buffer, available on every tier (tier 0: device-owned CPU memory submitted to the backend's geometry call; tier 1:
GPU memory). Contents are written only by [UpdateBuffer](MicroEngine.Graphics.Devices.CommandList.md#MicroEngine_Graphics_Devices_CommandList_UpdateBuffer_MicroEngine_Graphics_Devices_GpuBuffer_System_Int32_System_ReadOnlySpan_System_Byte__) and are undefined until written.

```csharp
public readonly struct BufferDescription : IEquatable<BufferDescription>
```

#### Implements

[IEquatable<BufferDescription\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_Devices_BufferDescription__ctor_System_Int32_MicroEngine_Graphics_Devices_BufferUsage_System_String_"></a> BufferDescription\(int, BufferUsage, string?\)

A buffer, available on every tier (tier 0: device-owned CPU memory submitted to the backend's geometry call; tier 1:
GPU memory). Contents are written only by [UpdateBuffer](MicroEngine.Graphics.Devices.CommandList.md#MicroEngine_Graphics_Devices_CommandList_UpdateBuffer_MicroEngine_Graphics_Devices_GpuBuffer_System_Int32_System_ReadOnlySpan_System_Byte__) and are undefined until written.

```csharp
public BufferDescription(int SizeInBytes, BufferUsage Usage, string? Name = null)
```

#### Parameters

`SizeInBytes` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Size, at least 1.

`Usage` [BufferUsage](MicroEngine.Graphics.Devices.BufferUsage.md)

Vertex and/or index.

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)?

Debug name.

## Properties

### <a id="MicroEngine_Graphics_Devices_BufferDescription_Name"></a> Name

Debug name.

```csharp
public string? Name { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Graphics_Devices_BufferDescription_SizeInBytes"></a> SizeInBytes

Size, at least 1.

```csharp
public int SizeInBytes { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_BufferDescription_Usage"></a> Usage

Vertex and/or index.

```csharp
public BufferUsage Usage { get; init; }
```

#### Property Value

 [BufferUsage](MicroEngine.Graphics.Devices.BufferUsage.md)

## Methods

### <a id="MicroEngine_Graphics_Devices_BufferDescription_Deconstruct_System_Int32__MicroEngine_Graphics_Devices_BufferUsage__System_String__"></a> Deconstruct\(out int, out BufferUsage, out string?\)

```csharp
public void Deconstruct(out int SizeInBytes, out BufferUsage Usage, out string? Name)
```

#### Parameters

`SizeInBytes` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Usage` [BufferUsage](MicroEngine.Graphics.Devices.BufferUsage.md)

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Graphics_Devices_BufferDescription_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_BufferDescription_Equals_MicroEngine_Graphics_Devices_BufferDescription_"></a> Equals\(BufferDescription\)

```csharp
public bool Equals(BufferDescription other)
```

#### Parameters

`other` [BufferDescription](MicroEngine.Graphics.Devices.BufferDescription.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_BufferDescription_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_BufferDescription_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_Devices_BufferDescription_op_Equality_MicroEngine_Graphics_Devices_BufferDescription_MicroEngine_Graphics_Devices_BufferDescription_"></a> operator ==\(BufferDescription, BufferDescription\)

```csharp
public static bool operator ==(BufferDescription left, BufferDescription right)
```

#### Parameters

`left` [BufferDescription](MicroEngine.Graphics.Devices.BufferDescription.md)

`right` [BufferDescription](MicroEngine.Graphics.Devices.BufferDescription.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_BufferDescription_op_Inequality_MicroEngine_Graphics_Devices_BufferDescription_MicroEngine_Graphics_Devices_BufferDescription_"></a> operator \!=\(BufferDescription, BufferDescription\)

```csharp
public static bool operator !=(BufferDescription left, BufferDescription right)
```

#### Parameters

`left` [BufferDescription](MicroEngine.Graphics.Devices.BufferDescription.md)

`right` [BufferDescription](MicroEngine.Graphics.Devices.BufferDescription.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
