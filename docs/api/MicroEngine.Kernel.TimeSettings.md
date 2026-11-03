# Class TimeSettings

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Kernel](MicroEngine.Kernel.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Modules and the frame schedule](../manual/lifecycle.md#modules-and-the-frame-schedule)  

The loop's time policy: the fixed simulation rate, how far a slow frame may catch up, and whether time follows the
wall clock. Every value is explicit; nothing is derived from the platform or from other options.

```csharp
public sealed record TimeSettings : IEquatable<TimeSettings>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[TimeSettings](MicroEngine.Kernel.TimeSettings.md)

#### Implements

[IEquatable<TimeSettings\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Kernel_TimeSettings__ctor"></a> TimeSettings\(\)

```csharp
public TimeSettings()
```

## Properties

### <a id="MicroEngine_Kernel_TimeSettings_FixedDelta"></a> FixedDelta

Seconds of one fixed step: <code>1 / FixedRate</code>.

```csharp
public double FixedDelta { get; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Kernel_TimeSettings_FixedRate"></a> FixedRate

Fixed steps per second the simulation runs at (60 by default); must be positive.

```csharp
public int FixedRate { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Kernel_TimeSettings_MaxFrameDelta"></a> MaxFrameDelta

The longest time in seconds one real-time frame may stand for (0.25 by default); must be positive. Longer frames
(the window was dragged, a breakpoint) count as this much, so the game does not jump ahead.

```csharp
public double MaxFrameDelta { get; init; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Kernel_TimeSettings_MaxStepsPerFrame"></a> MaxStepsPerFrame

At most this many fixed steps run in one frame (5 by default); must be positive. A frame that would need more
(a slow machine, a hitch) runs these and keeps at most one step of time pending, so the game slows down instead
of falling further behind.

```csharp
public int MaxStepsPerFrame { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Kernel_TimeSettings_Mode"></a> Mode

Whether time follows the wall clock or advances one fixed step per frame ([RealTime](MicroEngine.Kernel.TimeMode.md) by default).

```csharp
public TimeMode Mode { get; init; }
```

#### Property Value

 [TimeMode](MicroEngine.Kernel.TimeMode.md)

## Methods

### <a id="MicroEngine_Kernel_TimeSettings__Clone__"></a> <Clone\>$\(\)

```csharp
public TimeSettings <Clone>$()
```

#### Returns

 [TimeSettings](MicroEngine.Kernel.TimeSettings.md)

### <a id="MicroEngine_Kernel_TimeSettings_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Kernel_TimeSettings_Equals_MicroEngine_Kernel_TimeSettings_"></a> Equals\(TimeSettings?\)

```csharp
public bool Equals(TimeSettings? other)
```

#### Parameters

`other` [TimeSettings](MicroEngine.Kernel.TimeSettings.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Kernel_TimeSettings_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Kernel_TimeSettings_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Kernel_TimeSettings_Validate"></a> Validate\(\)

Checks every value.

```csharp
public void Validate()
```

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

A value is out of range; the message names it.

## Operators

### <a id="MicroEngine_Kernel_TimeSettings_op_Equality_MicroEngine_Kernel_TimeSettings_MicroEngine_Kernel_TimeSettings_"></a> operator ==\(TimeSettings?, TimeSettings?\)

```csharp
public static bool operator ==(TimeSettings? left, TimeSettings? right)
```

#### Parameters

`left` [TimeSettings](MicroEngine.Kernel.TimeSettings.md)?

`right` [TimeSettings](MicroEngine.Kernel.TimeSettings.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Kernel_TimeSettings_op_Inequality_MicroEngine_Kernel_TimeSettings_MicroEngine_Kernel_TimeSettings_"></a> operator \!=\(TimeSettings?, TimeSettings?\)

```csharp
public static bool operator !=(TimeSettings? left, TimeSettings? right)
```

#### Parameters

`left` [TimeSettings](MicroEngine.Kernel.TimeSettings.md)?

`right` [TimeSettings](MicroEngine.Kernel.TimeSettings.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
