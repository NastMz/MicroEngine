# Class StringTableData

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Localization](MicroEngine.Localization.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Graphics](../manual/rendering.md)  

The texts of one language as a file gives them, with what the file says about the language.

```csharp
public sealed record StringTableData : IEquatable<StringTableData>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[StringTableData](MicroEngine.Localization.StringTableData.md)

#### Implements

[IEquatable<StringTableData\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Localization_StringTableData__ctor_System_String_System_Collections_Generic_IReadOnlyDictionary_System_String_System_String__"></a> StringTableData\(string, IReadOnlyDictionary<string, string\>\)

The texts of one language as a file gives them, with what the file says about the language.

```csharp
public StringTableData(string Language, IReadOnlyDictionary<string, string> Entries)
```

#### Parameters

`Language` [string](https://learn.microsoft.com/dotnet/api/system.string)

The language tag (<code>en</code>, <code>pt-BR</code>).

`Entries` [IReadOnlyDictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlydictionary\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [string](https://learn.microsoft.com/dotnet/api/system.string)\>

Each key and its message as written.

## Properties

### <a id="MicroEngine_Localization_StringTableData_Entries"></a> Entries

Each key and its message as written.

```csharp
public IReadOnlyDictionary<string, string> Entries { get; init; }
```

#### Property Value

 [IReadOnlyDictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlydictionary\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Localization_StringTableData_Fonts"></a> Fonts

The font files of the language (<code>@fonts</code>), the main face first and then those that fill in what it lacks; empty when the file names none.

```csharp
public IReadOnlyList<string> Fonts { get; init; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Localization_StringTableData_Language"></a> Language

The language tag (<code>en</code>, <code>pt-BR</code>).

```csharp
public string Language { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Localization_StringTableData_Name"></a> Name

The language's own name for a language menu (<code>@name</code>: "Español"), or null when the file gives none.

```csharp
public string? Name { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Localization_StringTableData_RightToLeft"></a> RightToLeft

Whether the language reads right to left (<code>@direction</code>: <code>rtl</code> or <code>ltr</code>), or null to go by the language tag.

```csharp
public bool? RightToLeft { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)?

## Methods

### <a id="MicroEngine_Localization_StringTableData__Clone__"></a> <Clone\>$\(\)

```csharp
public StringTableData <Clone>$()
```

#### Returns

 [StringTableData](MicroEngine.Localization.StringTableData.md)

### <a id="MicroEngine_Localization_StringTableData_Deconstruct_System_String__System_Collections_Generic_IReadOnlyDictionary_System_String_System_String___"></a> Deconstruct\(out string, out IReadOnlyDictionary<string, string\>\)

```csharp
public void Deconstruct(out string Language, out IReadOnlyDictionary<string, string> Entries)
```

#### Parameters

`Language` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Entries` [IReadOnlyDictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlydictionary\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Localization_StringTableData_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Localization_StringTableData_Equals_MicroEngine_Localization_StringTableData_"></a> Equals\(StringTableData?\)

```csharp
public bool Equals(StringTableData? other)
```

#### Parameters

`other` [StringTableData](MicroEngine.Localization.StringTableData.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Localization_StringTableData_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Localization_StringTableData_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Localization_StringTableData_op_Equality_MicroEngine_Localization_StringTableData_MicroEngine_Localization_StringTableData_"></a> operator ==\(StringTableData?, StringTableData?\)

```csharp
public static bool operator ==(StringTableData? left, StringTableData? right)
```

#### Parameters

`left` [StringTableData](MicroEngine.Localization.StringTableData.md)?

`right` [StringTableData](MicroEngine.Localization.StringTableData.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Localization_StringTableData_op_Inequality_MicroEngine_Localization_StringTableData_MicroEngine_Localization_StringTableData_"></a> operator \!=\(StringTableData?, StringTableData?\)

```csharp
public static bool operator !=(StringTableData? left, StringTableData? right)
```

#### Parameters

`left` [StringTableData](MicroEngine.Localization.StringTableData.md)?

`right` [StringTableData](MicroEngine.Localization.StringTableData.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
