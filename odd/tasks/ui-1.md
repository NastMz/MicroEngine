# UI-1: UI primitives for any game's interface

Objective: an external developer can build any 2D game's interface from UI primitives the engine documents. Genre
components leave the engine and become examples. Issue NastMz/MicroEngine#125. Branch `docs/ui-1` for the plan, then
one `feat/ui-<topic>` branch per slice, from `main`. Delivery strategy: `ask-on-risk`, as chained pull requests, one
per slice; the forecast is well over 400 authored lines. Test runner: `dotnet test MicroEngine.slnx -c Release`
(focused: `tests/MicroEngine.Tests` UI tests, `tests/Samples.Tests`). Evaluation:
[`docs/evaluations/ui-1.md`](../../docs/evaluations/ui-1.md). Decision:
[ADR 0050](../../docs/decisions/0050-ui-primitives-not-components.md).

## Specs

- S1 Judge every gap by an external developer, not by the demos: "si un usuario externo tuviera que hacer un juego de
  ese estilo, ¿podría con lo que tiene hoy?" and "MicroEngine es un motor generalista para que cualquiera cree juegos,
  no solo nosotros ni nuestras demos".
- S2 The deliverable is the implementation the research justifies: "el entregable no es la investigación. Es decir, si
  la investigación da como una posibilidad de implementación, pues se procede con implementación."
- S3 Primitives, not a library: "el chiste del motor es darte las primitivas para que tú montes cualquier tipo de
  sistema. Pero no volverse una biblioteca de assets."
- S4 Composition: "que todos los componentes de alto nivel se puedan construir con componentes de bajo nivel",
  "sin skinning ni componentes de género". Assumption, recorded in ADR 0050: the engine ships no designed looks
  (skins), but it does offer the means to apply one (9-slice drawing, an image in `PanelStyle`, P6).
- S5 Dialogue box out: "Dialogbox no debería ser ahí, porque se puede, como dices, construir con panel y otras cosas."
- S6 Selection list: "Selectionless habría que ver si falta alguna primitiva que permita construirla por encima".
- S7 Existing controls: "si mantenerlos ahí o sacarlos" and, when a demo uses one, "los trasladamos a la demo".
- S8 Primitives P1–P8 and the components that leave, as ADR 0050 lists them. Each primitive comes with the example
  that needs it, its tests (public behaviour) and the UI manual's documentation. Every SDK change is listed under
  `## Next release` in `docs/guide/release-notes.md`, and breaking ones in `docs/migrations.md`.
- S9 No release breaks a game without notice: a component is deprecated (`[Obsolete]`) one SDK release before it is
  removed.

## Tasks

