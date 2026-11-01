# Class WaitSeconds

Namespace: [MicroEngine.Scenes](MicroEngine.Scenes.md)  
Assembly: MicroEngine.dll  
Guide: [Scenes](../manual/scenes.md)  

Yield instruction that pauses a coroutine for a number of seconds; create it with [Seconds](MicroEngine.Scenes.Wait.md#MicroEngine_Scenes_Wait_Seconds_System_Single_).

```csharp
public sealed record WaitSeconds : IEquatable<WaitSeconds>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[WaitSeconds](MicroEngine.Scenes.WaitSeconds.md)

#### Implements

[IEquatable<WaitSeconds\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Scenes_WaitSeconds__ctor_System_Single_"></a> WaitSeconds\(float\)

Yield instruction that pauses a coroutine for a number of seconds; create it with [Seconds](MicroEngine.Scenes.Wait.md#MicroEngine_Scenes_Wait_Seconds_System_Single_).

```csharp
public WaitSeconds(float Seconds)
```

#### Parameters

`Seconds` [float](https://learn.microsoft.com/dotnet/api/system.single)

Update time to wait, in seconds.

## Properties

### <a id="MicroEngine_Scenes_WaitSeconds_Seconds"></a> Seconds

Update time to wait, in seconds.

```csharp
public float Seconds { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_Scenes_WaitSeconds__Clone__"></a> <Clone\>$\(\)

```csharp
public WaitSeconds <Clone>$()
```

#### Returns

 [WaitSeconds](MicroEngine.Scenes.WaitSeconds.md)

### <a id="MicroEngine_Scenes_WaitSeconds_Deconstruct_System_Single__"></a> Deconstruct\(out float\)

```csharp
public void Deconstruct(out float Seconds)
```

#### Parameters

`Seconds` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Scenes_WaitSeconds_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_WaitSeconds_Equals_MicroEngine_Scenes_WaitSeconds_"></a> Equals\(WaitSeconds?\)

```csharp
public bool Equals(WaitSeconds? other)
```

#### Parameters

`other` [WaitSeconds](MicroEngine.Scenes.WaitSeconds.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_WaitSeconds_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Scenes_WaitSeconds_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Scenes_WaitSeconds_op_Equality_MicroEngine_Scenes_WaitSeconds_MicroEngine_Scenes_WaitSeconds_"></a> operator ==\(WaitSeconds?, WaitSeconds?\)

```csharp
public static bool operator ==(WaitSeconds? left, WaitSeconds? right)
```

#### Parameters

`left` [WaitSeconds](MicroEngine.Scenes.WaitSeconds.md)?

`right` [WaitSeconds](MicroEngine.Scenes.WaitSeconds.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_WaitSeconds_op_Inequality_MicroEngine_Scenes_WaitSeconds_MicroEngine_Scenes_WaitSeconds_"></a> operator \!=\(WaitSeconds?, WaitSeconds?\)

```csharp
public static bool operator !=(WaitSeconds? left, WaitSeconds? right)
```

#### Parameters

`left` [WaitSeconds](MicroEngine.Scenes.WaitSeconds.md)?

`right` [WaitSeconds](MicroEngine.Scenes.WaitSeconds.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)
