# ADR 0050: The engine's UI offers primitives; genre components become examples

## Status

Accepted (2026-11-06). Amends [ADR 0044](0044-ui-module.md) (which widgets the UI module holds and what stays in the
core) and [ADR 0006](0006-achievements.md) (the engine no longer shows achievement banners by itself once the
deprecated views are removed). Evidence: [UI-1](../evaluations/ui-1.md). Task record:
[`odd/tasks/ui-1.md`](../../odd/tasks/ui-1.md).

## Context

MicroEngine is a general-purpose engine: the test of its UI is whether a developer outside the project can build any
2D game's interface with it (a side or floating menu, an RPG menu, a settings screen, an inventory, a dialogue box, a
speech bubble, a banner), not whether the four demos can.

Today the UI mixes two kinds of code. The widget tree (`Widget`, `UiRoot`, `Label`, `Button`, `Panel`, `Stack`,
`TextField`) and the touch controls are mechanism: input routing, focus, capture, the text channel, fingers. Beside them
sit components with a genre or a design of their own: `DialogueBox` and `Typewriter` (an RPG dialogue box),
`SelectionList` (a cursor menu that ignores the pointer), `Toasts`, `AchievementToasts` and `AchievementsScene` (banners
and an achievement screen the engine shows by itself). They cannot be rebuilt from the tree, because the tree lacks
primitives: widgets never see pointer positions or the wheel, nothing clips or scrolls, there is no grid and no
positioned placement, `Label` neither wraps nor reveals letters, and looks are colours only (UI-1 §2–3).

General-purpose engines draw the line the same way: Godot's `Control` nodes and Unity's UGUI ship generic controls, a
layout model and replaceable looks (themes, style boxes, 9-slice images), and no dialogue box, speech bubble or
achievement screen.

## Decision

- **The rule.** The engine owns primitives: behaviour that is hard to get right or needs the engine's internals (input
  routing, pointer capture, focus, clipping, layout, text layout, the text channel, the platform), with a neutral
  default look a game can replace. Every higher-level component must be buildable from those primitives by a game,
  without engine changes. The engine ships no genre components and no designed looks; such components are examples
  in `samples/` that a game copies and restyles.
- **Primitives added** (UI-1 §4):
  - **P1:** pointer events on widgets (press, move and release with positions, capture until release, the wheel).
  - **P2:** clipping of a container's children, and a scrolling container that keeps the focused widget in view.
  - **P3:** a grid layout.
  - **P4:** positioned placement (children at points or anchors), and an offset and opacity per widget.
  - **P5:** `Label` wrapping to its width, and a visible character count.
  - **P6:** 9-slice drawing in the renderer, and an image option in `PanelStyle`.
  - **P7:** text selection in `TextField`, and a clipboard platform capability.
  - **P8:** shaping and right-to-left text as an optional module, because it needs a native library on every target.
- **What stays:** `Widget`, `UiRoot`, `Label`, `Button`, `Panel`, `Stack`, `TextField`, `UiTheme`, and the touch
  controls, whose looks (`TouchStyle`) are already separate from their behaviour.
- **What leaves:** `DialogueBox`, `Typewriter`, `SelectionList`, `Toasts`, `AchievementToasts` and `AchievementsScene`.
  Each becomes an example built on the primitives. Demos that use one keep a copy in their own code or move to the
  widget tree. The achievements module keeps tracking, persistence and its `Unlocked` event.
- **Sliders, toggles and value selectors are examples**, built on P1 to prove it, not engine types.
- **Order that keeps every release working:** add a primitive together with the example that needs it; once the
  examples replace a component, mark it `[Obsolete]` in one SDK release, with migration notes; remove it in a later
  release. P7 and P8 do not block the rest.

## Alternatives considered

- **Keep the components and add more controls** (sliders, toggles, scrolling lists as engine types), as Godot and Unity
  do for generic controls: it grows the API with designs the engine must then keep stable, and it still leaves the
  primitives missing for whatever the kit does not cover.
- **Move the whole UI into a separate optional package**, as raygui sits beside raylib: possible later, but it does not
  answer which types belong in it; this decision is needed first.
- **Remove the components at once:** it breaks every game that uses them in one release, the four demos included,
  with no replacement to migrate to.

## Consequences

- External games can build any of the interfaces above from documented primitives, each shown by an example.
- The public API grows by the primitives and later shrinks by six types. Their removal is a breaking change, announced
  one release ahead.
- Once the views are removed, a game that wants banners on unlock subscribes to `Unlocked` and shows them itself; the
  example shows how.
- The UI module is the engine's mechanism only, which makes it a candidate to become an optional package later.
