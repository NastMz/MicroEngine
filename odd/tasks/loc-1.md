# LOC-1: localization

Objective: a game ships in several languages without building its own string tables, language switching, plural
rules, prompt names, per-language assets or checks. Branch `feat/loc-1`, one pull request; issue NastMz/MicroEngine#180,
part of [CONSOLIDATION-1](consolidation-1.md) (T12). Evaluation: [`docs/evaluations/loc-1.md`](../../docs/evaluations/loc-1.md);
decision: [ADR 0070](../../docs/decisions/0070-localization-in-the-core.md). Test runner:
`dotnet test MicroEngine.slnx -c Release`.

## Specs

Quoted fragments are the issue's text, verbatim (L1).

- S1 "string tables as content (format chosen after evaluating existing ones such as gettext PO, Fluent and plain
  JSON or CSV)".
- S2 "keys with arguments and plural rules that do not need ICU".
- S3 "language change at run time with UI refreshed"; a fallback language; the system's language as the default
  (the brief's wording).
- S4 "localized key and button names in prompts".
- S5 "font and fallback chains per language". "Text shaping stays in its package."
- S6 "localized asset variants through the content source".
- S7 "Missing keys are reported by a check that can run in CI."
- S8 "A sample switches language at run time, including a right-to-left and a CJK language, with prompts, plurals and
  a localized image."
- S9 "Works trimmed, under NativeAOT and in the browser."
- S10 "Manual page, limitations page, ADR."

Assumption (mine): the persisted language choice is the game's own setting, kept with its other settings
(`Game.Saves`, ADR 0063); the engine offers `TrySetLanguage` for a saved value and stores nothing itself.

## Tasks

Route: inline, in this worktree. Status: `[ ]` pending, `[x]` done with its evidence.

| ID | Task | Specs | Status |
| --- | --- | --- | --- |
| T1 | Messages, plural rules, tables and the localizer in the kernel | S1, S2, S3 | [x] `MessageTests`, `PluralRulesTests`, `LocalizerTests` |
| T2 | `Game.Localization`: tables from content, system language, asset variants, log, state hash; widgets and prompts follow the language | S3, S4, S6 | [x] `GameLocalizationTests` |
| T3 | `microengine content check` reads string tables | S7 | [x] `StringTableCheckTests`, `ContentTests` |
| T4 | Fonts per language: what a table names, the characters a language shows, coverage | S5 | [x] `StringTableCheckTests.AFontSaysWhichCharactersItHas`; the game builds the font (L7) |
| T5 | Example in the tour, its test and its capture | S8 | [x] without a CJK language (L6); `LocalizationExampleTests` |
| T6 | Manual page, limitations, release notes, API reference, ADR, evaluation | S10 | [x] |
| T7 | Trimmed, NativeAOT and browser behaviour | S9 | trimmed [x] (L8); NativeAOT and browser not verified |

## Log

- L1 (2026-11-20, the issue's text). "## Problem The engine has no localization at all and it was never evaluated: UI
  labels hold plain strings, key names in prompts are English, and a game that ships in more than one language builds
  string tables, language switching, plural rules, font selection per script and localized asset selection itself.
  Mobile and web builds set `InvariantGlobalization`, so culture data is not available from the runtime. ## Target A
  localization service: string tables as content (format chosen after evaluating existing ones such as gettext PO,
  Fluent and plain JSON or CSV), keys with arguments and plural rules that do not need ICU, language change at run
  time with UI refreshed, localized key and button names in prompts, font and fallback chains per language, and
  localized asset variants through the content source. Text shaping stays in its package. ## Acceptance - [ ] A sample
  switches language at run time, including a right-to-left and a CJK language, with prompts, plurals and a localized
  image. - [ ] Missing keys are reported by a check that can run in CI. - [ ] Works trimmed, under NativeAOT and in
  the browser. - [ ] Manual page, limitations page, ADR. Depends on MODULE-1; coordinates with CONTENT-1 #113."
- L2 (2026-11-20) Audit confirmed against the code: no localization type exists; `Label`, `Button` and
  `TextField.Placeholder` hold plain strings; `ActionPrompts.KeyName`, `ButtonName`, `PointerNames` and the words
  "D-pad" and "Stick" are English constants; the web, Android and iOS hosts and `microengine package` set
  `InvariantGlobalization`; fonts have no coverage query (a missing glyph draws `?`); shaping and bidirectional
  layout exist in `MicroEngine.Text.Shaping`.
- L3 (2026-11-20) Experiment (scratch project, MessageFormat 8.0.0, trimmed, `InvariantGlobalization`):
  `new CultureInfo("ru")` throws `CultureNotFoundException`; with the invariant culture the library formats
  `{count, plural, one {# gem} other {# gems}}` as "1 gems"; a call allocates 1,323 bytes; a placeholder without a
  value (`{attack}`) throws `VariableNotFoundException`. Decided: own formatter with the same syntax, rules keyed by
  the language tag (evaluation, section C).
- L4 (2026-11-20) T1 and T2 done: `MessageTests`, `PluralRulesTests`, `LocalizerTests`, `GameLocalizationTests`
  (commit `197a214a`). The table format is JSON per language and CSV per table, named `*.strings.json` and
  `*.strings.csv` so the loader and the content check recognise a table without opening every JSON file.
- L5 (2026-11-20) T3 and T4 done: `StringTableChecks` (kernel), `FontCoverage` (a character-map reader, formats 4 and
  12), and the tables read by `microengine content check` (commit `4ba0c4bc`). `prompt.` keys are optional per
  language: a first version reported them as missing in English.
- L6 (2026-11-20) T5: the example shows English, Spanish, Russian and Arabic. Russian replaces the CJK language the
  issue asks for: the repository has no CJK font, none with an open licence is on this machine, and adding one is a
  download and a third-party file the owner approves. Japanese is covered by tests of the texts (plural rule,
  `Characters`, the coverage warning), not on screen. The localized image is an arrow with a variant for Arabic.
  Captures looked at (English, Spanish, Russian, Arabic): shaped Arabic from the right, "Z / Espacio / Intro / J",
  "2 самоцвета", "جوهرتان", the mirrored arrow. In the full tour the hint reads "CONFIRM" and "ACTION": the touch
  example earlier in the tour leaves touch as the prompts' device, as for every other example's prompts.
- L7 (2026-11-20) Not done, outside this change's surface: an `ISystemLanguages` implementation in the SDL platform
  (`SDL_GetPreferredLocales`) and in the browser host (`navigator.languages`); `PromptText` with any `IFont`; a
  `ShapedFont` asset kind; `docs/reference/components.md` and `AGENTS.md` naming this record; `sdk/tests/trimmed.mjs`
  learning a localized game.
- L8 (2026-11-20) T7: a scratch console program over this branch's `MicroEngine` project, published trimmed
  (`TrimMode=full`, warnings as errors, `InvariantGlobalization`) and run from its executable: no trim warning,
  "culture data: False", and 15 of 15 checks pass (tables from a folder and a CSV, English, Russian and Arabic
  plurals, fallback, variants, the check). The NativeAOT publish stopped at "Platform linker not found" on this
  machine: not verified. No browser or phone build was made.
- L9 (2026-11-20) `dotnet build MicroEngine.slnx -c Release`: 0 warnings, 0 errors. `dotnet test MicroEngine.slnx -c
  Release`: every project passes except `AssetGalleryTests.TheScriptedTourReportsNoHitch`, which passes when run
  alone (the timing-sensitive test the brief names).
