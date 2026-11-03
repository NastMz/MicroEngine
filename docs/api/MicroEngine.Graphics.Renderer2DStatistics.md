# Struct Renderer2DStatistics

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

Deterministic counts of the last batch ([Statistics](MicroEngine.Graphics.IRenderer2D.md#MicroEngine_Graphics_IRenderer2D_Statistics)), the same on every device and run.

```csharp
public readonly struct Renderer2DStatistics : IEquatable<Renderer2DStatistics>
```

#### Implements

[IEquatable<Renderer2DStatistics\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_Renderer2DStatistics__ctor_System_Int32_System_Int32_System_Int32_"></a> Renderer2DStatistics\(int, int, int\)

Deterministic counts of the last batch ([Statistics](MicroEngine.Graphics.IRenderer2D.md#MicroEngine_Graphics_IRenderer2D_Statistics)), the same on every device and run.

```csharp
public Renderer2DStatistics(int Sprites = 0, int DrawCalls = 0, int Flushes = 0)
```

#### Parameters

`Sprites` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Draws recorded (<code>Draw</code> and <code>DrawGeometry</code> calls).

`DrawCalls` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Indexed draws sent to the device.

`Flushes` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Command lists submitted (one per <code>End</code>, plus one per <code>Clear</code> or <code>Flush</code> that had work).

## Properties

### <a id="MicroEngine_Graphics_Renderer2DStatistics_DrawCalls"></a> DrawCalls

Indexed draws sent to the device.

```csharp
public int DrawCalls { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Renderer2DStatistics_Flushes"></a> Flushes

Command lists submitted (one per <code>End</code>, plus one per <code>Clear</code> or <code>Flush</code> that had work).

```csharp
public int Flushes { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Renderer2DStatistics_Sprites"></a> Sprites

Draws recorded (<code>Draw</code> and <code>DrawGeometry</code> calls).

```csharp
public int Sprites { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Graphics_Renderer2DStatistics_Deconstruct_System_Int32__System_Int32__System_Int32__"></a> Deconstruct\(out int, out int, out int\)

```csharp
public void Deconstruct(out int Sprites, out int DrawCalls, out int Flushes)
```

#### Parameters

`Sprites` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`DrawCalls` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Flushes` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Renderer2DStatistics_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Renderer2DStatistics_Equals_MicroEngine_Graphics_Renderer2DStatistics_"></a> Equals\(Renderer2DStatistics\)

```csharp
public bool Equals(Renderer2DStatistics other)
```

#### Parameters

`other` [Renderer2DStatistics](MicroEngine.Graphics.Renderer2DStatistics.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Renderer2DStatistics_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Renderer2DStatistics_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_Renderer2DStatistics_op_Equality_MicroEngine_Graphics_Renderer2DStatistics_MicroEngine_Graphics_Renderer2DStatistics_"></a> operator ==\(Renderer2DStatistics, Renderer2DStatistics\)

```csharp
public static bool operator ==(Renderer2DStatistics left, Renderer2DStatistics right)
```

#### Parameters

`left` [Renderer2DStatistics](MicroEngine.Graphics.Renderer2DStatistics.md)

`right` [Renderer2DStatistics](MicroEngine.Graphics.Renderer2DStatistics.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Renderer2DStatistics_op_Inequality_MicroEngine_Graphics_Renderer2DStatistics_MicroEngine_Graphics_Renderer2DStatistics_"></a> operator \!=\(Renderer2DStatistics, Renderer2DStatistics\)

```csharp
public static bool operator !=(Renderer2DStatistics left, Renderer2DStatistics right)
```

#### Parameters

`left` [Renderer2DStatistics](MicroEngine.Graphics.Renderer2DStatistics.md)

`right` [Renderer2DStatistics](MicroEngine.Graphics.Renderer2DStatistics.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
