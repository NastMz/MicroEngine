# Class StringTables

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Localization](MicroEngine.Localization.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Graphics](../manual/rendering.md)  

Reads string tables: a JSON file per language (<code>es.strings.json</code>) or a CSV file with a column per language
(<code>game.strings.csv</code>). Both hold a message per key ([Message](MicroEngine.Localization.Message.md)); keys starting with <code>@</code>
describe the language (<code>@name</code>, <code>@fonts</code>, <code>@direction</code>).

```csharp
public static class StringTables
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[StringTables](MicroEngine.Localization.StringTables.md)

## Fields

### <a id="MicroEngine_Localization_StringTables_CsvExtension"></a> CsvExtension

The ending of a CSV table's file name.

```csharp
public const string CsvExtension = ".strings.csv"
```

#### Field Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Localization_StringTables_JsonExtension"></a> JsonExtension

The ending of a JSON table's file name; what comes before it is the language tag.

```csharp
public const string JsonExtension = ".strings.json"
```

#### Field Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Localization_StringTables_PromptPrefix"></a> PromptPrefix

The start of the keys that translate the engine's names of keys and controls in action prompts:
<code>prompt.Space</code>, <code>prompt.Enter</code>, <code>prompt.D-pad</code>. A language gives only those it has another word for.

```csharp
public const string PromptPrefix = "prompt."
```

#### Field Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Localization_StringTables_IsTable_System_String_"></a> IsTable\(string\)

Whether a file name is a string table's.

```csharp
public static bool IsTable(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Localization_StringTables_LanguageOf_System_String_"></a> LanguageOf\(string\)

The language of a JSON table from its file name (<code>text/pt-BR.strings.json</code> is <code>pt-BR</code>), or null when the name is not a JSON table's.

```csharp
public static string? LanguageOf(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Localization_StringTables_ParseCsv_System_String_"></a> ParseCsv\(string\)

Reads a CSV table: the first row names the key column and then a language tag per column, every other row is a
key and its message in each language. An empty cell is a text the language does not have; a row whose first
cell starts with <code>#</code> is a comment. Cells follow the usual rules: commas separate them, quotes surround a
cell holding commas, quotes or line breaks, and two quotes inside are one.

```csharp
public static IReadOnlyList<StringTableData> ParseCsv(string csv)
```

#### Parameters

`csv` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[StringTableData](MicroEngine.Localization.StringTableData.md)\>

#### Exceptions

 [FormatException](https://learn.microsoft.com/dotnet/api/system.formatexception)

The text is not a table: it says what is wrong and on which line.

### <a id="MicroEngine_Localization_StringTables_ParseJson_System_String_System_String_"></a> ParseJson\(string, string\)

Reads a JSON table: an object whose values are messages, or objects of messages whose keys join with a dot
(<code>{"menu": {"play": "Play"}}</code> is <code>menu.play</code>). Comments and trailing commas are allowed.

```csharp
public static StringTableData ParseJson(string language, string json)
```

#### Parameters

`language` [string](https://learn.microsoft.com/dotnet/api/system.string)

`json` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [StringTableData](MicroEngine.Localization.StringTableData.md)

#### Exceptions

 [FormatException](https://learn.microsoft.com/dotnet/api/system.formatexception)

The text is not a table: it says what is wrong and on which line.

</div>
