# Class Achievement

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.Achievements.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

One entry of a game's achievement catalog. [Id](MicroEngine.Core.Achievement.md#MicroEngine_Core_Achievement_Id) is the stable key stored in saves, so
names and descriptions can change freely. [Goal](MicroEngine.Core.Achievement.md#MicroEngine_Core_Achievement_Goal) of 1 is an immediate achievement;
larger goals are progressive ("win 10 battles").

```csharp
public sealed record Achievement : IEquatable<Achievement>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Achievement](MicroEngine.Core.Achievement.md)

#### Implements

[IEquatable<Achievement\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Core_Achievement__ctor_System_String_System_String_System_String_System_Int32_"></a> Achievement\(string, string, string, int\)

One entry of a game's achievement catalog. [Id](MicroEngine.Core.Achievement.md#MicroEngine_Core_Achievement_Id) is the stable key stored in saves, so
names and descriptions can change freely. [Goal](MicroEngine.Core.Achievement.md#MicroEngine_Core_Achievement_Goal) of 1 is an immediate achievement;
larger goals are progressive ("win 10 battles").

```csharp
public Achievement(string Id, string Name, string Description, int Goal = 1)
```

#### Parameters

`Id` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Description` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Goal` [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Properties

### <a id="MicroEngine_Core_Achievement_Description"></a> Description

```csharp
public string Description { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Core_Achievement_Goal"></a> Goal

```csharp
public int Goal { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_Achievement_Icon"></a> Icon

Optional picture for the toast and the list screen.

```csharp
public Sprite? Icon { get; init; }
```

#### Property Value

 Sprite?

### <a id="MicroEngine_Core_Achievement_Id"></a> Id

```csharp
public string Id { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Core_Achievement_Name"></a> Name

```csharp
public string Name { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Core_Achievement__Clone__"></a> <Clone\>$\(\)

```csharp
public Achievement <Clone>$()
```

#### Returns

 [Achievement](MicroEngine.Core.Achievement.md)

### <a id="MicroEngine_Core_Achievement_Deconstruct_System_String__System_String__System_String__System_Int32__"></a> Deconstruct\(out string, out string, out string, out int\)

```csharp
public void Deconstruct(out string Id, out string Name, out string Description, out int Goal)
```

#### Parameters

`Id` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Description` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Goal` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_Achievement_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_Achievement_Equals_MicroEngine_Core_Achievement_"></a> Equals\(Achievement?\)

```csharp
public bool Equals(Achievement? other)
```

#### Parameters

`other` [Achievement](MicroEngine.Core.Achievement.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_Achievement_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_Achievement_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Core_Achievement_op_Equality_MicroEngine_Core_Achievement_MicroEngine_Core_Achievement_"></a> operator ==\(Achievement?, Achievement?\)

```csharp
public static bool operator ==(Achievement? left, Achievement? right)
```

#### Parameters

`left` [Achievement](MicroEngine.Core.Achievement.md)?

`right` [Achievement](MicroEngine.Core.Achievement.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_Achievement_op_Inequality_MicroEngine_Core_Achievement_MicroEngine_Core_Achievement_"></a> operator \!=\(Achievement?, Achievement?\)

```csharp
public static bool operator !=(Achievement? left, Achievement? right)
```

#### Parameters

`left` [Achievement](MicroEngine.Core.Achievement.md)?

`right` [Achievement](MicroEngine.Core.Achievement.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
