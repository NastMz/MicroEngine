# Struct FrameStatistics

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

What a frame cost, as [LastFrame](MicroEngine.Core.GameDiagnostics.md#MicroEngine_Core_GameDiagnostics_LastFrame) reports it. Times are in milliseconds of real time.

```csharp
public readonly struct FrameStatistics : IEquatable<FrameStatistics>
```

#### Implements

[IEquatable<FrameStatistics\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Core_FrameStatistics__ctor_System_Int64_System_Double_System_Double_System_Double_System_Double_System_Double_System_Int32_System_Int64_System_Int32_System_Int32_System_Int32_MicroEngine_Graphics_Devices_GraphicsCounters_MicroEngine_Audio_AudioStatistics_"></a> FrameStatistics\(long, double, double, double, double, double, int, long, int, int, int, GraphicsCounters, AudioStatistics\)

What a frame cost, as [LastFrame](MicroEngine.Core.GameDiagnostics.md#MicroEngine_Core_GameDiagnostics_LastFrame) reports it. Times are in milliseconds of real time.

```csharp
public FrameStatistics(long Frame, double Interval, double Events, double Update, double Render, double Present, int Steps, long Allocated, int Gen0Collections, int Gen1Collections, int Gen2Collections, GraphicsCounters Graphics, AudioStatistics Audio)
```

#### Parameters

`Frame` [long](https://learn.microsoft.com/dotnet/api/system.int64)

The number of the frame, from 1; 0 when nothing was measured.

`Interval` [double](https://learn.microsoft.com/dotnet/api/system.double)

Time since the previous measured frame started (0 for the first one).

`Events` [double](https://learn.microsoft.com/dotnet/api/system.double)

The platform's event pump.

`Update` [double](https://learn.microsoft.com/dotnet/api/system.double)

The fixed steps and the per-frame updates ([BeginFrame](MicroEngine.Kernel.FramePhase.md) to [LateUpdate](MicroEngine.Kernel.FramePhase.md)).

`Render` [double](https://learn.microsoft.com/dotnet/api/system.double)

Extracting and drawing ([Extract](MicroEngine.Kernel.FramePhase.md) and [Render](MicroEngine.Kernel.FramePhase.md)).

`Present` [double](https://learn.microsoft.com/dotnet/api/system.double)

Presenting ([Present](MicroEngine.Kernel.FramePhase.md) and [EndFrame](MicroEngine.Kernel.FramePhase.md)), which includes waiting for the display.

`Steps` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Fixed steps the frame ran.

`Allocated` [long](https://learn.microsoft.com/dotnet/api/system.int64)

Bytes the game's thread allocated during the frame.

`Gen0Collections` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Garbage collections of generation 0 during the frame.

`Gen1Collections` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Garbage collections of generation 1 during the frame.

`Gen2Collections` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Full garbage collections during the frame.

`Graphics` [GraphicsCounters](MicroEngine.Graphics.Devices.GraphicsCounters.md)

The work the frame submitted to the graphics device (draw calls, passes, uploads; [LiveResources](MicroEngine.Graphics.Devices.GraphicsCounters.md#MicroEngine_Graphics_Devices_GraphicsCounters_LiveResources) is the count at its end). Zeros on a platform whose device does not count.

`Audio` [AudioStatistics](MicroEngine.Audio.AudioStatistics.md)

The mixer's voices at the end of the frame.

## Properties

### <a id="MicroEngine_Core_FrameStatistics_Allocated"></a> Allocated

Bytes the game's thread allocated during the frame.

```csharp
public long Allocated { get; init; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Core_FrameStatistics_Audio"></a> Audio

The mixer's voices at the end of the frame.

```csharp
public AudioStatistics Audio { get; init; }
```

#### Property Value

 [AudioStatistics](MicroEngine.Audio.AudioStatistics.md)

### <a id="MicroEngine_Core_FrameStatistics_Collections"></a> Collections

Garbage collections of any generation during the frame.

```csharp
public int Collections { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_FrameStatistics_Events"></a> Events

The platform's event pump.

```csharp
public double Events { get; init; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Core_FrameStatistics_Frame"></a> Frame

The number of the frame, from 1; 0 when nothing was measured.

```csharp
public long Frame { get; init; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Core_FrameStatistics_Gen0Collections"></a> Gen0Collections

Garbage collections of generation 0 during the frame.

```csharp
public int Gen0Collections { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_FrameStatistics_Gen1Collections"></a> Gen1Collections

Garbage collections of generation 1 during the frame.

```csharp
public int Gen1Collections { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_FrameStatistics_Gen2Collections"></a> Gen2Collections

Full garbage collections during the frame.

```csharp
public int Gen2Collections { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_FrameStatistics_Graphics"></a> Graphics

The work the frame submitted to the graphics device (draw calls, passes, uploads; [LiveResources](MicroEngine.Graphics.Devices.GraphicsCounters.md#MicroEngine_Graphics_Devices_GraphicsCounters_LiveResources) is the count at its end). Zeros on a platform whose device does not count.

```csharp
public GraphicsCounters Graphics { get; init; }
```

#### Property Value

 [GraphicsCounters](MicroEngine.Graphics.Devices.GraphicsCounters.md)

### <a id="MicroEngine_Core_FrameStatistics_Interval"></a> Interval

Time since the previous measured frame started (0 for the first one).

```csharp
public double Interval { get; init; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Core_FrameStatistics_Present"></a> Present

Presenting ([Present](MicroEngine.Kernel.FramePhase.md) and [EndFrame](MicroEngine.Kernel.FramePhase.md)), which includes waiting for the display.

```csharp
public double Present { get; init; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Core_FrameStatistics_Render"></a> Render

Extracting and drawing ([Extract](MicroEngine.Kernel.FramePhase.md) and [Render](MicroEngine.Kernel.FramePhase.md)).

```csharp
public double Render { get; init; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Core_FrameStatistics_Steps"></a> Steps

Fixed steps the frame ran.

```csharp
public int Steps { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_FrameStatistics_Total"></a> Total

The frame's work: events, update, render and present.

```csharp
public double Total { get; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Core_FrameStatistics_Update"></a> Update

The fixed steps and the per-frame updates ([BeginFrame](MicroEngine.Kernel.FramePhase.md) to [LateUpdate](MicroEngine.Kernel.FramePhase.md)).

```csharp
public double Update { get; init; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

## Methods

### <a id="MicroEngine_Core_FrameStatistics_Deconstruct_System_Int64__System_Double__System_Double__System_Double__System_Double__System_Double__System_Int32__System_Int64__System_Int32__System_Int32__System_Int32__MicroEngine_Graphics_Devices_GraphicsCounters__MicroEngine_Audio_AudioStatistics__"></a> Deconstruct\(out long, out double, out double, out double, out double, out double, out int, out long, out int, out int, out int, out GraphicsCounters, out AudioStatistics\)

```csharp
public void Deconstruct(out long Frame, out double Interval, out double Events, out double Update, out double Render, out double Present, out int Steps, out long Allocated, out int Gen0Collections, out int Gen1Collections, out int Gen2Collections, out GraphicsCounters Graphics, out AudioStatistics Audio)
```

#### Parameters

`Frame` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`Interval` [double](https://learn.microsoft.com/dotnet/api/system.double)

`Events` [double](https://learn.microsoft.com/dotnet/api/system.double)

`Update` [double](https://learn.microsoft.com/dotnet/api/system.double)

`Render` [double](https://learn.microsoft.com/dotnet/api/system.double)

`Present` [double](https://learn.microsoft.com/dotnet/api/system.double)

`Steps` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Allocated` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`Gen0Collections` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Gen1Collections` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Gen2Collections` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Graphics` [GraphicsCounters](MicroEngine.Graphics.Devices.GraphicsCounters.md)

`Audio` [AudioStatistics](MicroEngine.Audio.AudioStatistics.md)

### <a id="MicroEngine_Core_FrameStatistics_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_FrameStatistics_Equals_MicroEngine_Core_FrameStatistics_"></a> Equals\(FrameStatistics\)

```csharp
public bool Equals(FrameStatistics other)
```

#### Parameters

`other` [FrameStatistics](MicroEngine.Core.FrameStatistics.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_FrameStatistics_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_FrameStatistics_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Core_FrameStatistics_op_Equality_MicroEngine_Core_FrameStatistics_MicroEngine_Core_FrameStatistics_"></a> operator ==\(FrameStatistics, FrameStatistics\)

```csharp
public static bool operator ==(FrameStatistics left, FrameStatistics right)
```

#### Parameters

`left` [FrameStatistics](MicroEngine.Core.FrameStatistics.md)

`right` [FrameStatistics](MicroEngine.Core.FrameStatistics.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_FrameStatistics_op_Inequality_MicroEngine_Core_FrameStatistics_MicroEngine_Core_FrameStatistics_"></a> operator \!=\(FrameStatistics, FrameStatistics\)

```csharp
public static bool operator !=(FrameStatistics left, FrameStatistics right)
```

#### Parameters

`left` [FrameStatistics](MicroEngine.Core.FrameStatistics.md)

`right` [FrameStatistics](MicroEngine.Core.FrameStatistics.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
