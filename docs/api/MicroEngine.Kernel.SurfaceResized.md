# Struct SurfaceResized

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Kernel](MicroEngine.Kernel.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Modules and the frame schedule](../manual/lifecycle.md#modules-and-the-frame-schedule)  

The new size of the drawing surface, in pixels ([Resized](MicroEngine.Kernel.IEngineLifecycle.md#MicroEngine_Kernel_IEngineLifecycle_Resized)).

```csharp
public readonly struct SurfaceResized : IEquatable<SurfaceResized>
```

#### Implements

[IEquatable<SurfaceResized\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Kernel_SurfaceResized__ctor_System_Int32_System_Int32_"></a> SurfaceResized\(int, int\)

The new size of the drawing surface, in pixels ([Resized](MicroEngine.Kernel.IEngineLifecycle.md#MicroEngine_Kernel_IEngineLifecycle_Resized)).

```csharp
public SurfaceResized(int Width, int Height)
```

#### Parameters

`Width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Width in pixels.

`Height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Height in pixels.

## Properties

### <a id="MicroEngine_Kernel_SurfaceResized_Height"></a> Height

Height in pixels.

```csharp
public int Height { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Kernel_SurfaceResized_Width"></a> Width

Width in pixels.

```csharp
public int Width { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Kernel_SurfaceResized_Deconstruct_System_Int32__System_Int32__"></a> Deconstruct\(out int, out int\)

```csharp
public void Deconstruct(out int Width, out int Height)
```

#### Parameters

`Width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Kernel_SurfaceResized_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Kernel_SurfaceResized_Equals_MicroEngine_Kernel_SurfaceResized_"></a> Equals\(SurfaceResized\)

```csharp
public bool Equals(SurfaceResized other)
```

#### Parameters

`other` [SurfaceResized](MicroEngine.Kernel.SurfaceResized.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Kernel_SurfaceResized_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Kernel_SurfaceResized_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Kernel_SurfaceResized_op_Equality_MicroEngine_Kernel_SurfaceResized_MicroEngine_Kernel_SurfaceResized_"></a> operator ==\(SurfaceResized, SurfaceResized\)

```csharp
public static bool operator ==(SurfaceResized left, SurfaceResized right)
```

#### Parameters

`left` [SurfaceResized](MicroEngine.Kernel.SurfaceResized.md)

`right` [SurfaceResized](MicroEngine.Kernel.SurfaceResized.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Kernel_SurfaceResized_op_Inequality_MicroEngine_Kernel_SurfaceResized_MicroEngine_Kernel_SurfaceResized_"></a> operator \!=\(SurfaceResized, SurfaceResized\)

```csharp
public static bool operator !=(SurfaceResized left, SurfaceResized right)
```

#### Parameters

`left` [SurfaceResized](MicroEngine.Kernel.SurfaceResized.md)

`right` [SurfaceResized](MicroEngine.Kernel.SurfaceResized.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
