# Struct AchievementStatus

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.Achievements.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

A snapshot of one achievement: its definition, accumulated progress and whether it is unlocked.

```csharp
public readonly struct AchievementStatus : IEquatable<AchievementStatus>
```

#### Implements

[IEquatable<AchievementStatus\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Core_AchievementStatus__ctor_MicroEngine_Core_Achievement_System_Int32_System_Boolean_"></a> AchievementStatus\(Achievement, int, bool\)

A snapshot of one achievement: its definition, accumulated progress and whether it is unlocked.

```csharp
public AchievementStatus(Achievement Definition, int Progress, bool Unlocked)
```

#### Parameters

`Definition` [Achievement](MicroEngine.Core.Achievement.md)

`Progress` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Unlocked` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Properties

### <a id="MicroEngine_Core_AchievementStatus_Definition"></a> Definition

```csharp
public Achievement Definition { get; init; }
```

#### Property Value

 [Achievement](MicroEngine.Core.Achievement.md)

### <a id="MicroEngine_Core_AchievementStatus_Fraction"></a> Fraction

0..1 for drawing progress bars.

```csharp
public float Fraction { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Core_AchievementStatus_Goal"></a> Goal

The progress needed to unlock it.

```csharp
public int Goal { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_AchievementStatus_Id"></a> Id

The achievement's id.

```csharp
public string Id { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Core_AchievementStatus_IsProgressive"></a> IsProgressive

Whether the goal is larger than 1, so the achievement shows progress.

```csharp
public bool IsProgressive { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_AchievementStatus_Progress"></a> Progress

```csharp
public int Progress { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_AchievementStatus_Unlocked"></a> Unlocked

```csharp
public bool Unlocked { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="MicroEngine_Core_AchievementStatus_Deconstruct_MicroEngine_Core_Achievement__System_Int32__System_Boolean__"></a> Deconstruct\(out Achievement, out int, out bool\)

```csharp
public void Deconstruct(out Achievement Definition, out int Progress, out bool Unlocked)
```

#### Parameters

`Definition` [Achievement](MicroEngine.Core.Achievement.md)

`Progress` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Unlocked` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_AchievementStatus_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_AchievementStatus_Equals_MicroEngine_Core_AchievementStatus_"></a> Equals\(AchievementStatus\)

```csharp
public bool Equals(AchievementStatus other)
```

#### Parameters

`other` [AchievementStatus](MicroEngine.Core.AchievementStatus.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_AchievementStatus_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_AchievementStatus_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Core_AchievementStatus_op_Equality_MicroEngine_Core_AchievementStatus_MicroEngine_Core_AchievementStatus_"></a> operator ==\(AchievementStatus, AchievementStatus\)

```csharp
public static bool operator ==(AchievementStatus left, AchievementStatus right)
```

#### Parameters

`left` [AchievementStatus](MicroEngine.Core.AchievementStatus.md)

`right` [AchievementStatus](MicroEngine.Core.AchievementStatus.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_AchievementStatus_op_Inequality_MicroEngine_Core_AchievementStatus_MicroEngine_Core_AchievementStatus_"></a> operator \!=\(AchievementStatus, AchievementStatus\)

```csharp
public static bool operator !=(AchievementStatus left, AchievementStatus right)
```

#### Parameters

`left` [AchievementStatus](MicroEngine.Core.AchievementStatus.md)

`right` [AchievementStatus](MicroEngine.Core.AchievementStatus.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
