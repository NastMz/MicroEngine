# Class FrameSummary

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

What a profiled run looked like. [MotionError](MicroEngine.Core.FrameSummary.md#MicroEngine_Core_FrameSummary_MotionError) is the measure of uneven motion: for every frame, how far
the game time it advanced differs from the wall time that passed, so a frame that repeats the previous picture or
jumps two steps counts even when every frame took the same time.

```csharp
public sealed record FrameSummary : IEquatable<FrameSummary>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[FrameSummary](MicroEngine.Core.FrameSummary.md)

#### Implements

[IEquatable<FrameSummary\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Core_FrameSummary__ctor_System_Int32_MicroEngine_Core_Percentiles_MicroEngine_Core_Percentiles_MicroEngine_Core_Percentiles_MicroEngine_Core_Percentiles_MicroEngine_Core_Percentiles_MicroEngine_Core_Percentiles_MicroEngine_Core_Percentiles_System_Collections_Generic_IReadOnlyList_System_Int32__System_Double_System_Int32_System_Int32_"></a> FrameSummary\(int, Percentiles, Percentiles, Percentiles, Percentiles, Percentiles, Percentiles, Percentiles, IReadOnlyList<int\>, double, int, int\)

What a profiled run looked like. [MotionError](MicroEngine.Core.FrameSummary.md#MicroEngine_Core_FrameSummary_MotionError) is the measure of uneven motion: for every frame, how far
the game time it advanced differs from the wall time that passed, so a frame that repeats the previous picture or
jumps two steps counts even when every frame took the same time.

```csharp
public FrameSummary(int Frames, Percentiles Interval, Percentiles Work, Percentiles Update, Percentiles Render, Percentiles Present, Percentiles MotionError, Percentiles InterpolatedMotionError, IReadOnlyList<int> StepsPerFrame, double AllocatedPerFrame, int Collections, int Hitches)
```

#### Parameters

`Frames` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Frames summarized.

`Interval` [Percentiles](MicroEngine.Core.Percentiles.md)

Time between frame starts.

`Work` [Percentiles](MicroEngine.Core.Percentiles.md)

Events, update and render: the CPU time of a frame before presenting.

`Update` [Percentiles](MicroEngine.Core.Percentiles.md)

Fixed steps.

`Render` [Percentiles](MicroEngine.Core.Percentiles.md)

Drawing.

`Present` [Percentiles](MicroEngine.Core.Percentiles.md)

Presenting (the VSync wait, when on).

`MotionError` [Percentiles](MicroEngine.Core.Percentiles.md)

|game time advanced − time the frame stood for| per frame, drawing the last step. The time
            a frame stood for is the whole number of refresh periods nearest to its interval when the display rate is known
            ([DisplayRate](MicroEngine.Core.FrameProfiler.md#MicroEngine_Core_FrameProfiler_DisplayRate)), its measured interval otherwise.

`InterpolatedMotionError` [Percentiles](MicroEngine.Core.Percentiles.md)

The same for a game that draws with [FrameBlend](MicroEngine.Core.Game.md#MicroEngine_Core_Game_FrameBlend).

`StepsPerFrame` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[int](https://learn.microsoft.com/dotnet/api/system.int32)\>

How many frames ran 0, 1, 2... steps (index = steps).

`AllocatedPerFrame` [double](https://learn.microsoft.com/dotnet/api/system.double)

Mean bytes allocated per frame.

`Collections` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Garbage collections during the frames.

`Hitches` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Frames whose interval exceeded 1.5 times the median interval.

## Properties

### <a id="MicroEngine_Core_FrameSummary_AllocatedPerFrame"></a> AllocatedPerFrame

Mean bytes allocated per frame.

```csharp
public double AllocatedPerFrame { get; init; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Core_FrameSummary_Collections"></a> Collections

Garbage collections during the frames.

```csharp
public int Collections { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_FrameSummary_Frames"></a> Frames

Frames summarized.

```csharp
public int Frames { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_FrameSummary_Hitches"></a> Hitches

Frames whose interval exceeded 1.5 times the median interval.

```csharp
public int Hitches { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_FrameSummary_InterpolatedMotionError"></a> InterpolatedMotionError

The same for a game that draws with [FrameBlend](MicroEngine.Core.Game.md#MicroEngine_Core_Game_FrameBlend).

```csharp
public Percentiles InterpolatedMotionError { get; init; }
```

#### Property Value

 [Percentiles](MicroEngine.Core.Percentiles.md)

### <a id="MicroEngine_Core_FrameSummary_Interval"></a> Interval

Time between frame starts.

```csharp
public Percentiles Interval { get; init; }
```

#### Property Value

 [Percentiles](MicroEngine.Core.Percentiles.md)

### <a id="MicroEngine_Core_FrameSummary_MotionError"></a> MotionError

|game time advanced − time the frame stood for| per frame, drawing the last step. The time
            a frame stood for is the whole number of refresh periods nearest to its interval when the display rate is known
            ([DisplayRate](MicroEngine.Core.FrameProfiler.md#MicroEngine_Core_FrameProfiler_DisplayRate)), its measured interval otherwise.

```csharp
public Percentiles MotionError { get; init; }
```

#### Property Value

 [Percentiles](MicroEngine.Core.Percentiles.md)

### <a id="MicroEngine_Core_FrameSummary_Present"></a> Present

Presenting (the VSync wait, when on).

```csharp
public Percentiles Present { get; init; }
```

#### Property Value

 [Percentiles](MicroEngine.Core.Percentiles.md)

### <a id="MicroEngine_Core_FrameSummary_Render"></a> Render

Drawing.

```csharp
public Percentiles Render { get; init; }
```

#### Property Value

 [Percentiles](MicroEngine.Core.Percentiles.md)

### <a id="MicroEngine_Core_FrameSummary_StepsPerFrame"></a> StepsPerFrame

How many frames ran 0, 1, 2... steps (index = steps).

```csharp
public IReadOnlyList<int> StepsPerFrame { get; init; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[int](https://learn.microsoft.com/dotnet/api/system.int32)\>

### <a id="MicroEngine_Core_FrameSummary_Update"></a> Update

Fixed steps.

```csharp
public Percentiles Update { get; init; }
```

#### Property Value

 [Percentiles](MicroEngine.Core.Percentiles.md)

### <a id="MicroEngine_Core_FrameSummary_Work"></a> Work

Events, update and render: the CPU time of a frame before presenting.

```csharp
public Percentiles Work { get; init; }
```

#### Property Value

 [Percentiles](MicroEngine.Core.Percentiles.md)

## Methods

### <a id="MicroEngine_Core_FrameSummary__Clone__"></a> <Clone\>$\(\)

```csharp
public FrameSummary <Clone>$()
```

#### Returns

 [FrameSummary](MicroEngine.Core.FrameSummary.md)

### <a id="MicroEngine_Core_FrameSummary_Deconstruct_System_Int32__MicroEngine_Core_Percentiles__MicroEngine_Core_Percentiles__MicroEngine_Core_Percentiles__MicroEngine_Core_Percentiles__MicroEngine_Core_Percentiles__MicroEngine_Core_Percentiles__MicroEngine_Core_Percentiles__System_Collections_Generic_IReadOnlyList_System_Int32___System_Double__System_Int32__System_Int32__"></a> Deconstruct\(out int, out Percentiles, out Percentiles, out Percentiles, out Percentiles, out Percentiles, out Percentiles, out Percentiles, out IReadOnlyList<int\>, out double, out int, out int\)

```csharp
public void Deconstruct(out int Frames, out Percentiles Interval, out Percentiles Work, out Percentiles Update, out Percentiles Render, out Percentiles Present, out Percentiles MotionError, out Percentiles InterpolatedMotionError, out IReadOnlyList<int> StepsPerFrame, out double AllocatedPerFrame, out int Collections, out int Hitches)
```

#### Parameters

`Frames` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Interval` [Percentiles](MicroEngine.Core.Percentiles.md)

`Work` [Percentiles](MicroEngine.Core.Percentiles.md)

`Update` [Percentiles](MicroEngine.Core.Percentiles.md)

`Render` [Percentiles](MicroEngine.Core.Percentiles.md)

`Present` [Percentiles](MicroEngine.Core.Percentiles.md)

`MotionError` [Percentiles](MicroEngine.Core.Percentiles.md)

`InterpolatedMotionError` [Percentiles](MicroEngine.Core.Percentiles.md)

`StepsPerFrame` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[int](https://learn.microsoft.com/dotnet/api/system.int32)\>

`AllocatedPerFrame` [double](https://learn.microsoft.com/dotnet/api/system.double)

`Collections` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Hitches` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_FrameSummary_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_FrameSummary_Equals_MicroEngine_Core_FrameSummary_"></a> Equals\(FrameSummary?\)

```csharp
public bool Equals(FrameSummary? other)
```

#### Parameters

`other` [FrameSummary](MicroEngine.Core.FrameSummary.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_FrameSummary_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_FrameSummary_ToString"></a> ToString\(\)

A few lines for a console.

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Core_FrameSummary_op_Equality_MicroEngine_Core_FrameSummary_MicroEngine_Core_FrameSummary_"></a> operator ==\(FrameSummary?, FrameSummary?\)

```csharp
public static bool operator ==(FrameSummary? left, FrameSummary? right)
```

#### Parameters

`left` [FrameSummary](MicroEngine.Core.FrameSummary.md)?

`right` [FrameSummary](MicroEngine.Core.FrameSummary.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_FrameSummary_op_Inequality_MicroEngine_Core_FrameSummary_MicroEngine_Core_FrameSummary_"></a> operator \!=\(FrameSummary?, FrameSummary?\)

```csharp
public static bool operator !=(FrameSummary? left, FrameSummary? right)
```

#### Parameters

`left` [FrameSummary](MicroEngine.Core.FrameSummary.md)?

`right` [FrameSummary](MicroEngine.Core.FrameSummary.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
