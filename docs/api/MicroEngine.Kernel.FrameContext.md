# Struct FrameContext

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Kernel](MicroEngine.Kernel.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

What a frame system knows about time when it runs ([FrameSchedule](MicroEngine.Kernel.FrameSchedule.md)).

```csharp
public readonly struct FrameContext : IEquatable<FrameContext>
```

#### Implements

[IEquatable<FrameContext\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Kernel_FrameContext__ctor_System_Double_System_Double_System_Single_System_Int64_System_Int64_"></a> FrameContext\(double, double, float, long, long\)

What a frame system knows about time when it runs ([FrameSchedule](MicroEngine.Kernel.FrameSchedule.md)).

```csharp
public FrameContext(double Delta, double FixedDelta, float Blend, long FixedStep, long Frame)
```

#### Parameters

`Delta` [double](https://learn.microsoft.com/dotnet/api/system.double)

Seconds this frame stands for: in real time, the elapsed time measured by the loop (paced to the display and at most
[MaxFrameDelta](MicroEngine.Kernel.TimeSettings.md#MicroEngine_Kernel_TimeSettings_MaxFrameDelta)); in deterministic time, exactly [FixedDelta](MicroEngine.Kernel.FrameContext.md#MicroEngine_Kernel_FrameContext_FixedDelta).

`FixedDelta` [double](https://learn.microsoft.com/dotnet/api/system.double)

Seconds of one fixed step: <code>1 / TimeSettings.FixedRate</code>.

`Blend` [float](https://learn.microsoft.com/dotnet/api/system.single)

How far the frame is between the last fixed step and the next one, 0..1: the part of a step of time that has passed
but has not been simulated yet. Drawing at <code>Lerp(previous, current, Blend)</code> makes motion even on displays whose
rate is not a multiple of the fixed rate. Always 1 in deterministic time (the frame shows exactly the last step) and
0 inside the fixed phases (they run on a step boundary).

`FixedStep` [long](https://learn.microsoft.com/dotnet/api/system.int64)

The number of fixed steps completed before this moment; inside a fixed phase, the index of the step being run, from 0.

`Frame` [long](https://learn.microsoft.com/dotnet/api/system.int64)

The number of the frame, from 1 for the first frame run.

## Properties

### <a id="MicroEngine_Kernel_FrameContext_Blend"></a> Blend

How far the frame is between the last fixed step and the next one, 0..1: the part of a step of time that has passed
but has not been simulated yet. Drawing at <code>Lerp(previous, current, Blend)</code> makes motion even on displays whose
rate is not a multiple of the fixed rate. Always 1 in deterministic time (the frame shows exactly the last step) and
0 inside the fixed phases (they run on a step boundary).

```csharp
public float Blend { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Kernel_FrameContext_Delta"></a> Delta

Seconds this frame stands for: in real time, the elapsed time measured by the loop (paced to the display and at most
[MaxFrameDelta](MicroEngine.Kernel.TimeSettings.md#MicroEngine_Kernel_TimeSettings_MaxFrameDelta)); in deterministic time, exactly [FixedDelta](MicroEngine.Kernel.FrameContext.md#MicroEngine_Kernel_FrameContext_FixedDelta).

```csharp
public double Delta { get; init; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Kernel_FrameContext_FixedDelta"></a> FixedDelta

Seconds of one fixed step: <code>1 / TimeSettings.FixedRate</code>.

```csharp
public double FixedDelta { get; init; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Kernel_FrameContext_FixedStep"></a> FixedStep

The number of fixed steps completed before this moment; inside a fixed phase, the index of the step being run, from 0.

```csharp
public long FixedStep { get; init; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Kernel_FrameContext_Frame"></a> Frame

The number of the frame, from 1 for the first frame run.

```csharp
public long Frame { get; init; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

## Methods

### <a id="MicroEngine_Kernel_FrameContext_Deconstruct_System_Double__System_Double__System_Single__System_Int64__System_Int64__"></a> Deconstruct\(out double, out double, out float, out long, out long\)

```csharp
public void Deconstruct(out double Delta, out double FixedDelta, out float Blend, out long FixedStep, out long Frame)
```

#### Parameters

`Delta` [double](https://learn.microsoft.com/dotnet/api/system.double)

`FixedDelta` [double](https://learn.microsoft.com/dotnet/api/system.double)

`Blend` [float](https://learn.microsoft.com/dotnet/api/system.single)

`FixedStep` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`Frame` [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Kernel_FrameContext_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Kernel_FrameContext_Equals_MicroEngine_Kernel_FrameContext_"></a> Equals\(FrameContext\)

```csharp
public bool Equals(FrameContext other)
```

#### Parameters

`other` [FrameContext](MicroEngine.Kernel.FrameContext.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Kernel_FrameContext_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Kernel_FrameContext_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Kernel_FrameContext_op_Equality_MicroEngine_Kernel_FrameContext_MicroEngine_Kernel_FrameContext_"></a> operator ==\(FrameContext, FrameContext\)

```csharp
public static bool operator ==(FrameContext left, FrameContext right)
```

#### Parameters

`left` [FrameContext](MicroEngine.Kernel.FrameContext.md)

`right` [FrameContext](MicroEngine.Kernel.FrameContext.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Kernel_FrameContext_op_Inequality_MicroEngine_Kernel_FrameContext_MicroEngine_Kernel_FrameContext_"></a> operator \!=\(FrameContext, FrameContext\)

```csharp
public static bool operator !=(FrameContext left, FrameContext right)
```

#### Parameters

`left` [FrameContext](MicroEngine.Kernel.FrameContext.md)

`right` [FrameContext](MicroEngine.Kernel.FrameContext.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
