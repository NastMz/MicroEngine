# Class LocalizationIssue

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Localization](MicroEngine.Localization.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Graphics](../manual/rendering.md)  

Something wrong with a game's string tables, found by [StringTableChecks](MicroEngine.Localization.StringTableChecks.md).

```csharp
public sealed record LocalizationIssue : IEquatable<LocalizationIssue>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[LocalizationIssue](MicroEngine.Localization.LocalizationIssue.md)

#### Implements

[IEquatable<LocalizationIssue\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Localization_LocalizationIssue__ctor_System_Boolean_System_String_System_String_System_String_"></a> LocalizationIssue\(bool, string, string?, string\)

Something wrong with a game's string tables, found by [StringTableChecks](MicroEngine.Localization.StringTableChecks.md).

```csharp
public LocalizationIssue(bool Error, string Language, string? Key, string Message)
```

#### Parameters

`Error` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether a player would see it (a key, a raw placeholder, the wrong language); false for what is allowed but worth a look.

`Language` [string](https://learn.microsoft.com/dotnet/api/system.string)

The language whose table has the problem.

`Key` [string](https://learn.microsoft.com/dotnet/api/system.string)?

The key, or null when the problem is the language's as a whole.

`Message` [string](https://learn.microsoft.com/dotnet/api/system.string)

What is wrong and what to do.

## Properties

### <a id="MicroEngine_Localization_LocalizationIssue_Error"></a> Error

Whether a player would see it (a key, a raw placeholder, the wrong language); false for what is allowed but worth a look.

```csharp
public bool Error { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Localization_LocalizationIssue_Key"></a> Key

The key, or null when the problem is the language's as a whole.

```csharp
public string? Key { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Localization_LocalizationIssue_Language"></a> Language

The language whose table has the problem.

```csharp
public string Language { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Localization_LocalizationIssue_Message"></a> Message

What is wrong and what to do.

```csharp
public string Message { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Localization_LocalizationIssue__Clone__"></a> <Clone\>$\(\)

```csharp
public LocalizationIssue <Clone>$()
```

#### Returns

 [LocalizationIssue](MicroEngine.Localization.LocalizationIssue.md)

### <a id="MicroEngine_Localization_LocalizationIssue_Deconstruct_System_Boolean__System_String__System_String__System_String__"></a> Deconstruct\(out bool, out string, out string?, out string\)

```csharp
public void Deconstruct(out bool Error, out string Language, out string? Key, out string Message)
```

#### Parameters

`Error` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`Language` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Key` [string](https://learn.microsoft.com/dotnet/api/system.string)?

`Message` [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Localization_LocalizationIssue_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Localization_LocalizationIssue_Equals_MicroEngine_Localization_LocalizationIssue_"></a> Equals\(LocalizationIssue?\)

```csharp
public bool Equals(LocalizationIssue? other)
```

#### Parameters

`other` [LocalizationIssue](MicroEngine.Localization.LocalizationIssue.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Localization_LocalizationIssue_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Localization_LocalizationIssue_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Localization_LocalizationIssue_op_Equality_MicroEngine_Localization_LocalizationIssue_MicroEngine_Localization_LocalizationIssue_"></a> operator ==\(LocalizationIssue?, LocalizationIssue?\)

```csharp
public static bool operator ==(LocalizationIssue? left, LocalizationIssue? right)
```

#### Parameters

`left` [LocalizationIssue](MicroEngine.Localization.LocalizationIssue.md)?

`right` [LocalizationIssue](MicroEngine.Localization.LocalizationIssue.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Localization_LocalizationIssue_op_Inequality_MicroEngine_Localization_LocalizationIssue_MicroEngine_Localization_LocalizationIssue_"></a> operator \!=\(LocalizationIssue?, LocalizationIssue?\)

```csharp
public static bool operator !=(LocalizationIssue? left, LocalizationIssue? right)
```

#### Parameters

`left` [LocalizationIssue](MicroEngine.Localization.LocalizationIssue.md)?

`right` [LocalizationIssue](MicroEngine.Localization.LocalizationIssue.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
