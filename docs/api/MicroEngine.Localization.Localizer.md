# Class Localizer

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Localization](MicroEngine.Localization.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Graphics](../manual/rendering.md)  

The texts of a game in each of its languages, and the language in use. Text is asked for by key
(<code>text["menu.play"]</code>, <code>text.Format("hud.gems", ("count", 3))</code>) and comes from the table of
[Language](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_Language), then from its parent language (<code>es</code> for <code>es-MX</code>), then from
[FallbackLanguage](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_FallbackLanguage). Changing the language raises [Changed](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_Changed); widgets that hold a
[LocalizedText](MicroEngine.Localization.LocalizedText.md) follow it by themselves. Games get one in <code>Game.Localization</code>, which also loads
tables from content files.

```csharp
public class Localizer
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Localizer](MicroEngine.Localization.Localizer.md)

## Constructors

### <a id="MicroEngine_Localization_Localizer__ctor"></a> Localizer\(\)

```csharp
public Localizer()
```

## Properties

### <a id="MicroEngine_Localization_Localizer_Current"></a> Current

The language in use, with its name, direction and fonts.

```csharp
public LanguageInfo Current { get; }
```

#### Property Value

 [LanguageInfo](MicroEngine.Localization.LanguageInfo.md)

### <a id="MicroEngine_Localization_Localizer_DefaultLanguage"></a> DefaultLanguage

The language in use until one is chosen ([LanguageChosen](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_LanguageChosen)): the table matching this tag, as
[Match](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_Match_System_Collections_Generic_IEnumerable_System_String__) finds it. Null, or a language the game has no text in, leaves [FallbackLanguage](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_FallbackLanguage)
in use. The engine sets it to the system's language when tables are loaded.

```csharp
public string? DefaultLanguage { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Localization_Localizer_FallbackLanguage"></a> FallbackLanguage

The language whose text is shown when the language in use lacks a key, and the language in use until another
is chosen: <code>en</code> by default. When no table is for it, the first table loaded takes its place.

```csharp
public string FallbackLanguage { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Localization_Localizer_HasTables"></a> HasTables

Whether any table was added.

```csharp
public bool HasTables { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Localization_Localizer_Language"></a> Language

The language in use: a tag of [Languages](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_Languages). Until one is chosen it is [FallbackLanguage](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_FallbackLanguage).
Setting it takes the table of that tag, else of its language (<code>es</code> for <code>es-AR</code>), whatever the letter
case, and raises [Changed](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_Changed) when the language is another one.

```csharp
public string Language { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

No table is for that language (tables exist, and none matches).

### <a id="MicroEngine_Localization_Localizer_LanguageChosen"></a> LanguageChosen

Whether the game or the player chose [Language](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_Language); false while it is still the default.

```csharp
public bool LanguageChosen { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Localization_Localizer_Languages"></a> Languages

The languages the game has text in, in the order their tables were added.

```csharp
public IReadOnlyList<LanguageInfo> Languages { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[LanguageInfo](MicroEngine.Localization.LanguageInfo.md)\>

### <a id="MicroEngine_Localization_Localizer_MissingKeys"></a> MissingKeys

The keys asked for that no table of the language in use, its parents or the fallback language has, in the order they were asked.

```csharp
public IReadOnlyList<string> MissingKeys { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Localization_Localizer_Report"></a> Report

Receives, once each, a sentence about what is wrong with the text asked for: a key no language has, a key the
language in use lacks (shown from the fallback language), a message that cannot be read. The engine writes
them to the log.

```csharp
public Action<string>? Report { get; set; }
```

#### Property Value

 [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>?

### <a id="MicroEngine_Localization_Localizer_RightToLeft"></a> RightToLeft

Whether the language in use reads right to left.

```csharp
public bool RightToLeft { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Localization_Localizer_Version"></a> Version

Changes whenever a text may have changed: another language, more tables. Code that lays text out once compares
it to know when to do so again.

```csharp
public int Version { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Localization_Localizer_Item_System_String_"></a> this\[string\]

The text of <code class="paramref">key</code> in the language in use; the same as [Format](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_Format_System_String_System_ReadOnlySpan_MicroEngine_Localization_TextArgument__) without values.

```csharp
public string this[string key] { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Localization_Localizer_Add_MicroEngine_Localization_StringTableData_"></a> Add\(StringTableData\)

Adds the texts of a table read from a file ([StringTables](MicroEngine.Localization.StringTables.md)) to its language; a key given again replaces the earlier text.

```csharp
public void Add(StringTableData table)
```

#### Parameters

`table` [StringTableData](MicroEngine.Localization.StringTableData.md)

### <a id="MicroEngine_Localization_Localizer_Add_System_String_System_Collections_Generic_IEnumerable_System_Collections_Generic_KeyValuePair_System_String_System_String___"></a> Add\(string, IEnumerable<KeyValuePair<string, string\>\>\)

Adds texts to <code class="paramref">language</code>: each key and its message. A key given again replaces the earlier text.

```csharp
public void Add(string language, IEnumerable<KeyValuePair<string, string>> entries)
```

#### Parameters

`language` [string](https://learn.microsoft.com/dotnet/api/system.string)

`entries` [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[KeyValuePair](https://learn.microsoft.com/dotnet/api/system.collections.generic.keyvaluepair\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [string](https://learn.microsoft.com/dotnet/api/system.string)\>\>

### <a id="MicroEngine_Localization_Localizer_Characters_System_String_System_Boolean_"></a> Characters\(string?, bool\)

Every character the texts of <code class="paramref">language</code> (the language in use when null) can show, each once,
in code order: what to hand to a font's <code>Prepare</code> so no glyph is rasterized during play, and what its font
must cover. Includes the language's own name, the texts of its parents and of the fallback language, which fill in
missing keys, and the printable ASCII characters (numbers and names put into messages) unless <code class="paramref">includeAscii</code> is false.

```csharp
public string Characters(string? language = null, bool includeAscii = true)
```

#### Parameters

`language` [string](https://learn.microsoft.com/dotnet/api/system.string)?

`includeAscii` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Localization_Localizer_Format_System_String_System_ReadOnlySpan_MicroEngine_Localization_TextArgument__"></a> Format\(string, params ReadOnlySpan<TextArgument\>\)

The text of <code class="paramref">key</code> in the language in use, with <code class="paramref">arguments</code> in place:
<code>Format("hud.gems", ("count", 3))</code>. A key no table has gives the key itself, and is reported once
([Report](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_Report), [MissingKeys](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_MissingKeys)). A text without values is returned as stored: asking for it
every frame allocates nothing.

```csharp
public string Format(string key, params ReadOnlySpan<TextArgument> arguments)
```

#### Parameters

`key` [string](https://learn.microsoft.com/dotnet/api/system.string)

`arguments` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[TextArgument](MicroEngine.Localization.TextArgument.md)\>

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Localization_Localizer_Format_MicroEngine_Localization_LocalizedText_"></a> Format\(LocalizedText\)

The text of <code class="paramref">text</code> in the language in use, as [Format](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_Format_System_String_System_ReadOnlySpan_MicroEngine_Localization_TextArgument__).

```csharp
public string Format(LocalizedText text)
```

#### Parameters

`text` [LocalizedText](MicroEngine.Localization.LocalizedText.md)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Localization_Localizer_Has_System_String_System_String_"></a> Has\(string, string?\)

Whether <code class="paramref">key</code> has a text: in the language in use, its parents or the fallback language, or in <code class="paramref">language</code> alone when given.

```csharp
public bool Has(string key, string? language = null)
```

#### Parameters

`key` [string](https://learn.microsoft.com/dotnet/api/system.string)

`language` [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Localization_Localizer_Keys_System_String_"></a> Keys\(string\)

The keys of <code class="paramref">language</code>'s own table (not its parents'); empty when it has none.

```csharp
public IReadOnlyCollection<string> Keys(string language)
```

#### Parameters

`language` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [IReadOnlyCollection](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlycollection\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Localization_Localizer_Match_System_Collections_Generic_IEnumerable_System_String__"></a> Match\(IEnumerable<string\>\)

The language of the game that best suits a player who prefers <code class="paramref">preferred</code>, most wanted first
(a system's language list, a saved choice): the first whose exact tag has a table, else whose language has one
(<code>es</code> for <code>es-AR</code>, or the only region the game has for <code>pt</code>). Null when none matches.

```csharp
public string? Match(IEnumerable<string> preferred)
```

#### Parameters

`preferred` [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Localization_Localizer_OnChanged"></a> OnChanged\(\)

Called after the language in use or its texts changed, before [Changed](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_Changed) is raised.

```csharp
protected virtual void OnChanged()
```

### <a id="MicroEngine_Localization_Localizer_Pattern_System_String_System_String_"></a> Pattern\(string, string\)

The message of <code class="paramref">key</code> as <code class="paramref">language</code>'s own table writes it, or null.

```csharp
public string? Pattern(string key, string language)
```

#### Parameters

`key` [string](https://learn.microsoft.com/dotnet/api/system.string)

`language` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Localization_Localizer_TextOrNull_System_String_System_ReadOnlySpan_MicroEngine_Localization_TextArgument__"></a> TextOrNull\(string, params ReadOnlySpan<TextArgument\>\)

The text of <code class="paramref">key</code> in the language in use, or null when no table has it; nothing is reported.

```csharp
public string? TextOrNull(string key, params ReadOnlySpan<TextArgument> arguments)
```

#### Parameters

`key` [string](https://learn.microsoft.com/dotnet/api/system.string)

`arguments` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[TextArgument](MicroEngine.Localization.TextArgument.md)\>

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Localization_Localizer_TrySetLanguage_System_String_"></a> TrySetLanguage\(string?\)

Sets [Language](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_Language) when a table matches <code class="paramref">language</code>, and says whether one did: for a
choice read from a save or from the system, which may name a language the game does not have. Before any table
is loaded every tag is taken.

```csharp
public bool TrySetLanguage(string? language)
```

#### Parameters

`language` [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Localization_Localizer_Changed"></a> Changed

Raised after the language in use or its texts changed.

```csharp
public event Action? Changed
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action)?

</div>
