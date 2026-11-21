# LOC-1: localization

Evaluation of 2026-11-20 for NastMz/MicroEngine#180, part of [CONSOLIDATION-1](consolidation-1.md). Decision:
[ADR 0070](../decisions/0070-localization-in-the-core.md). Record: [`odd/tasks/loc-1.md`](../../odd/tasks/loc-1.md).

## A. Need

Any game sold outside one country: a menu, a HUD with counts ("3 gems"), dialogue, a tutorial that names the button to
press, a title image with words in it, a voice line. Stores list the languages a game supports, and a second language
is usually the cheapest way to reach more players. The work is the same in every genre.

## B. What a developer writes today

Everything. Confirmed against the code:

| Where | What it shows |
| --- | --- |
| `src/MicroEngine.UI/Widgets.cs` (`Label`, `Button`), `TextField.Placeholder` | plain strings; a language change means rebuilding the interface |
| `src/MicroEngine/Input/ActionPrompts.cs` (`KeyName`, `ButtonName`, "D-pad", "Stick"), `PointerNames` | English constants: "Press Space" in every language |
| `src/MicroEngine.Cli/Hosts/{web,android,ios}/Host.csproj`, `Packaging.cs:72` | `InvariantGlobalization=true`: no culture data, so no plural rules and no system language from the runtime |
| `src/MicroEngine.Text/ScalableFont.cs` | a character the font lacks draws `?`; nothing says which characters a language needs or a font has |
| `src/MicroEngine.Cli/Content.cs` | the content check reads images, sounds, fonts, sheets; a JSON file that is not an atlas is skipped |
| the four demos | English strings typed into scenes |

So a game in two languages writes: a table format and its reader, a lookup with a fallback, plural selection per
language, a way to refresh every widget, translated key names, a choice of file per language, and a test that no
language lacks a key. What exists and helps: scalable fonts with fallback faces, and shaping with bidirectional
layout in `MicroEngine.Text.Shaping` (Arabic, Hebrew, Indic scripts).

## C. Alternatives

Mature solutions first. Versions and licences read from nuget.org on 2026-11-20.

| | Version | Licence | Targets | What it is |
| --- | --- | --- | --- | --- |
| .NET resources (`.resx`, `ResourceManager`, satellite assemblies) | in the runtime | MIT | every one | compiled string tables chosen by `CultureInfo` |
| MessageFormat (jeffijoe/messageformat.net) | 8.0.0 (2026-02-23) | MIT | net8.0, net10.0, netstandard2.0/2.1 | ICU MessageFormat: arguments, plural, select |
| SmartFormat | 3.6.2 (2026-10-25) | MIT | net462, net6.0, net8.0, netstandard2.0/2.1 | composite formatting with plural and choose extensions |
| NGettext | 0.6.7 (2021-09-17) | MIT | netstandard1.0 to 2.0 | reads compiled gettext `.mo` catalogues, evaluates `Plural-Forms` |
| Karambolo.PO | 1.13.0 (2026-03-22) | MIT | net9.0, netstandard1.0 to 2.1 | reads and writes gettext `.po` files |
| GetText.NET | 11.0.10-preview.1 (2026-06-21) | LGPL (package licence page) | netstandard2.0 | gettext runtime and extractor |
| Linguini.Bundle (Project Fluent) | 0.11.0 (2026-03-15) | MIT OR Apache-2.0 | net6.0, net8.0, netstandard2.1 | Fluent `.ftl` bundles |
| Fluent.Net | 1.0.63 (2024-11-07) | Apache-2.0 | netstandard2.0 | an older Fluent port |
| I18Next.Net | 2.0.0 (2026-10-04) | Apache-2.0 | net10.0, net6.0, net8.0, netstandard2.0 | the i18next JSON conventions |

**.NET resources.** The tables are compiled into satellite assemblies and chosen by `CultureInfo.CurrentUICulture`.
With `InvariantGlobalization`, which every packaged MicroEngine game has, `new CultureInfo("ru")` throws
`CultureNotFoundException` (measured below) unless the build also turns `PredefinedCulturesOnly` off, and then every
culture behaves as the invariant one. Resources have no plural forms; a translator edits XML and a build stands
between the file and the game; nothing checks that a key exists in every language. It would be a second content path
beside `Content/`, against ADR 0066 ("files load as they are").

**MessageFormat 8.0.0**, the closest fit for the message side, was run in a scratch project (`net10.0`, trimmed,
`InvariantGlobalization=true`, the settings of a packaged game):

- its formatter takes a `CultureInfo` to choose plural rules; `new CultureInfo("en")` and `("ru")` throw, and with the
  invariant culture `{count, plural, one {# gem} other {# gems}}` gives "1 gems" and Russian always takes `other`;
- one call with two arguments allocates 1,323 bytes (the values travel in a dictionary of objects) and takes about
  6 µs, with its pattern cache on;
- a placeholder without a value throws `VariableNotFoundException`, so "Press {attack}" cannot be handed on to the
  action prompts.

Linguini and Fluent.Net also take a `CultureInfo` for plural selection (their builders require one; not run).
The gettext libraries key messages by their source text and evaluate a C expression from the catalogue header for
plurals; that works without culture data, but a `.po` workflow (source strings in code, an extractor, `msgfmt`)
is a toolchain the engine would impose, and the catalogue has no place for what a game also needs per language
(name, fonts, direction).