| Task | Specs | Route | Status | Commit / evidence |
| --- | --- | --- | --- | --- |
| T1 Evaluation, ADR 0050 and this plan | S1–S8 | inline | done | this branch |
| T2 P1 pointer events and the wheel on widgets; settings screen example (slider, toggle, value selector) | S3 S4 S8 | inline | done | `feat/ui-pointer`; RED: 5 missing members; GREEN: `WidgetTests` 24/24, `Samples.Tests` 38/38 |
| T3 P2 clipping and a scrolling container; long list example | S3 S8 | inline | done | `feat/ui-scroll`; RED: `ScrollView` missing, then the nested-clip test alone; GREEN: UI tests 29/29, `Samples.Tests` 40/40 |
| T4 P3 grid layout; inventory example | S3 S8 | inline | done | `feat/ui-grid` (stacked on `feat/ui-scroll`); RED: `Grid` missing; GREEN: UI tests 32/32, `Samples.Tests` 42/42 |
| T5 P4 positioned placement, per-widget offset and opacity; speech bubble and banner examples | S3 S8 | inline | done | `feat/ui-layer` (stacked on `feat/ui-grid`); RED: members and `Layer` missing; GREEN: UI tests 35/35, `Samples.Tests` 44/44 |
| T6 P5 `Label` wrapping and visible characters; dialogue box example | S3 S5 S8 | inline | done | `feat/ui-text` (stacked on `feat/ui-layer`); RED: `Wrap`, `Lines`, `VisibleCharacters`, `VisibleLines` missing; GREEN: UI tests 38/38, `Samples.Tests` 45/45 |
| T7 P6 9-slice drawing and `PanelStyle` image; themed panel example | S4 S8 | inline | done | `feat/ui-skin`; RED: `NineSlice` missing; GREEN: `WidgetTests` 35/35, `Samples.Tests` 46/46 |
| T8 Menu example on the widget tree; any primitive `SelectionList` still lacks | S6 S8 | inline | done | `feat/ui-menu`; RED: `FocusChange` and `UiRoot.FocusChanged` missing; GREEN: `WidgetTests` 36/36, `Samples.Tests` 47/47 |
| T9 Deprecate `DialogueBox`, `Typewriter`, `SelectionList`, `Toasts`, `AchievementToasts`, `AchievementsScene`; migration notes | S7 S9 | inline | done | `feat/ui-deprecate`; RED: `DrawAboveScenes` and `TrackAchievements` missing; GREEN: `AchievementTests` 24/24; Risk: item 3 |
| T10 Demos, tutorial and template off the deprecated types (copies in the demos or the widget tree) | S7 | inline | done | `feat/ui-deprecate`; demo suites 60/71/68/126, `Samples.Tests` 48/48 |
| T11 Remove the deprecated types, in the SDK release after T9's | S9 | inline | blocked: needs T9 released | Risk: item 3 |
| T12 P7 selection in `TextField`, clipboard capability on SDL, raylib and browsers | S3 S8 | inline | done | `feat/ui-text-selection`; RED: selection members, `IClipboard` and `TextEntryExample` missing; GREEN: `WidgetTests` 38/38, `Samples.Tests` 49/49, clipboard conformance 3/3 (written after the code) |
| T13 P8 optional shaping and right-to-left module; check HarfBuzz on every target, WebAssembly included, first | S3 S8 | inline | done | `feat/text-shaping`; check: Windows, Linux (Docker), browser-wasm; RED: `ShapedFont` missing; GREEN: `ShapedFontTests` 5/5, architecture 22/22, `Samples.Tests` 50/50; Risk: item 5 |
| T14 Bidirectional lines (UAX #9, implicit levels): runs of both directions reordered inside one line | S1 S3 | inline | done | `feat/text-bidi` (stacked on `feat/text-shaping`); RED: `Bidi`, `BidiClass`, `IsRightToLeft` missing; GREEN: `ShapedFontTests` 8/8; Unicode `BidiTest.txt` 33346/33346 in scope (scratch, not committed) |

Routes are inline: each task is one behaviour with its example, tests and docs, and the tasks run in order. A writer
is delegated only if two of them run in parallel on disjoint files.

## Log

- L1 (2026-11-06) Issue #125, as filed: "Evaluate missing reusable 2D UI and text primitives ... Identify remaining
  general-purpose primitives that limit normal 2D game interfaces without expanding into a complete editor/framework
  unnecessarily ... There is a precise gap map, validated consumer use cases and recommendation on the minimum
  reusable SDK capabilities, leaving game-specific interface styles and content to the games."
- L2 (2026-11-06) A first evaluation deferred every gap because no demo needed it. The owner rejected that basis:
  "Recuerda que MicroEngine es un motor generalista para que cualquiera cree juegos, no solo nosotros ni nuestras
  demos. ... si un usuario externo tuviera que hacer un juego de ese estilo, ¿podría con lo que tiene hoy?"
- L3 (2026-11-06) The owner questioned whether widgets belong in an engine: "el chiste del motor es darte las
  primitivas para que tú montes cualquier tipo de sistema. Pero no volverse una biblioteca de assets." Compared with
  Godot and Unity, which ship generic controls with replaceable looks and no genre components.
- L4 (2026-11-06) Direction agreed: "necesitamos ver cuáles son las primitivas que permiten construirlos de alto nivel
  ... Dialogbox no debería ser ahí ... Selectionless habría que ver si falta alguna primitiva ... sin skinning ni
  componentes de género." The evaluation was rewritten as a decomposition (UI-1 §3), and ADR 0050 records the rule.
- L5 (2026-11-06) T1 done on `docs/ui-1`. Next: T2.
- L6 (2026-11-06) T2 done on `feat/ui-pointer` (stacked on `docs/ui-1`). `Widget` gains `OnPointerDown`, `OnPointerMove`,
  `OnPointerUp`, `OnPointerCancel` and `OnWheel`; `UiRoot` delivers them (the held press by pointer id at apply time,
  the wheel bubbling to containers, a cancel when the held widget leaves the tree or the content changes). The settings
  example found that spatial focus skips a narrow control under wide ones; the example aligns its column (one width),
  and the manual says so. Checks: `MicroEngine.Tests` 735 passed, 1 failed outside the change
  (`No_project_references_the_removed_entity_adapter` scans local `.claude/worktrees/` copies on this machine);
  `Samples.Tests` 38/38; snippets; `repo-docs` 17/17; tour capture of the settings screen inspected. Next: T3.
- L7 (2026-11-06) CI on #134 failed in `sdk/tests/docs.mjs`: the samples' build against the published SDK (0.5.0-alpha.1,
  nuget.org) was required, and `Examples` now uses the new pointer members. Fix: that build is a report while the
  release notes list `## Next release` changes (`pendingChanges`), as ADR 0048 already ties the documentation to the
  next release; it stays required with nothing pending. Every later slice that adds API would have hit the same rule.
  RED: `version.test.mjs` failed on the new cases; GREEN: 27/27; `node sdk/tests/docs.mjs` locally: Examples reported,
  all required checks passed.
