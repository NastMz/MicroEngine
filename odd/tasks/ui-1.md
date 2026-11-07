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
| T3 P2 clipping and a scrolling container; long list example | S3 S8 | inline | pending | — |
| T4 P3 grid layout; inventory example | S3 S8 | inline | pending | — |
| T5 P4 positioned placement, per-widget offset and opacity; speech bubble and banner examples | S3 S8 | inline | pending | — |
| T6 P5 `Label` wrapping and visible characters; dialogue box example | S3 S5 S8 | inline | pending | — |
| T7 P6 9-slice drawing and `PanelStyle` image; themed panel example | S4 S8 | inline | pending | — |
| T8 Menu example on the widget tree; any primitive `SelectionList` still lacks | S6 S8 | inline | pending | — |
| T9 Deprecate `DialogueBox`, `Typewriter`, `SelectionList`, `Toasts`, `AchievementToasts`, `AchievementsScene`; migration notes | S7 S9 | inline | pending | Risk: item 3 |
| T10 Demos, tutorial and template off the deprecated types (copies in the demos or the widget tree) | S7 | inline | pending | — |
| T11 Remove the deprecated types, in the SDK release after T9's | S9 | inline | blocked: needs T9 released | Risk: item 3 |
| T12 P7 selection in `TextField`, clipboard capability on SDL, raylib and browsers | S3 S8 | inline | pending | — |
| T13 P8 optional shaping and right-to-left module; check HarfBuzz on every target, WebAssembly included, first | S3 S8 | inline | pending | Risk: item 5 |

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