**Formats.** What translators and translation platforms exchange without conversion: JSON of key and ICU message
(the format of FormatJS and of most platforms' exports), gettext PO, CSV (the default of Godot and of small teams
working in a spreadsheet), XLIFF. Fluent is the most expressive and the least supported by tools.

**The engine's own.** A message formatter with the ICU syntax, plural rules keyed by the language tag, tables as
content files. About 900 lines without a dependency.

## D. Target experience

Written before the API:

```csharp
protected override void Load()
{
    Localization.Load("text");                         // Content/text/en.strings.json, es.strings.json, ar...
    Localization.TrySetLanguage(settings.Language);    // the player's saved choice; else the system's language
}

// A menu that follows the language without code of its own:
menu.Add(new Button(new LocalizedText("menu.play"), Play));
gems.Localized = new("hud.gems", ("count", 3));                     // "3 gems", "3 gemas", "3 самоцвета"

// Text drawn by hand, a hint naming the control, an image with words in it:
Font.Draw(r, Game.Prompts.Format(Game.Localization["hint.jump"]), at, Color.White);   // "Pulsa Espacio para saltar"
r.Draw(Assets.Texture(Game.Localization.Path("ui/title.png")), at);                    // ui/title.es.png

// The language menu:
foreach (var language in Game.Localization.Languages) Add(language.Name, () => Game.Localization.Language = language.Code);
```

```json
{
  "@name": "Español",
  "menu": { "play": "Jugar", "quit": "Salir" },
  "hud.gems": "{count, plural, one {# gema} other {# gemas}}",
  "hint.jump": "Pulsa {jump} para saltar",
  "prompt.Space": "Espacio"
}
```

And in CI: `microengine content check` fails when a language lacks a key, a message does not parse, a translation
names a value the others do not, or a language's font lacks characters its texts show.

## E. Responsibility limit

The engine owns: the table formats and their reading, lookup and fallback, message formatting and plural rules, the
language in use and its change, widgets and prompts following it, the variant of an asset per language, the check.
The game owns: its keys and texts, where the choice is saved (its settings, ADR 0063), which fonts each language
uses, formatting numbers and dates beyond plain digits, and the layout decisions a right-to-left language asks for
beyond text alignment. Shaping stays in `MicroEngine.Text.Shaping`.

## F. Integration

- **Kernel**: `Message`, `PluralRules`, `StringTables`, `Localizer`, `LocalizedText`, `StringTableChecks` live in the
  kernel assembly (namespace `MicroEngine.Localization`), with no dependency, so the UI assembly can hold a
  `LocalizedText` without referencing the game.
- **Assets**: tables are read through the game's content source (folder, zip, a host's listing). A variant is a file
  beside the original (`title.es.png`), resolved to a path any loader takes; nothing in the asset store changes, and
  kinds a game registers get variants too.
- **UI**: `UiRoot.Localization`; `Label`, `Button` and `TextField` hold an optional `LocalizedText` and resolve it
  when the localizer's version moves. Labels start from the right in a right-to-left language.
- **Input**: `ActionPrompts.Names` translates the engine's English names; the game's localization sets it to the
  table's `prompt.<English name>` keys and moves the prompts' version with the language.
- **Saves**: none; `TrySetLanguage` takes a saved value that may be out of date.
- **State hash and determinism**: `engine.localization` (the language) once a table is loaded; games without tables
  hash as before. A deterministic run never reads the machine's language.
- **Content check**: `microengine content check` reads the tables with the engine's own readers.
- **Scene systems (ADR 0060)**: not used; the language is the game's, not a scene's.

## G. Trade-offs

- An own formatter is a subset of ICU MessageFormat to keep: `selectordinal`, `offset:` and the date, time and
  number styles are refused at parse time instead of half-working.
- Plural rules are code in the kernel (a few hundred bytes per family) instead of CLDR data: about 65 languages,
  fractions approximated, `Register` for the rest.
- The file names `<language>.strings.json` and `<name>.strings.csv` are a convention this project now owns; they let
  the content check and the loader recognise a table without opening every JSON file.
- No built-in translations of key names: a Spanish name with an accent would draw `?` in the built-in font, and the
  list would be the engine's to maintain in every language.
- Every language of a loaded table is in memory; texts are parsed on first use.

## H. Decision

**New, in the core** (the audit's level for this area). Reasons: the mature message libraries cannot count without
culture data, which packaged games do not have; resources and gettext bring a build step and a second content path;
the remaining part (tables, switching, widgets, prompts, variants, the check) is integration no library provides.
The syntax is the standard one, so a game can still move its tables to a platform or another runtime.

## I. Validation

- `MessageTests`, `PluralRulesTests`, `LocalizerTests`, `GameLocalizationTests`, `StringTableCheckTests`,
  `FontCoverageTests` (kernel and engine), `ContentTests` (the tool), `LocalizationExampleTests` (the tour).
- The example captured in English, Spanish, Russian and Arabic, and looked at.
- A console program over the engine published trimmed with `InvariantGlobalization`, warnings as errors, and run:
  no culture data, 15 of 15 checks (tables, plurals in three languages, fallback, variants, the check).
- Not verified here: a browser or phone build; a NativeAOT build (this machine has no native linker; the kernel
  assembly is marked AOT-compatible and builds clean with its analyzers); a CJK language on screen, for lack of a
  CJK font in the repository, so the example shows Russian in its place.