- L8 (2026-11-07) T2 and the CI fix merged (#134). T3 done on `feat/ui-scroll`: `Widget.ClipsChildren` (drawing cut
  at the bounds, hit tests too), `Widget.InterceptsDrag` (a container takes a press from a child after 4 layout units),
  `UiContext.ClipTo` (clips that nest; `IRenderer.SetClip` has no getter, so they cannot nest otherwise), `ScrollView`
  (focus reveal once per focus change, wheel, drag, a scroll bar in its own gutter). `TextField` used `SetClip(null)`
  and dropped an outer clip: fixed and pinned by a pixel test. The tour showed the bar hidden over the buttons (gutter
  added) and the examples menu cut at 18 entries (line height 9). Checks: `MicroEngine.Tests` 740 passed, the same
  1 environmental failure as L6; the four game suites pass; `Samples.Tests` 40/40; snippets; `repo-docs` 17/17;
  `sdk/version.mjs check`. Next: T4.
- L9 (2026-11-07) T4 done on `feat/ui-grid`, stacked on `feat/ui-scroll` (#135) because both edit the UI manual, the
  release notes, the examples menu and the tour. `Grid` (uniform cells of the largest child, hidden children take
  none, `Align` within the cell, spacing 4 units like `Stack`). A nineteenth example no longer fitted the menu, which
  was a `SelectionList`: the menu is now buttons in a `ScrollView` (the first use of the widget tree outside the widget
  examples, part of T8), and the examples game sets `Presentation.UiScale = 3` so widgets read at the canvas's size.
  Checks: UI tests 32/32, `MicroEngine.Tests` 743 passed with the same environmental failure, `Samples.Tests` 42/42,
  snippets, `repo-docs` 17/17, `sdk/version.mjs check`; menu, settings and inventory captures inspected. Next: T5.
- L10 (2026-11-07) T5 done on `feat/ui-layer`. `Widget.Offset` (moves the subtree for layout and the pointer),
  `Widget.Opacity` (multiplies down the tree through `UiContext.Opacity`; the context's theme is faded, own colours go
  through `UiContext.Fade`), `Widget.BlocksPointer` (`UiRoot.Contains` now asks the hit test), `Layer` (edges or a point
  with a pivot; `Move` to follow). `ScrollView.Offset`/`MaxOffset` clashed with `Widget.Offset`: renamed `Scroll` and
  `MaxScroll` before any release. Found: per-draw fading lets a panel's border show through its fill (the theme panel
  measured 231 instead of 128); true group opacity needs an offscreen target, recorded as a limitation. Checks: UI
  tests 35/35, `MicroEngine.Tests` 746 passed with the environmental failure, `Samples.Tests` 44/44, TrailDuel and
  CourtyardKey suites, snippets, `repo-docs` 17/17, `sdk/version.mjs check`; the layer capture inspected. Next: T6.
- L11 (2026-11-07) User: "Sigue con T6". T6 done on `feat/ui-text`. `Label.Wrap` breaks the text to the label's fixed
  `Width` (no fixed width, no wrapping: a measure cannot know the arranged width); `VisibleCharacters` counts the text
  as written and keeps the whole text's lines and size, so a reveal never reflows; `Lines` and `VisibleLines` expose
  what is drawn. The dialogue example is now a `Layer` with a name tag and a box of labels plus two buttons, paging a
  long page into boxes of three lines with the font's `Wrap`; it no longer uses `DialogueBox`, `Typewriter` or
  `SelectionList`. The layer example's bubble wraps. Found while inspecting the capture: hiding the "more" arrow with
  `Visible` changed the box's height as it blinked; it uses `Opacity` now and the test pins the box size. Checks: UI
  tests 38/38, `MicroEngine.Tests` 749 passed with the environmental failure, `Samples.Tests` 45/45, snippets,
  `repo-docs` 17/17, `sdk/version.mjs check`; dialogue and layer captures inspected. Next: T7.
