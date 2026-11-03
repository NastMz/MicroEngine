# Struct RectI

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

An integer rectangle in pixels: origin top-left, x to the right, y down; right and bottom exclusive.

```csharp
public readonly struct RectI : IEquatable<RectI>
```

#### Implements

[IEquatable<RectI\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_Devices_RectI__ctor_System_Int32_System_Int32_System_Int32_System_Int32_"></a> RectI\(int, int, int, int\)

An integer rectangle in pixels: origin top-left, x to the right, y down; right and bottom exclusive.

```csharp
public RectI(int X, int Y, int Width, int Height)
```

#### Parameters

`X` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Left edge (inclusive).

`Y` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Top edge (inclusive).

`Width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Width in pixels.

`Height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Height in pixels.

## Properties

### <a id="MicroEngine_Graphics_Devices_RectI_Bottom"></a> Bottom

Bottom edge (exclusive).

```csharp
public int Bottom { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_RectI_Height"></a> Height

Height in pixels.

```csharp
public int Height { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_RectI_IsEmpty"></a> IsEmpty

True when the rectangle covers no pixel.

```csharp
public bool IsEmpty { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_RectI_Right"></a> Right

Right edge (exclusive).

```csharp
public int Right { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_RectI_Width"></a> Width

Width in pixels.

```csharp
public int Width { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_RectI_X"></a> X

Left edge (inclusive).

```csharp
public int X { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_RectI_Y"></a> Y

Top edge (inclusive).

```csharp
public int Y { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Graphics_Devices_RectI_Contains_MicroEngine_Graphics_Devices_RectI_"></a> Contains\(RectI\)

True when <code class="paramref">other</code> lies entirely inside this rectangle.

```csharp
public bool Contains(RectI other)
```

#### Parameters

`other` [RectI](MicroEngine.Graphics.Devices.RectI.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_RectI_Deconstruct_System_Int32__System_Int32__System_Int32__System_Int32__"></a> Deconstruct\(out int, out int, out int, out int\)

```csharp
public void Deconstruct(out int X, out int Y, out int Width, out int Height)
```

#### Parameters

`X` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Y` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_RectI_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_RectI_Equals_MicroEngine_Graphics_Devices_RectI_"></a> Equals\(RectI\)

```csharp
public bool Equals(RectI other)
```

#### Parameters

`other` [RectI](MicroEngine.Graphics.Devices.RectI.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_RectI_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_RectI_Intersect_MicroEngine_Graphics_Devices_RectI_"></a> Intersect\(RectI\)

The overlap of two rectangles (empty when they do not overlap).

```csharp
public RectI Intersect(RectI other)
```

#### Parameters

`other` [RectI](MicroEngine.Graphics.Devices.RectI.md)

#### Returns

 [RectI](MicroEngine.Graphics.Devices.RectI.md)

### <a id="MicroEngine_Graphics_Devices_RectI_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_Devices_RectI_op_Equality_MicroEngine_Graphics_Devices_RectI_MicroEngine_Graphics_Devices_RectI_"></a> operator ==\(RectI, RectI\)

```csharp
public static bool operator ==(RectI left, RectI right)
```

#### Parameters

`left` [RectI](MicroEngine.Graphics.Devices.RectI.md)

`right` [RectI](MicroEngine.Graphics.Devices.RectI.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_RectI_op_Inequality_MicroEngine_Graphics_Devices_RectI_MicroEngine_Graphics_Devices_RectI_"></a> operator \!=\(RectI, RectI\)

```csharp
public static bool operator !=(RectI left, RectI right)
```

#### Parameters

`left` [RectI](MicroEngine.Graphics.Devices.RectI.md)

`right` [RectI](MicroEngine.Graphics.Devices.RectI.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
