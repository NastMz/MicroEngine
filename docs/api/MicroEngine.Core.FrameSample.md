# Struct FrameSample

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

One frame as [FrameProfiler](MicroEngine.Core.FrameProfiler.md) measured it. Times are in milliseconds.

```csharp
public readonly struct FrameSample : IEquatable<FrameSample>
```

#### Implements

[IEquatable<FrameSample\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Core_FrameSample__ctor_System_Double_System_Double_System_Double_System_Double_System_Double_System_Int32_System_Double_System_Double_System_Int64_System_Int32_"></a> FrameSample\(double, double, double, double, double, int, double, double, long, int\)

One frame as [FrameProfiler](MicroEngine.Core.FrameProfiler.md) measured it. Times are in milliseconds.

```csharp
public FrameSample(double Interval, double Events, double Update, double Render, double Present, int Steps, double SimulatedTime, double InterpolatedTime, long Allocated, int Collections)
```

#### Parameters

`Interval` [double](https://learn.microsoft.com/dotnet/api/system.double)

Time from the start of the previous frame to the start of this one (0 for the first frame).

`Events` [double](https://learn.microsoft.com/dotnet/api/system.double)

Polling system events ([PollEvents](MicroEngine.Core.IPlatform.md#MicroEngine_Core_IPlatform_PollEvents)).

`Update` [double](https://learn.microsoft.com/dotnet/api/system.double)

Running the fixed steps of this frame.

`Render` [double](https://learn.microsoft.com/dotnet/api/system.double)

The game drawing its frame ([BeginFrame](MicroEngine.Core.IPlatform.md#MicroEngine_Core_IPlatform_BeginFrame) and the scenes).

`Present` [double](https://learn.microsoft.com/dotnet/api/system.double)

Finishing and presenting the frame ([EndFrame](MicroEngine.Core.IPlatform.md#MicroEngine_Core_IPlatform_EndFrame)); with VSync on, this is
            mostly the wait for the display.

`Steps` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Fixed steps run in this frame.

`SimulatedTime` [double](https://learn.microsoft.com/dotnet/api/system.double)

Seconds of game time after the frame's last step ([Time](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Time)).

`InterpolatedTime` [double](https://learn.microsoft.com/dotnet/api/system.double)

Seconds of game time a game drawing with [FrameBlend](MicroEngine.Core.Game.md#MicroEngine_Core_Game_FrameBlend) shows.

`Allocated` [long](https://learn.microsoft.com/dotnet/api/system.int64)

Bytes the loop's thread allocated during the frame.

`Collections` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Garbage collections (any generation) that happened during the frame.

## Properties

### <a id="MicroEngine_Core_FrameSample_Allocated"></a> Allocated

Bytes the loop's thread allocated during the frame.

```csharp
public long Allocated { get; init; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Core_FrameSample_Collections"></a> Collections

Garbage collections (any generation) that happened during the frame.

```csharp
public int Collections { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_FrameSample_Events"></a> Events

Polling system events ([PollEvents](MicroEngine.Core.IPlatform.md#MicroEngine_Core_IPlatform_PollEvents)).

```csharp
public double Events { get; init; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Core_FrameSample_InterpolatedTime"></a> InterpolatedTime

Seconds of game time a game drawing with [FrameBlend](MicroEngine.Core.Game.md#MicroEngine_Core_Game_FrameBlend) shows.

```csharp
public double InterpolatedTime { get; init; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Core_FrameSample_Interval"></a> Interval

Time from the start of the previous frame to the start of this one (0 for the first frame).

```csharp
public double Interval { get; init; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Core_FrameSample_Present"></a> Present

Finishing and presenting the frame ([EndFrame](MicroEngine.Core.IPlatform.md#MicroEngine_Core_IPlatform_EndFrame)); with VSync on, this is
            mostly the wait for the display.

```csharp
public double Present { get; init; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Core_FrameSample_Render"></a> Render

The game drawing its frame ([BeginFrame](MicroEngine.Core.IPlatform.md#MicroEngine_Core_IPlatform_BeginFrame) and the scenes).

```csharp
public double Render { get; init; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Core_FrameSample_SimulatedTime"></a> SimulatedTime

Seconds of game time after the frame's last step ([Time](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Time)).

```csharp
public double SimulatedTime { get; init; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Core_FrameSample_Steps"></a> Steps

Fixed steps run in this frame.

```csharp
public int Steps { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_FrameSample_Update"></a> Update

Running the fixed steps of this frame.

```csharp
public double Update { get; init; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

## Methods

### <a id="MicroEngine_Core_FrameSample_Deconstruct_System_Double__System_Double__System_Double__System_Double__System_Double__System_Int32__System_Double__System_Double__System_Int64__System_Int32__"></a> Deconstruct\(out double, out double, out double, out double, out double, out int, out double, out double, out long, out int\)

```csharp
public void Deconstruct(out double Interval, out double Events, out double Update, out double Render, out double Present, out int Steps, out double SimulatedTime, out double InterpolatedTime, out long Allocated, out int Collections)
```

#### Parameters

`Interval` [double](https://learn.microsoft.com/dotnet/api/system.double)

`Events` [double](https://learn.microsoft.com/dotnet/api/system.double)

`Update` [double](https://learn.microsoft.com/dotnet/api/system.double)

`Render` [double](https://learn.microsoft.com/dotnet/api/system.double)

`Present` [double](https://learn.microsoft.com/dotnet/api/system.double)

`Steps` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`SimulatedTime` [double](https://learn.microsoft.com/dotnet/api/system.double)

`InterpolatedTime` [double](https://learn.microsoft.com/dotnet/api/system.double)

`Allocated` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`Collections` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_FrameSample_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_FrameSample_Equals_MicroEngine_Core_FrameSample_"></a> Equals\(FrameSample\)

```csharp
public bool Equals(FrameSample other)
```

#### Parameters

`other` [FrameSample](MicroEngine.Core.FrameSample.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_FrameSample_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_FrameSample_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Core_FrameSample_op_Equality_MicroEngine_Core_FrameSample_MicroEngine_Core_FrameSample_"></a> operator ==\(FrameSample, FrameSample\)

```csharp
public static bool operator ==(FrameSample left, FrameSample right)
```

#### Parameters

`left` [FrameSample](MicroEngine.Core.FrameSample.md)

`right` [FrameSample](MicroEngine.Core.FrameSample.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_FrameSample_op_Inequality_MicroEngine_Core_FrameSample_MicroEngine_Core_FrameSample_"></a> operator \!=\(FrameSample, FrameSample\)

```csharp
public static bool operator !=(FrameSample left, FrameSample right)
```

#### Parameters

`left` [FrameSample](MicroEngine.Core.FrameSample.md)

`right` [FrameSample](MicroEngine.Core.FrameSample.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