- L12 (2026-11-07) User: "Ya está fusionado, sigue con T7". T7 done on `feat/ui-skin`, from `main`. `NineSlice` (texture,
  source rectangle, four borders, `Scale`) and `RendererExtensions.DrawNineSlice` (corners kept, edges and centre
  stretched, corners shrink together when the destination is smaller). `PanelStyle.Image` and `ImageTint` are
  init properties, so the record's constructor is unchanged; with an image, `Panel` draws it (and `Shadow` in its
  shape), `UiContext.Fade` fades the tint, and a button tints it with the accent while focused, darker while pressed.
  New example `SkinExample` with `Content/frames.png`, an original 24x12 image (wood and parchment frames) drawn for
  it. Found in the capture: a patterned centre stretches into blocks; the image's centres are plain and the manual says
  to keep them plain. Checks: `WidgetTests` 35/35, solution suites pass except the known environmental test and
  `Pads_connect_are_recognised_press_and_hand_over_when_unplugged` (a physical pad is attached to this machine;
  `Losing_the_focus_releases_held_keys` failed once and passed when rerun); snippets, `repo-docs` 17/17,
  `sdk/version.mjs check`; the skin capture inspected. Next: T8.
- L13 (2026-11-07) User: "Listo, mergeado, continúa con el siguiente". T8 done on `feat/ui-menu`. S6 answered: every
  part of `SelectionList` maps to the widget tree (index: the focus; `Wrap`: `WrapFocus`; a disabled option: `Enabled`
  off, muted and skipped, where `SelectionList` let the cursor rest on it; the cursor: drawn by the item) except
  `Moved`, which had no equivalent short of overriding every item or polling `Focused`. Added
  `UiRoot.FocusChanged` with `FocusChange(From, To)`; the first focus has no `From`. The cursor following the mouse
  needs no primitive (`Hovered` plus `Focus`). New `MenuExample` with a `MenuItem` widget of its own. Checks:
  `MicroEngine.Tests` 753 passed (no environmental failure since T7), `Samples.Tests` 47/47, snippets, `repo-docs`
  17/17, `sdk/version.mjs check`; the menu capture inspected. Next: T9.
- L14 (2026-11-07) T9 and T10, one branch because the build treats warnings as errors: marking a type obsolete breaks
  every project that still uses it. User decision (asked): a new `TrackAchievements(catalog, store)` keeps tracking
  and saving without the engine's banners, and `UseAchievements` is deprecated with them, instead of a `banners`
  parameter that would need a second breaking change at removal. The removal showed one more missing primitive by S1:
  nothing let a game draw above every scene (the engine drew its banners itself), so `Game.DrawAboveScenes(canvas)`
  runs after the scenes and their transition. Deprecated with `[Obsolete]` messages that name the replacement:
  `DialogueBox`, `Typewriter`, `SelectionList`, `Toasts`, `Toast`, `AchievementToasts`, `AchievementsScene`,
  `Game.UseAchievements`, `Game.AchievementToasts`; engine tests of them disable CS0618 per file until T11. Demos keep
  copies in `games/Shared` (namespace `DemoKit`, linked by `games/Directory.Build.targets`): `OptionList`,
  `AchievementBanners`, `AchievementList`, `Typewriter`; Courtyard Key keeps its own `DialogueBox`. The tutorial's title
  and pause menus and `RebindExample` moved to the widget tree; new `AchievementsExample` (list in a `ScrollView`,
  banner per unlock). The template used none of them. Docs: UI and achievements manuals, tutorial step 10, migration
  notes (`## Next release`), release notes, references. Next: T12 (T11 waits for an SDK release with T9).
