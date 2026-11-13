# Struct SystemTiming

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

How long one system of the frame schedule took during the last frame ([Systems](MicroEngine.Core.GameDiagnostics.md#MicroEngine_Core_GameDiagnostics_Systems)).

```csharp
public readonly struct SystemTiming : IEquatable<SystemTiming>
```

#### Implements

[IEquatable<SystemTiming\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Core_SystemTiming__ctor_System_String_MicroEngine_Kernel_FramePhase_System_Double_"></a> SystemTiming\(string, FramePhase, double\)

How long one system of the frame schedule took during the last frame ([Systems](MicroEngine.Core.GameDiagnostics.md#MicroEngine_Core_GameDiagnostics_Systems)).

```csharp
public SystemTiming(string Name, FramePhase Phase, double Milliseconds)
```

#### Parameters

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)

The system's name in [FrameSchedule](MicroEngine.Kernel.FrameSchedule.md).

`Phase` [FramePhase](MicroEngine.Kernel.FramePhase.md)

Its phase.

`Milliseconds` [double](https://learn.microsoft.com/dotnet/api/system.double)

Its time during the frame; a fixed phase adds up every step of the frame.

## Properties

### <a id="MicroEngine_Core_SystemTiming_Milliseconds"></a> Milliseconds

Its time during the frame; a fixed phase adds up every step of the frame.

```csharp
public double Milliseconds { get; init; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Core_SystemTiming_Name"></a> Name

The system's name in [FrameSchedule](MicroEngine.Kernel.FrameSchedule.md).

```csharp
public string Name { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Core_SystemTiming_Phase"></a> Phase

Its phase.

```csharp
public FramePhase Phase { get; init; }
```

#### Property Value

 [FramePhase](MicroEngine.Kernel.FramePhase.md)

## Methods

### <a id="MicroEngine_Core_SystemTiming_Deconstruct_System_String__MicroEngine_Kernel_FramePhase__System_Double__"></a> Deconstruct\(out string, out FramePhase, out double\)

```csharp
public void Deconstruct(out string Name, out FramePhase Phase, out double Milliseconds)
```

#### Parameters

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Phase` [FramePhase](MicroEngine.Kernel.FramePhase.md)

`Milliseconds` [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Core_SystemTiming_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_SystemTiming_Equals_MicroEngine_Core_SystemTiming_"></a> Equals\(SystemTiming\)

```csharp
public bool Equals(SystemTiming other)
```

#### Parameters

`other` [SystemTiming](MicroEngine.Core.SystemTiming.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_SystemTiming_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_SystemTiming_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Core_SystemTiming_op_Equality_MicroEngine_Core_SystemTiming_MicroEngine_Core_SystemTiming_"></a> operator ==\(SystemTiming, SystemTiming\)

```csharp
public static bool operator ==(SystemTiming left, SystemTiming right)
```

#### Parameters

`left` [SystemTiming](MicroEngine.Core.SystemTiming.md)

`right` [SystemTiming](MicroEngine.Core.SystemTiming.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_SystemTiming_op_Inequality_MicroEngine_Core_SystemTiming_MicroEngine_Core_SystemTiming_"></a> operator \!=\(SystemTiming, SystemTiming\)

```csharp
public static bool operator !=(SystemTiming left, SystemTiming right)
```

#### Parameters

`left` [SystemTiming](MicroEngine.Core.SystemTiming.md)

`right` [SystemTiming](MicroEngine.Core.SystemTiming.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
