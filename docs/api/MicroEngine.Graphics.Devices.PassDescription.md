# Struct PassDescription

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

A render pass: one colour attachment and an optional depth attachment (several colour attachments are a later,
additive extension). Viewport and scissor start as the whole colour target; bindings start empty.

```csharp
public readonly struct PassDescription : IEquatable<PassDescription>
```

#### Implements

[IEquatable<PassDescription\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_Devices_PassDescription__ctor_MicroEngine_Graphics_Devices_ColorAttachment_System_Nullable_MicroEngine_Graphics_Devices_DepthAttachment__"></a> PassDescription\(ColorAttachment, DepthAttachment?\)

A render pass: one colour attachment and an optional depth attachment (several colour attachments are a later,
additive extension). Viewport and scissor start as the whole colour target; bindings start empty.

```csharp
public PassDescription(ColorAttachment Color, DepthAttachment? Depth = null)
```

#### Parameters

`Color` [ColorAttachment](MicroEngine.Graphics.Devices.ColorAttachment.md)

Colour attachment.

`Depth` [DepthAttachment](MicroEngine.Graphics.Devices.DepthAttachment.md)?

Depth attachment, or null.

## Properties

### <a id="MicroEngine_Graphics_Devices_PassDescription_Color"></a> Color

Colour attachment.

```csharp
public ColorAttachment Color { get; init; }
```

#### Property Value

 [ColorAttachment](MicroEngine.Graphics.Devices.ColorAttachment.md)

### <a id="MicroEngine_Graphics_Devices_PassDescription_Depth"></a> Depth

Depth attachment, or null.

```csharp
public DepthAttachment? Depth { get; init; }
```

#### Property Value

 [DepthAttachment](MicroEngine.Graphics.Devices.DepthAttachment.md)?

## Methods

### <a id="MicroEngine_Graphics_Devices_PassDescription_Deconstruct_MicroEngine_Graphics_Devices_ColorAttachment__System_Nullable_MicroEngine_Graphics_Devices_DepthAttachment___"></a> Deconstruct\(out ColorAttachment, out DepthAttachment?\)

```csharp
public void Deconstruct(out ColorAttachment Color, out DepthAttachment? Depth)
```

#### Parameters

`Color` [ColorAttachment](MicroEngine.Graphics.Devices.ColorAttachment.md)

`Depth` [DepthAttachment](MicroEngine.Graphics.Devices.DepthAttachment.md)?

### <a id="MicroEngine_Graphics_Devices_PassDescription_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_PassDescription_Equals_MicroEngine_Graphics_Devices_PassDescription_"></a> Equals\(PassDescription\)

```csharp
public bool Equals(PassDescription other)
```

#### Parameters

`other` [PassDescription](MicroEngine.Graphics.Devices.PassDescription.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_PassDescription_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_PassDescription_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_Devices_PassDescription_op_Equality_MicroEngine_Graphics_Devices_PassDescription_MicroEngine_Graphics_Devices_PassDescription_"></a> operator ==\(PassDescription, PassDescription\)

```csharp
public static bool operator ==(PassDescription left, PassDescription right)
```

#### Parameters

`left` [PassDescription](MicroEngine.Graphics.Devices.PassDescription.md)

`right` [PassDescription](MicroEngine.Graphics.Devices.PassDescription.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_PassDescription_op_Inequality_MicroEngine_Graphics_Devices_PassDescription_MicroEngine_Graphics_Devices_PassDescription_"></a> operator \!=\(PassDescription, PassDescription\)

```csharp
public static bool operator !=(PassDescription left, PassDescription right)
```

#### Parameters

`left` [PassDescription](MicroEngine.Graphics.Devices.PassDescription.md)

`right` [PassDescription](MicroEngine.Graphics.Devices.PassDescription.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
