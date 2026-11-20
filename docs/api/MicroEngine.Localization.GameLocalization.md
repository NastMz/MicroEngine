# Class GameLocalization

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Localization](MicroEngine.Localization.md)  
Assembly: MicroEngine.dll  
Guide: [Graphics](../manual/rendering.md)  

A game's texts and language ([Localization](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Localization)): a [Localizer](MicroEngine.Localization.Localizer.md) that loads its string
tables from the game's content, starts in the system's language when the game has it, names the variant of an
asset for the language in use, and writes what is missing to the log.

```csharp
public sealed class GameLocalization : Localizer
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Localizer](MicroEngine.Localization.Localizer.md) ← 
[GameLocalization](MicroEngine.Localization.GameLocalization.md)

#### Inherited Members

[Localizer.TrySetLanguage\(string?\)](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_TrySetLanguage_System_String_), 
[Localizer.Add\(StringTableData\)](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_Add_MicroEngine_Localization_StringTableData_), 
[Localizer.Add\(string, IEnumerable<KeyValuePair<string, string\>\>\)](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_Add_System_String_System_Collections_Generic_IEnumerable_System_Collections_Generic_KeyValuePair_System_String_System_String___), 
[Localizer.Match\(IEnumerable<string\>\)](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_Match_System_Collections_Generic_IEnumerable_System_String__), 
[Localizer.Has\(string, string?\)](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_Has_System_String_System_String_), 
[Localizer.Keys\(string\)](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_Keys_System_String_), 
[Localizer.Pattern\(string, string\)](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_Pattern_System_String_System_String_), 
[Localizer.TextOrNull\(string, params ReadOnlySpan<TextArgument\>\)](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_TextOrNull_System_String_System_ReadOnlySpan_MicroEngine_Localization_TextArgument__), 
[Localizer.Format\(string, params ReadOnlySpan<TextArgument\>\)](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_Format_System_String_System_ReadOnlySpan_MicroEngine_Localization_TextArgument__), 
[Localizer.Format\(LocalizedText\)](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_Format_MicroEngine_Localization_LocalizedText_), 
[Localizer.Characters\(string?, bool\)](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_Characters_System_String_System_Boolean_), 
[Localizer.Language](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_Language), 
[Localizer.LanguageChosen](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_LanguageChosen), 
[Localizer.DefaultLanguage](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_DefaultLanguage), 
[Localizer.FallbackLanguage](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_FallbackLanguage), 
[Localizer.Languages](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_Languages), 
[Localizer.Current](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_Current), 
[Localizer.RightToLeft](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_RightToLeft), 
[Localizer.HasTables](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_HasTables), 
[Localizer.Version](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_Version), 
[Localizer.Report](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_Report), 
[Localizer.MissingKeys](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_MissingKeys), 
Localizer.this\[string\], 
[Localizer.Changed](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_Changed), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring), 
[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode)

## Examples

```csharp
protected override void Load()
{
    Localization.Load("text");                          // Content/text/en.strings.json, es.strings.json...
    Localization.TrySetLanguage(settings.Language);     // the player's saved choice, when there is one
    Scenes.Push(new TitleScene());
}

// In a scene:
var play = new Button(new LocalizedText("menu.play"), Play);            // follows the language by itself
string gems = Game.Localization.Format("hud.gems", ("count", 3));       // "3 gems", "3 gemas"
var title = Assets.Texture(Game.Localization.Path("ui/title.png"));     // ui/title.es.png in Spanish
```

## Constructors

### <a id="MicroEngine_Localization_GameLocalization__ctor_System_Func_MicroEngine_Platform_IContentSource__"></a> GameLocalization\(Func<IContentSource\>\)

The texts of a game whose content is <code class="paramref">source</code> (read when a table is loaded or a variant looked up).

```csharp
public GameLocalization(Func<IContentSource> source)
```

#### Parameters

`source` [Func](https://learn.microsoft.com/dotnet/api/system.func\-1)<[IContentSource](MicroEngine.Platform.IContentSource.md)\>

## Properties

### <a id="MicroEngine_Localization_GameLocalization_SystemLanguages"></a> SystemLanguages

The player's languages on this system, most wanted first: what the platform says
([ISystemLanguages](MicroEngine.Localization.ISystemLanguages.md)), else the runtime's interface culture where the build carries culture data.
Empty in a deterministic run (scripted runs, tests, captures), so those never depend on the machine. A host
or a test may set it. The first of them the game has text in is the language in use until one is chosen.

```csharp
public IReadOnlyList<string> SystemLanguages { get; set; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

## Methods

### <a id="MicroEngine_Localization_GameLocalization_Load_System_String_"></a> Load\(string\)

Loads string tables from the game's content: every table of a folder (<code>Load("text")</code> reads
<code>text/en.strings.json</code>, <code>text/es.strings.json</code>, <code>text/extra.strings.csv</code>...), or one file. A
JSON table is one language, named by its file (<code>pt-BR.strings.json</code>); a CSV table has a column per
language. Tables add up: loading more text for a language (another folder, a level's texts) keeps what it
had, and a key given again takes the newer text. The first load makes the system's language the language in
use when the game has it and has not chosen one.

```csharp
public GameLocalization Load(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [GameLocalization](MicroEngine.Localization.GameLocalization.md)

#### Exceptions

 [AssetLoadException](MicroEngine.Core.AssetLoadException.md)

The folder or file does not exist, the folder holds no table, or a table cannot be read (the message names the file and the line).

### <a id="MicroEngine_Localization_GameLocalization_OnChanged"></a> OnChanged\(\)

Called after the language in use or its texts changed, before [Changed](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_Changed) is raised.

```csharp
protected override void OnChanged()
```

### <a id="MicroEngine_Localization_GameLocalization_Path_System_String_"></a> Path\(string\)

The file to load for <code class="paramref">path</code> in the language in use: its variant when the content has one,
else the path itself. A variant carries the language tag before the file's extensions:
<code>ui/title.png</code> has <code>ui/title.es.png</code> for Spanish and <code>ui/title.pt-BR.png</code> for Brazilian
Portuguese; a region without a variant of its own takes its language's. Pass the result to any loader
(<code>Assets.Texture(Localization.Path("ui/title.png"))</code>, a sound, a font, a kind of the game's own). The
answer is remembered per language, so asking while drawing allocates nothing.

```csharp
public string Path(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Localization_GameLocalization_Variant_System_String_System_String_"></a> Variant\(string, string\)

The name of the variant of <code class="paramref">path</code> for <code class="paramref">language</code>: the tag goes before the file's extensions (<code>glow.frag.shader</code> is <code>glow.es.frag.shader</code>).

```csharp
public static string Variant(string path, string language)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

`language` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Localization_GameLocalization_Variants_System_String_"></a> Variants\(string\)

<code class="paramref">path</code> and every variant of it the content has for the game's languages, the file itself
first: what to preload so changing the language loads nothing during play.

```csharp
public IReadOnlyList<string> Variants(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

</div>
