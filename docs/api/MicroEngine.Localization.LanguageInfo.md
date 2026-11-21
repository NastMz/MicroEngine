# Class LanguageInfo

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Localization](MicroEngine.Localization.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Graphics](../manual/rendering.md)  

A language a game has text in.

```csharp
public sealed record LanguageInfo : IEquatable<LanguageInfo>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[LanguageInfo](MicroEngine.Localization.LanguageInfo.md)

#### Implements

[IEquatable<LanguageInfo\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Localization_LanguageInfo__ctor_System_String_System_String_System_Boolean_System_Collections_Generic_IReadOnlyList_System_String__"></a> LanguageInfo\(string, string, bool, IReadOnlyList<string\>\)

A language a game has text in.

```csharp
public LanguageInfo(string Code, string Name, bool RightToLeft, IReadOnlyList<string> Fonts)
```

#### Parameters

`Code` [string](https://learn.microsoft.com/dotnet/api/system.string)

The language tag of its table (<code>es</code>, <code>pt-BR</code>).

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)

Its own name for a language menu ("Español"); the code when its table gives none.

`RightToLeft` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether it reads right to left.

`Fonts` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

The font files its table names, the main face first; empty when it names none.

## Properties

### <a id="MicroEngine_Localization_LanguageInfo_Code"></a> Code

The language tag of its table (<code>es</code>, <code>pt-BR</code>).

```csharp
public string Code { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Localization_LanguageInfo_Fonts"></a> Fonts

The font files its table names, the main face first; empty when it names none.

```csharp
public IReadOnlyList<string> Fonts { get; init; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Localization_LanguageInfo_Name"></a> Name

Its own name for a language menu ("Español"); the code when its table gives none.

```csharp
public string Name { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Localization_LanguageInfo_RightToLeft"></a> RightToLeft

Whether it reads right to left.

```csharp
public bool RightToLeft { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="MicroEngine_Localization_LanguageInfo__Clone__"></a> <Clone\>$\(\)

```csharp
public LanguageInfo <Clone>$()
```

#### Returns

 [LanguageInfo](MicroEngine.Localization.LanguageInfo.md)

### <a id="MicroEngine_Localization_LanguageInfo_Deconstruct_System_String__System_String__System_Boolean__System_Collections_Generic_IReadOnlyList_System_String___"></a> Deconstruct\(out string, out string, out bool, out IReadOnlyList<string\>\)

```csharp
public void Deconstruct(out string Code, out string Name, out bool RightToLeft, out IReadOnlyList<string> Fonts)
```

#### Parameters

`Code` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`RightToLeft` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`Fonts` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Localization_LanguageInfo_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Localization_LanguageInfo_Equals_MicroEngine_Localization_LanguageInfo_"></a> Equals\(LanguageInfo?\)

```csharp
public bool Equals(LanguageInfo? other)
```

#### Parameters

`other` [LanguageInfo](MicroEngine.Localization.LanguageInfo.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Localization_LanguageInfo_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Localization_LanguageInfo_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Localization_LanguageInfo_op_Equality_MicroEngine_Localization_LanguageInfo_MicroEngine_Localization_LanguageInfo_"></a> operator ==\(LanguageInfo?, LanguageInfo?\)

```csharp
public static bool operator ==(LanguageInfo? left, LanguageInfo? right)
```

#### Parameters

`left` [LanguageInfo](MicroEngine.Localization.LanguageInfo.md)?

`right` [LanguageInfo](MicroEngine.Localization.LanguageInfo.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Localization_LanguageInfo_op_Inequality_MicroEngine_Localization_LanguageInfo_MicroEngine_Localization_LanguageInfo_"></a> operator \!=\(LanguageInfo?, LanguageInfo?\)

```csharp
public static bool operator !=(LanguageInfo? left, LanguageInfo? right)
```

#### Parameters

`left` [LanguageInfo](MicroEngine.Localization.LanguageInfo.md)?

`right` [LanguageInfo](MicroEngine.Localization.LanguageInfo.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
