# 0070 — Localization in the core: string tables as content, ICU messages with built-in plural rules

Status: Accepted (2026-11-20). The SDK is in Alpha: new public types carry no compatibility commitment. Evidence:
[LOC-1](../evaluations/loc-1.md). Issue NastMz/MicroEngine#180, part of
[CONSOLIDATION-1](../evaluations/consolidation-1.md). Builds on
[0066](0066-content-production-as-data-and-optional-tools.md) (content loads as it is, `microengine content`),
[0062](0062-one-control-representation.md) (prompts), [0054](0054-saves-without-reflection.md) (trimmed and NativeAOT
builds) and [0063](0063-versioned-saves-and-the-games-store.md) (settings).

## Context

The engine had no localization and had never evaluated it. Widgets held plain strings, prompts named keys in English,
and a game in two languages wrote its own tables, lookup, plural rules, refresh of the interface, choice of file per
language and check for missing translations. Every packaged game, and every browser and phone build, runs with
`InvariantGlobalization`: the runtime has no culture data there.

That last fact decides the message side. MessageFormat 8.0.0, the mature .NET implementation of ICU MessageFormat,
chooses plural forms through `CultureInfo`: under the settings of a packaged game `new CultureInfo("ru")` throws, and
with the invariant culture "1 gem" becomes "1 gems". It also allocates about 1.3 kB per call and throws for a
placeholder without a value. The Fluent libraries take a `CultureInfo` as well. .NET resources and gettext bring a
build step and a second content path, against ADR 0066.

## Decision

- **Messages in the ICU MessageFormat syntax, formatted by the engine.** `Message` reads `{name}`,
  `{n, plural, ...}` (exact numbers, the language's form, `other`), `{x, select, ...}` and apostrophe quoting.
  `selectordinal`, `offset:` and the date, time and number styles are refused when parsing. A name without a value
  stays as written, so one text carries both values and action prompts.
- **Plural rules are code, keyed by the language tag** (`PluralRules`): CLDR's cardinal rules for whole numbers in
  about 65 languages, fractions approximated, `Register` for others. No `CultureInfo` anywhere.
- **String tables are content files**, recognised by name: `<language>.strings.json` (one language, nested keys,
  comments allowed) and `<name>.strings.csv` (a column per language). Keys starting with `@` describe the language
  (`@name`, `@fonts`, `@direction`). No build step, no generated code.
- **`Localizer` in the kernel assembly** (namespace `MicroEngine.Localization`), without dependencies: tables,
  the language in use, the chain region, language, fallback language, `Changed` and `Version`, reports of missing
  keys. `GameLocalization` (`Game.Localization`) adds loading from the content source, the system's language as the
  default, the variant of a file per language and the log.
- **The first language** is the first of the system's languages the game has: from the platform when it offers
  `ISystemLanguages`, else from the runtime's interface culture. A deterministic run reads neither. The player's
  choice is the game's setting; the engine stores nothing.
- **Widgets hold a `LocalizedText`** (`Label`, `Button`, `TextField`), resolved when the localizer's version moves;
  labels start from the right in a right-to-left language.
- **Prompt names are translated by the tables** (`prompt.<English name>`), through `ActionPrompts.Names`. The engine
  ships no translations of its own.
- **A file has a variant per language beside it** (`title.es.png`), resolved to a path by `Localization.Path`. The
  asset store is unchanged.
- **The language is in the state hash** (`engine.localization`) once a game has tables.
- **`microengine content check` reads the tables** with the engine's readers: missing keys, unreadable messages,
  values and plural forms that disagree, fonts missing or lacking characters (`FontCoverage`, a character-map
  reader), variants the game would not find. `StringTableChecks` is the same comparison for a game's tests.

## Alternatives

- **MessageFormat (or SmartFormat) as a dependency of the core.** Counts wrongly without culture data, which is the
  normal state of a shipped game; fixing that means replacing its plural selection, the part it would be taken for.
- **An optional package over a library.** Widgets and prompts must hold and resolve localized text, so the contract
  has to be where the UI assembly can see it; a package would leave the core with strings and the game with glue.
- **.NET resources and satellite assemblies.** A compiled second content path chosen by `CultureInfo`, no plurals,
  nothing a content check can read.
- **gettext PO, Fluent, XLIFF as the table format.** Each is exported to JSON or CSV by the tools that write it; PO
  keys texts by their English source and Fluent needs its own runtime. Readers can be added as table sources later:
  `Localizer.Add` takes entries from anywhere.
- **Translations of key names built into the engine.** A list to maintain in every language, whose accented names
  would draw `?` in the built-in font.
- **Variants inside the asset store** (a language dimension on every key). Every loader and cache would learn about
  languages to save the game one call.

## Consequences

- A game in several languages writes tables and keys; it no longer writes lookup, plural selection, interface
  refresh, prompt names, file choice or the missing-key test.
- The engine owns a message parser, plural rules and two file-name conventions, and must keep reading them.
- Without a platform implementation of `ISystemLanguages`, browser, phone and packaged builds start in the fallback
  language: the SDL and browser platforms should offer it (outside this change).
- Right-to-left support is text direction and label alignment; mirroring layouts, a shaped-font asset kind and
  prompt glyphs in any font are separate work.
- A source of numbers, dates and currencies per language is not part of it.

## Verification

`MessageTests`, `PluralRulesTests`, `LocalizerTests`, `StringTableCheckTests`, `GameLocalizationTests`,
`ContentTests` (string tables), `LocalizationExampleTests`; the example captured in English, Spanish, Russian and
Arabic and looked at. Not verified: a browser, phone or NativeAOT build of a game using it; a Chinese, Japanese or
Korean language on screen (no such font in the repository).
