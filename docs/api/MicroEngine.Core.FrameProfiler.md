# Class FrameProfiler

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

Measures a run frame by frame and the phases of its start, for <code>--profile</code> and benchmarks. It only measures
what the engine itself sees: when [EndFrame](MicroEngine.Core.IPlatform.md#MicroEngine_Core_IPlatform_EndFrame) returns, not when the display shows the frame
(presentation times need the platform's own tools). Samples go into a buffer allocated up front, so profiling
adds no allocations to the frames it measures; a run without a profiler does not measure anything.

```csharp
public sealed class FrameProfiler
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[FrameProfiler](MicroEngine.Core.FrameProfiler.md)

## Constructors

### <a id="MicroEngine_Core_FrameProfiler__ctor_System_Int32_"></a> FrameProfiler\(int\)

Creates a profiler that keeps the last <code class="paramref">capacity</code> frames (36,000 by default: ten minutes at 60 frames per second).

```csharp
public FrameProfiler(int capacity = 36000)
```

#### Parameters

`capacity` [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Properties

### <a id="MicroEngine_Core_FrameProfiler_DisplayRate"></a> DisplayRate

The refresh rate of the display pacing the run ([IDisplayPacing](MicroEngine.Platform.IDisplayPacing.md)), or 0 when unknown. With it,
motion is judged against the whole refresh periods each frame stayed on screen rather than against the loop's
irregular wake-ups; the loop sets it when the run starts.

```csharp
public double DisplayRate { get; set; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Core_FrameProfiler_Environment"></a> Environment

Free-form facts about the run (machine, platform, renderer, display), written with the results.

```csharp
public IDictionary<string, string> Environment { get; }
```

#### Property Value

 [IDictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.idictionary\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Core_FrameProfiler_FrameCount"></a> FrameCount

Frames measured since the start (including those no longer kept).

```csharp
public long FrameCount { get; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Core_FrameProfiler_Phases"></a> Phases

The start phases recorded with [Phase](MicroEngine.Core.FrameProfiler.md#MicroEngine_Core_FrameProfiler_Phase_System_String_), in order, with their durations in milliseconds.

```csharp
public IReadOnlyList<(string Phase, double Milliseconds)> Phases { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<\([string](https://learn.microsoft.com/dotnet/api/system.string) [Phase](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.string,system.double\-.phase), [double](https://learn.microsoft.com/dotnet/api/system.double) [Milliseconds](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.string,system.double\-.milliseconds)\)\>

### <a id="MicroEngine_Core_FrameProfiler_Samples"></a> Samples

The kept frames, oldest first.

```csharp
public IReadOnlyList<FrameSample> Samples { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[FrameSample](MicroEngine.Core.FrameSample.md)\>

## Methods

### <a id="MicroEngine_Core_FrameProfiler_Add_MicroEngine_Core_FrameSample__"></a> Add\(in FrameSample\)

Records a frame.

```csharp
public void Add(in FrameSample sample)
```

#### Parameters

`sample` [FrameSample](MicroEngine.Core.FrameSample.md)

### <a id="MicroEngine_Core_FrameProfiler_Phase_System_String_"></a> Phase\(string\)

Ends the current start phase: records the time since the previous phase ended (or since the profiler was created) under <code class="paramref">name</code>.

```csharp
public void Phase(string name)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Core_FrameProfiler_Save_System_String_System_Int32_"></a> Save\(string, int\)

Writes [ToJson](MicroEngine.Core.FrameProfiler.md#MicroEngine_Core_FrameProfiler_ToJson_System_Int32_) to <code class="paramref">path</code>, or [ToCsv](MicroEngine.Core.FrameProfiler.md#MicroEngine_Core_FrameProfiler_ToCsv) when it ends in ".csv".

```csharp
public void Save(string path, int warmup = 0)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

`warmup` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_FrameProfiler_Summarize_System_Int32_"></a> Summarize\(int\)

Statistics of the kept frames, skipping the first <code class="paramref">warmup</code> (start-up and JIT).

```csharp
public FrameSummary Summarize(int warmup = 0)
```

#### Parameters

`warmup` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [FrameSummary](MicroEngine.Core.FrameSummary.md)

### <a id="MicroEngine_Core_FrameProfiler_ToCsv"></a> ToCsv\(\)

Every kept frame as CSV, one row per frame.

```csharp
public string ToCsv()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Core_FrameProfiler_ToJson_System_Int32_"></a> ToJson\(int\)

The results as JSON: environment, start phases, summary and every kept frame.

```csharp
public string ToJson(int warmup = 0)
```

#### Parameters

`warmup` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

</div>
