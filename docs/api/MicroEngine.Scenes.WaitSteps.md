# Class WaitSteps

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Scenes](MicroEngine.Scenes.md)  
Assembly: MicroEngine.dll  
Guide: [Scenes](../manual/scenes.md)  

Yield instruction that pauses a coroutine for a number of updates; create it with [Steps](MicroEngine.Scenes.Wait.md#MicroEngine_Scenes_Wait_Steps_System_Int32_).

```csharp
public sealed record WaitSteps : IEquatable<WaitSteps>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[WaitSteps](MicroEngine.Scenes.WaitSteps.md)

#### Implements

[IEquatable<WaitSteps\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Scenes_WaitSteps__ctor_System_Int32_"></a> WaitSteps\(int\)

Yield instruction that pauses a coroutine for a number of updates; create it with [Steps](MicroEngine.Scenes.Wait.md#MicroEngine_Scenes_Wait_Steps_System_Int32_).

```csharp
public WaitSteps(int Steps)
```

#### Parameters

`Steps` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Updates to wait.

## Properties

### <a id="MicroEngine_Scenes_WaitSteps_Steps"></a> Steps

Updates to wait.

```csharp
public int Steps { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Scenes_WaitSteps__Clone__"></a> <Clone\>$\(\)

```csharp
public WaitSteps <Clone>$()
```

#### Returns

 [WaitSteps](MicroEngine.Scenes.WaitSteps.md)

### <a id="MicroEngine_Scenes_WaitSteps_Deconstruct_System_Int32__"></a> Deconstruct\(out int\)

```csharp
public void Deconstruct(out int Steps)
```

#### Parameters

`Steps` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Scenes_WaitSteps_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_WaitSteps_Equals_MicroEngine_Scenes_WaitSteps_"></a> Equals\(WaitSteps?\)

```csharp
public bool Equals(WaitSteps? other)
```

#### Parameters

`other` [WaitSteps](MicroEngine.Scenes.WaitSteps.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_WaitSteps_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Scenes_WaitSteps_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Scenes_WaitSteps_op_Equality_MicroEngine_Scenes_WaitSteps_MicroEngine_Scenes_WaitSteps_"></a> operator ==\(WaitSteps?, WaitSteps?\)

```csharp
public static bool operator ==(WaitSteps? left, WaitSteps? right)
```

#### Parameters

`left` [WaitSteps](MicroEngine.Scenes.WaitSteps.md)?

`right` [WaitSteps](MicroEngine.Scenes.WaitSteps.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_WaitSteps_op_Inequality_MicroEngine_Scenes_WaitSteps_MicroEngine_Scenes_WaitSteps_"></a> operator \!=\(WaitSteps?, WaitSteps?\)

```csharp
public static bool operator !=(WaitSteps? left, WaitSteps? right)
```

#### Parameters

`left` [WaitSteps](MicroEngine.Scenes.WaitSteps.md)?

`right` [WaitSteps](MicroEngine.Scenes.WaitSteps.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
