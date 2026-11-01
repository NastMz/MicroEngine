# Class RunResult

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

How a run ended.

```csharp
public sealed record RunResult : IEquatable<RunResult>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[RunResult](MicroEngine.Core.RunResult.md)

#### Implements

[IEquatable<RunResult\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Core_RunResult__ctor_System_Int64_System_Int64_System_Collections_Generic_IReadOnlyList_System_String__System_Exception_"></a> RunResult\(long, long, IReadOnlyList<string\>, Exception?\)

How a run ended.

```csharp
public RunResult(long Updates, long Frames, IReadOnlyList<string> Captures, Exception? Error)
```

#### Parameters

`Updates` [long](https://learn.microsoft.com/dotnet/api/system.int64)

Fixed updates simulated.

`Frames` [long](https://learn.microsoft.com/dotnet/api/system.int64)

Frames rendered.

`Captures` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

Paths of the screenshots written by the script, in order.

`Error` [Exception](https://learn.microsoft.com/dotnet/api/system.exception)?

The exception that stopped the run, or null when it ended normally.

## Properties

### <a id="MicroEngine_Core_RunResult_Captures"></a> Captures

Paths of the screenshots written by the script, in order.

```csharp
public IReadOnlyList<string> Captures { get; init; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Core_RunResult_Error"></a> Error

The exception that stopped the run, or null when it ended normally.

```csharp
public Exception? Error { get; init; }
```

#### Property Value

 [Exception](https://learn.microsoft.com/dotnet/api/system.exception)?

### <a id="MicroEngine_Core_RunResult_Frames"></a> Frames

Frames rendered.

```csharp
public long Frames { get; init; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Core_RunResult_Updates"></a> Updates

Fixed updates simulated.

```csharp
public long Updates { get; init; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

## Methods

### <a id="MicroEngine_Core_RunResult__Clone__"></a> <Clone\>$\(\)

```csharp
public RunResult <Clone>$()
```

#### Returns

 [RunResult](MicroEngine.Core.RunResult.md)

### <a id="MicroEngine_Core_RunResult_Deconstruct_System_Int64__System_Int64__System_Collections_Generic_IReadOnlyList_System_String___System_Exception__"></a> Deconstruct\(out long, out long, out IReadOnlyList<string\>, out Exception?\)

```csharp
public void Deconstruct(out long Updates, out long Frames, out IReadOnlyList<string> Captures, out Exception? Error)
```

#### Parameters

`Updates` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`Frames` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`Captures` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

`Error` [Exception](https://learn.microsoft.com/dotnet/api/system.exception)?

### <a id="MicroEngine_Core_RunResult_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_RunResult_Equals_MicroEngine_Core_RunResult_"></a> Equals\(RunResult?\)

```csharp
public bool Equals(RunResult? other)
```

#### Parameters

`other` [RunResult](MicroEngine.Core.RunResult.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_RunResult_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_RunResult_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Core_RunResult_op_Equality_MicroEngine_Core_RunResult_MicroEngine_Core_RunResult_"></a> operator ==\(RunResult?, RunResult?\)

```csharp
public static bool operator ==(RunResult? left, RunResult? right)
```

#### Parameters

`left` [RunResult](MicroEngine.Core.RunResult.md)?

`right` [RunResult](MicroEngine.Core.RunResult.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_RunResult_op_Inequality_MicroEngine_Core_RunResult_MicroEngine_Core_RunResult_"></a> operator \!=\(RunResult?, RunResult?\)

```csharp
public static bool operator !=(RunResult? left, RunResult? right)
```

#### Parameters

`left` [RunResult](MicroEngine.Core.RunResult.md)?

`right` [RunResult](MicroEngine.Core.RunResult.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
