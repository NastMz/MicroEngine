# Class WaitUntil

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Scenes](MicroEngine.Scenes.md)  
Assembly: MicroEngine.dll  
Guide: [Scenes](../manual/scenes.md)  

Yield instruction that pauses a coroutine until a condition holds; create it with [Until](MicroEngine.Scenes.Wait.md#MicroEngine_Scenes_Wait_Until_System_Func_System_Boolean__).

```csharp
public sealed record WaitUntil : IEquatable<WaitUntil>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[WaitUntil](MicroEngine.Scenes.WaitUntil.md)

#### Implements

[IEquatable<WaitUntil\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Scenes_WaitUntil__ctor_System_Func_System_Boolean__"></a> WaitUntil\(Func<bool\>\)

Yield instruction that pauses a coroutine until a condition holds; create it with [Until](MicroEngine.Scenes.Wait.md#MicroEngine_Scenes_Wait_Until_System_Func_System_Boolean__).

```csharp
public WaitUntil(Func<bool> Condition)
```

#### Parameters

`Condition` [Func](https://learn.microsoft.com/dotnet/api/system.func\-1)<[bool](https://learn.microsoft.com/dotnet/api/system.boolean)\>

Checked once per update; the coroutine resumes in the update it first returns true.

## Properties

### <a id="MicroEngine_Scenes_WaitUntil_Condition"></a> Condition

Checked once per update; the coroutine resumes in the update it first returns true.

```csharp
public Func<bool> Condition { get; init; }
```

#### Property Value

 [Func](https://learn.microsoft.com/dotnet/api/system.func\-1)<[bool](https://learn.microsoft.com/dotnet/api/system.boolean)\>

## Methods

### <a id="MicroEngine_Scenes_WaitUntil__Clone__"></a> <Clone\>$\(\)

```csharp
public WaitUntil <Clone>$()
```

#### Returns

 [WaitUntil](MicroEngine.Scenes.WaitUntil.md)

### <a id="MicroEngine_Scenes_WaitUntil_Deconstruct_System_Func_System_Boolean___"></a> Deconstruct\(out Func<bool\>\)

```csharp
public void Deconstruct(out Func<bool> Condition)
```

#### Parameters

`Condition` [Func](https://learn.microsoft.com/dotnet/api/system.func\-1)<[bool](https://learn.microsoft.com/dotnet/api/system.boolean)\>

### <a id="MicroEngine_Scenes_WaitUntil_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_WaitUntil_Equals_MicroEngine_Scenes_WaitUntil_"></a> Equals\(WaitUntil?\)

```csharp
public bool Equals(WaitUntil? other)
```

#### Parameters

`other` [WaitUntil](MicroEngine.Scenes.WaitUntil.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_WaitUntil_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Scenes_WaitUntil_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Scenes_WaitUntil_op_Equality_MicroEngine_Scenes_WaitUntil_MicroEngine_Scenes_WaitUntil_"></a> operator ==\(WaitUntil?, WaitUntil?\)

```csharp
public static bool operator ==(WaitUntil? left, WaitUntil? right)
```

#### Parameters

`left` [WaitUntil](MicroEngine.Scenes.WaitUntil.md)?

`right` [WaitUntil](MicroEngine.Scenes.WaitUntil.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_WaitUntil_op_Inequality_MicroEngine_Scenes_WaitUntil_MicroEngine_Scenes_WaitUntil_"></a> operator \!=\(WaitUntil?, WaitUntil?\)

```csharp
public static bool operator !=(WaitUntil? left, WaitUntil? right)
```

#### Parameters

`left` [WaitUntil](MicroEngine.Scenes.WaitUntil.md)?

`right` [WaitUntil](MicroEngine.Scenes.WaitUntil.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
