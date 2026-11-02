# Class FrameClock

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Kernel](MicroEngine.Kernel.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

The loop's clock: turns the time each frame stands for into fixed steps, following [TimeSettings](MicroEngine.Kernel.TimeSettings.md). The
platform only supplies a monotonic clock; how much simulated time a frame adds, how many steps it runs, how far a slow
frame catches up and the interpolation blend are decided here, the same way on every backend.

```csharp
public sealed class FrameClock
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[FrameClock](MicroEngine.Kernel.FrameClock.md)

## Remarks

One frame is: [BeginFrame](MicroEngine.Kernel.FrameClock.md#MicroEngine_Kernel_FrameClock_BeginFrame_System_Double_) with the frame's elapsed time, [TryStep](MicroEngine.Kernel.FrameClock.md#MicroEngine_Kernel_FrameClock_TryStep_MicroEngine_Kernel_FrameContext__) until it returns false
(or until the caller stops early), then [EndSteps](MicroEngine.Kernel.FrameClock.md#MicroEngine_Kernel_FrameClock_EndSteps).

```csharp
clock.BeginFrame(clock.ClampElapsed(now - last));
while (clock.TryStep(out var step)) schedule.Run(FramePhase.FixedUpdate, step);
clock.EndSteps();
```

## Constructors

### <a id="MicroEngine_Kernel_FrameClock__ctor_MicroEngine_Kernel_TimeSettings_"></a> FrameClock\(TimeSettings\)

A clock following <code class="paramref">settings</code>.

```csharp
public FrameClock(TimeSettings settings)
```

#### Parameters

`settings` [TimeSettings](MicroEngine.Kernel.TimeSettings.md)

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

<code class="paramref">settings</code> is null.

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

A setting is out of range ([Validate](MicroEngine.Kernel.TimeSettings.md#MicroEngine_Kernel_TimeSettings_Validate)).

## Properties

### <a id="MicroEngine_Kernel_FrameClock_Blend"></a> Blend

The current frame's interpolation blend, set by [EndSteps](MicroEngine.Kernel.FrameClock.md#MicroEngine_Kernel_FrameClock_EndSteps) ([Blend](MicroEngine.Kernel.FrameContext.md#MicroEngine_Kernel_FrameContext_Blend)).

```csharp
public float Blend { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Kernel_FrameClock_Context"></a> Context

The frame's context for the phases that run once per frame.

```csharp
public FrameContext Context { get; }
```

#### Property Value

 [FrameContext](MicroEngine.Kernel.FrameContext.md)

### <a id="MicroEngine_Kernel_FrameClock_Delta"></a> Delta

Seconds the current frame stands for ([Delta](MicroEngine.Kernel.FrameContext.md#MicroEngine_Kernel_FrameContext_Delta)).

```csharp
public double Delta { get; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Kernel_FrameClock_FixedDelta"></a> FixedDelta

Seconds of one fixed step.

```csharp
public double FixedDelta { get; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Kernel_FrameClock_FixedStep"></a> FixedStep

Fixed steps completed so far.

```csharp
public long FixedStep { get; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Kernel_FrameClock_Frame"></a> Frame

Frames begun so far; the current frame's number while one runs.

```csharp
public long Frame { get; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Kernel_FrameClock_Mode"></a> Mode

Whether time follows the wall clock or advances one step per frame.

```csharp
public TimeMode Mode { get; }
```

#### Property Value

 [TimeMode](MicroEngine.Kernel.TimeMode.md)

### <a id="MicroEngine_Kernel_FrameClock_Pending"></a> Pending

Time added but not simulated yet, in seconds.

```csharp
public double Pending { get; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Kernel_FrameClock_Settings"></a> Settings

The policy this clock follows.

```csharp
public TimeSettings Settings { get; }
```

#### Property Value

 [TimeSettings](MicroEngine.Kernel.TimeSettings.md)

### <a id="MicroEngine_Kernel_FrameClock_StepsThisFrame"></a> StepsThisFrame

Steps run in the current frame.

```csharp
public int StepsThisFrame { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Kernel_FrameClock_BeginFrame_System_Double_"></a> BeginFrame\(double\)

Starts a frame. In real time, <code class="paramref">elapsed</code> seconds are added to the pending time (pass a value already
limited by [ClampElapsed](MicroEngine.Kernel.FrameClock.md#MicroEngine_Kernel_FrameClock_ClampElapsed_System_Double_); negative values count as 0); in deterministic time exactly one step is added
and <code class="paramref">elapsed</code> is ignored.

```csharp
public void BeginFrame(double elapsed)
```

#### Parameters

`elapsed` [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Kernel_FrameClock_ClampElapsed_System_Double_"></a> ClampElapsed\(double\)

The time a real-time frame may stand for: <code class="paramref">elapsed</code> limited to 0..[MaxFrameDelta](MicroEngine.Kernel.TimeSettings.md#MicroEngine_Kernel_TimeSettings_MaxFrameDelta).
The loop applies it to the time it measured before pacing that time to the display.

```csharp
public double ClampElapsed(double elapsed)
```

#### Parameters

`elapsed` [double](https://learn.microsoft.com/dotnet/api/system.double)

#### Returns

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Kernel_FrameClock_DiscardPending"></a> DiscardPending\(\)

Forgets the pending time, so time spent away (a suspended app) is not simulated when it comes back.

```csharp
public void DiscardPending()
```

### <a id="MicroEngine_Kernel_FrameClock_EndSteps"></a> EndSteps\(\)

Ends the frame's steps. A frame that ran [MaxStepsPerFrame](MicroEngine.Kernel.TimeSettings.md#MicroEngine_Kernel_TimeSettings_MaxStepsPerFrame) steps keeps at most one step
pending (the rest is dropped: the game slows down rather than falling further behind). Then sets
[Blend](MicroEngine.Kernel.FrameClock.md#MicroEngine_Kernel_FrameClock_Blend): the pending fraction of a step in real time, 1 in deterministic time.

```csharp
public void EndSteps()
```

### <a id="MicroEngine_Kernel_FrameClock_TryStep_MicroEngine_Kernel_FrameContext__"></a> TryStep\(out FrameContext\)

Takes the next fixed step if one is due and the frame has not run [MaxStepsPerFrame](MicroEngine.Kernel.TimeSettings.md#MicroEngine_Kernel_TimeSettings_MaxStepsPerFrame)
yet. The caller runs the fixed phases with <code class="paramref">step</code> when it returns true.

```csharp
public bool TryStep(out FrameContext step)
```

#### Parameters

`step` [FrameContext](MicroEngine.Kernel.FrameContext.md)

The step's context: [FixedStep](MicroEngine.Kernel.FrameContext.md#MicroEngine_Kernel_FrameContext_FixedStep) is the index of this step, [Blend](MicroEngine.Kernel.FrameContext.md#MicroEngine_Kernel_FrameContext_Blend) is 0.

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