- L15 (2026-11-07) T12 done on `feat/ui-text-selection`. `IClipboard` (MicroEngine.Input) is a platform capability:
  SDL's clipboard on desktops and phones, raylib's (GLFW), `MemoryClipboard` headless (`HeadlessPlatform.Clipboard`),
  and in browsers `PageClipboard`, which reads the text of the page's latest paste or the game's latest copy and writes
  through `navigator.clipboard` (SDL's own clipboard on Emscripten stays inside the game). SDL's web key handler cancels
  every key pressed with Ctrl, which cancels the browser's paste, so the page holds Ctrl/Command+V back from SDL, lets
  the paste event deliver the text (`WebHost.Paste`), then replays the key to SDL. `UiRoot.Clipboard` (set from the
  capability for scene widgets) and modifier tracking in `UiRoot` (Shift, Control, Command; SDL and GLFW send key-ups
  on focus loss, so none sticks). `TextField`: anchor and caret, `Select`, `SelectAll`, `SelectionStart`,
  `SelectionLength`, `SelectedText`, `Copy`, `Cut`, `Paste` (one line, cut to `MaxLength`), Shift+arrows/Home/End,
  pointer press and drag, Ctrl/Command+A/C/X/V, highlight. New `TextEntryExample`. Verified in Chromium (built-in
  browser, dev package of the examples): typing, Shift+arrows, Ctrl+C reaching `navigator.clipboard.writeText`,
  Ctrl+V through the page's paste, the PASTE button, drag selection; key and paste events were synthetic, Firefox not
  run. Found, not changed: keys are applied before typed text of the same frame (text then Enter in one frame submits
  the old text). Next: T13.
- L16 (2026-11-07) User: "Listo, mergeo, dale con T13." Check first: HarfBuzzSharp 14.4.0 shaped Arabic to the same
  glyphs on Windows, Linux (Docker, with HarfBuzzSharp.NativeAssets.Linux) and in a browser package (its WebAssembly
  asset links the 3.1.56/st archive through its own buildTransitive targets, no host change); macOS, Android and iOS
  natives ship in the packages but were not run. FontStashSharp has no public glyph-index rendering, so the module
  rasterizes with StbTrueTypeSharp into its own pages. HarfBuzz does no bidi.
- L17 (2026-11-07) User chose option 1: "1" (the shaping module is a published SDK package, not unpublished like
  Box2D). T13 on `feat/text-shaping`: `MicroEngine.Text.Shaping` (seventh package; `sdk/version.mjs` packageIds,
  `sdk/tests/packages.mjs`, counts in docs and `sdk-release.yml`), `ShapedFont` / `ShapedFontSize` (`IFont`) /
  `ShapedGlyph`: HarfBuzz per line with the guessed direction, missing runs reshaped with the next fallback face,
  fallbacks sized by the main face's em (a first capture showed Noto Arabic far smaller when each face used its own
  pixel height), glyph pages of its own, shaped-line cache. Licences: `THIRD-PARTY-NOTICES.md`, the tool's
  `Notices/HarfBuzz-LICENSE.txt` copied into desktop (by file), web (by `HarfBuzzSharp.*`) and phone packages (the
  package's build props set `MicroEngineTextShaping`). Examples reference the package (samples swap it for the
  project like MicroEngine.Desktop; `sdk/tests/docs.mjs` moves every SDK package version) and ship Inter and Noto Sans
  Arabic/Hebrew (OFL, `samples/Examples/CREDITS.md`). New `ShapingExample` (tour capture 24). Verified in Chromium from a
  web package: joined Arabic, Hebrew, wrapping label, HarfBuzz licence in `licenses/`; a 4:3 window showed the example
  drawing above the game frame, fixed by placing it from `Views.CanvasToUi`. Mixed-direction lines are T14.
- L18 (2026-11-07) User: "Yes, T14" (#143 still open: `feat/text-bidi` stacked on it). `Bidi`: UAX #9 for one line of
  one paragraph without explicit embeddings (P2-P3, X9 by dropping BN and the explicit controls, W1-W7, N1-N2, I1-I2,
  L1-L2 over runs); no N0 bracket pairs. Classes from Unicode 16.0 DerivedBidiClass.txt (@missing defaults, then the
  entries) as 1239 ranges in `Bidi/BidiClasses.g.cs`, written by `Bidi/generate.mjs`; Unicode License v3 in
  `THIRD-PARTY-NOTICES.md`. The shaper shapes each level run with its direction set before guessing the script.
  `ShapedFontSize.IsRightToLeft`. Checked against Unicode's BidiTest.txt: every case with automatic paragraph direction
  and no explicit formatting class, 33346 of 33346 levels equal (a scratch test reading the downloaded file, deleted).
  BidiCharacterTest.txt is almost all bracket cases: 1 in scope, passed. Example: two mixed lines, each line aligned
  by `IsRightToLeft`, the label moved to the bottom (`Layer`); capture 24 looked at.
