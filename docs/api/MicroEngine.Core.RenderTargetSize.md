# Struct RenderTargetSize

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

The size of a render target of the assets (the options of a "render target" asset key).

```csharp
public readonly struct RenderTargetSize : IEquatable<RenderTargetSize>
```

#### Implements

[IEquatable<RenderTargetSize\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Core_RenderTargetSize__ctor_System_Int32_System_Int32_"></a> RenderTargetSize\(int, int\)

The size of a render target of the assets (the options of a "render target" asset key).

```csharp
public RenderTargetSize(int Width, int Height)
```

#### Parameters

`Width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Width in pixels.

`Height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Height in pixels.

## Properties

### <a id="MicroEngine_Core_RenderTargetSize_Height"></a> Height

Height in pixels.

```csharp
public int Height { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_RenderTargetSize_Width"></a> Width

Width in pixels.

```csharp
public int Width { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Core_RenderTargetSize_Deconstruct_System_Int32__System_Int32__"></a> Deconstruct\(out int, out int\)

```csharp
public void Deconstruct(out int Width, out int Height)
```

#### Parameters

`Width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_RenderTargetSize_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_RenderTargetSize_Equals_MicroEngine_Core_RenderTargetSize_"></a> Equals\(RenderTargetSize\)

```csharp
public bool Equals(RenderTargetSize other)
```

#### Parameters

`other` [RenderTargetSize](MicroEngine.Core.RenderTargetSize.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_RenderTargetSize_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_RenderTargetSize_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Core_RenderTargetSize_op_Equality_MicroEngine_Core_RenderTargetSize_MicroEngine_Core_RenderTargetSize_"></a> operator ==\(RenderTargetSize, RenderTargetSize\)

```csharp
public static bool operator ==(RenderTargetSize left, RenderTargetSize right)
```

#### Parameters

`left` [RenderTargetSize](MicroEngine.Core.RenderTargetSize.md)

`right` [RenderTargetSize](MicroEngine.Core.RenderTargetSize.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_RenderTargetSize_op_Inequality_MicroEngine_Core_RenderTargetSize_MicroEngine_Core_RenderTargetSize_"></a> operator \!=\(RenderTargetSize, RenderTargetSize\)

```csharp
public static bool operator !=(RenderTargetSize left, RenderTargetSize right)
```

#### Parameters

`left` [RenderTargetSize](MicroEngine.Core.RenderTargetSize.md)

`right` [RenderTargetSize](MicroEngine.Core.RenderTargetSize.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
