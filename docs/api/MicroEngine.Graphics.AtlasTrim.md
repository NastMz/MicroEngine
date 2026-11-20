# Struct AtlasTrim

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

What trimming cut from a sprite: where the kept pixels lay in the whole image, and both sizes.

```csharp
public readonly struct AtlasTrim : IEquatable<AtlasTrim>
```

#### Implements

[IEquatable<AtlasTrim\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_AtlasTrim__ctor_System_Int32_System_Int32_System_Int32_System_Int32_System_Int32_System_Int32_"></a> AtlasTrim\(int, int, int, int, int, int\)

What trimming cut from a sprite: where the kept pixels lay in the whole image, and both sizes.

```csharp
public AtlasTrim(int X, int Y, int SourceWidth, int SourceHeight, int Width, int Height)
```

#### Parameters

`X` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Left edge of the kept pixels in the whole image.

`Y` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Top edge of the kept pixels in the whole image.

`SourceWidth` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Width of the whole image.

`SourceHeight` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Height of the whole image.

`Width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Width of the kept pixels.

`Height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Height of the kept pixels.

## Properties

### <a id="MicroEngine_Graphics_AtlasTrim_Height"></a> Height

Height of the kept pixels.

```csharp
public int Height { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_AtlasTrim_SourceHeight"></a> SourceHeight

Height of the whole image.

```csharp
public int SourceHeight { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_AtlasTrim_SourceWidth"></a> SourceWidth

Width of the whole image.

```csharp
public int SourceWidth { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_AtlasTrim_Width"></a> Width

Width of the kept pixels.

```csharp
public int Width { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_AtlasTrim_X"></a> X

Left edge of the kept pixels in the whole image.

```csharp
public int X { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_AtlasTrim_Y"></a> Y

Top edge of the kept pixels in the whole image.

```csharp
public int Y { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Graphics_AtlasTrim_Deconstruct_System_Int32__System_Int32__System_Int32__System_Int32__System_Int32__System_Int32__"></a> Deconstruct\(out int, out int, out int, out int, out int, out int\)

```csharp
public void Deconstruct(out int X, out int Y, out int SourceWidth, out int SourceHeight, out int Width, out int Height)
```

#### Parameters

`X` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Y` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`SourceWidth` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`SourceHeight` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_AtlasTrim_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_AtlasTrim_Equals_MicroEngine_Graphics_AtlasTrim_"></a> Equals\(AtlasTrim\)

```csharp
public bool Equals(AtlasTrim other)
```

#### Parameters

`other` [AtlasTrim](MicroEngine.Graphics.AtlasTrim.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_AtlasTrim_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_AtlasTrim_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_AtlasTrim_op_Equality_MicroEngine_Graphics_AtlasTrim_MicroEngine_Graphics_AtlasTrim_"></a> operator ==\(AtlasTrim, AtlasTrim\)

```csharp
public static bool operator ==(AtlasTrim left, AtlasTrim right)
```

#### Parameters

`left` [AtlasTrim](MicroEngine.Graphics.AtlasTrim.md)

`right` [AtlasTrim](MicroEngine.Graphics.AtlasTrim.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_AtlasTrim_op_Inequality_MicroEngine_Graphics_AtlasTrim_MicroEngine_Graphics_AtlasTrim_"></a> operator \!=\(AtlasTrim, AtlasTrim\)

```csharp
public static bool operator !=(AtlasTrim left, AtlasTrim right)
```

#### Parameters

`left` [AtlasTrim](MicroEngine.Graphics.AtlasTrim.md)

`right` [AtlasTrim](MicroEngine.Graphics.AtlasTrim.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
