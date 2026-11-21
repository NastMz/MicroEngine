# Namespace MicroEngine.Localization

<div data-pagefind-weight="0.3">

### Classes

 [FontCoverage](MicroEngine.Localization.FontCoverage.md)

The characters a TrueType or OpenType font file has glyphs for, read from its character map without rasterizing
anything: which characters of a language's texts a font would draw as <code>?</code>. The content check uses it for the
fonts a string table names; a game can ask the same of the fonts it loads
(<code>FontCoverage.Read(Assets.Bytes("fonts/ui.ttf")).Missing(Localization.Characters())</code>).

 [GameLocalization](MicroEngine.Localization.GameLocalization.md)

A game's texts and language ([Localization](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Localization)): a [Localizer](MicroEngine.Localization.Localizer.md) that loads its string
tables from the game's content, starts in the system's language when the game has it, names the variant of an
asset for the language in use, and writes what is missing to the log.

 [LanguageInfo](MicroEngine.Localization.LanguageInfo.md)

A language a game has text in.

 [LanguageTag](MicroEngine.Localization.LanguageTag.md)

Language tags as tables and players write them: <code>en</code>, <code>pt-BR</code>, <code>es_MX</code>, <code>zh-Hans</code>.

 [LocalizationIssue](MicroEngine.Localization.LocalizationIssue.md)

Something wrong with a game's string tables, found by [StringTableChecks](MicroEngine.Localization.StringTableChecks.md).

 [Localizer](MicroEngine.Localization.Localizer.md)

The texts of a game in each of its languages, and the language in use. Text is asked for by key
(<code>text["menu.play"]</code>, <code>text.Format("hud.gems", ("count", 3))</code>) and comes from the table of
[Language](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_Language), then from its parent language (<code>es</code> for <code>es-MX</code>), then from
[FallbackLanguage](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_FallbackLanguage). Changing the language raises [Changed](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_Changed); widgets that hold a
[LocalizedText](MicroEngine.Localization.LocalizedText.md) follow it by themselves. Games get one in <code>Game.Localization</code>, which also loads
tables from content files.

 [MessageArgument](MicroEngine.Localization.MessageArgument.md)

An argument a [Message](MicroEngine.Localization.Message.md) names, for tools that compare the translations of a text.

 [Message](MicroEngine.Localization.Message.md)

A text with named values, plural forms and selections, in the ICU MessageFormat syntax translation tools write:
<code>{who} found {count, plural, =0 {nothing} one {# gem} other {# gems}}</code>. Parsed once, formatted any number of
times. String tables hold these; [Localizer](MicroEngine.Localization.Localizer.md) formats them in the language in use.

 [PluralRules](MicroEngine.Localization.PluralRules.md)

Which plural form a number takes in a language, from rules built into the engine: nothing is read from the
operating system or the runtime's culture data, so trimmed, NativeAOT, phone and browser builds count alike.

 [StringTableChecks](MicroEngine.Localization.StringTableChecks.md)

Compares the string tables of a game's languages, as <code>microengine content check</code> does: a key a language
lacks, a message that cannot be read, a value one translation names and the others do not, a plural without the
forms its language uses. A game can run it in a test of its own over the tables it loads.

 [StringTableData](MicroEngine.Localization.StringTableData.md)

The texts of one language as a file gives them, with what the file says about the language.

 [StringTables](MicroEngine.Localization.StringTables.md)

Reads string tables: a JSON file per language (<code>es.strings.json</code>) or a CSV file with a column per language
(<code>game.strings.csv</code>). Both hold a message per key ([Message](MicroEngine.Localization.Message.md)); keys starting with <code>@</code>
describe the language (<code>@name</code>, <code>@fonts</code>, <code>@direction</code>).

### Structs

 [LocalizedText](MicroEngine.Localization.LocalizedText.md)

A text named by its key, with the values it shows: what a widget keeps so its text follows the language
(<code>new Label(new LocalizedText("hud.gems", ("count", gems)))</code>). [Format](MicroEngine.Localization.Localizer.md#MicroEngine_Localization_Localizer_Format_System_String_System_ReadOnlySpan_MicroEngine_Localization_TextArgument__)
gives the text in the language in use.

 [TextArgument](MicroEngine.Localization.TextArgument.md)

A named value a message shows or chooses by: <code>("count", 3)</code>, <code>("who", "Ana")</code>. Tuples of a name and a
string or a number convert to it, so calls read <code>Format("gems", ("count", gems))</code>; nothing is boxed.

### Interfaces

 [ISystemLanguages](MicroEngine.Localization.ISystemLanguages.md)

An optional feature of a platform: the languages the player set on the system, most wanted first, as tags
(<code>es-MX</code>, <code>en</code>). The game's first language is the first of them it has text in. A platform that cannot
tell offers no such feature, and the runtime's own culture is used where the build carries culture data.

### Enums

 [MessageArgumentKind](MicroEngine.Localization.MessageArgumentKind.md)

How a message uses one of its arguments.

 [PluralCategory](MicroEngine.Localization.PluralCategory.md)

The form a language uses for a number: English has two ([One](MicroEngine.Localization.PluralCategory.md) for 1, [Other](MicroEngine.Localization.PluralCategory.md) for the rest),
Russian four, Arabic six, Japanese one. A plural message gives a text per category
(<code>{count, plural, one {# gem} other {# gems}}</code>).

</div>
