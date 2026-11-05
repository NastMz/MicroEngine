# ADR 0044: A UI module of widgets driven as the highest input layer; captures show what the player sees

## Status

Accepted (2026-11-04). Builds on [ADR 0032](0032-input-devices-players-layers.md) (input layers with consumption, the
text channel, renamable engine action names), the views hook of the world view and the UI view (`Scene.DrawUi`,
`Scene.UiContains`, `Game.Views`, `IScreenView`, `Presentation.UiScale`) and [ADR 0006](0006-achievements.md) (the
achievement banners). Numbered after the ADRs open on other branches when it was written (0041-0043). Evidence: ARCH-1
§3 (S19), §6.3 and §6.5 (the UI row), §9 (stage 4), annex A3 (UI-1, UI-2, IN-4).

## Context

The engine's widgets (`Typewriter`, `DialogueBox`, `SelectionList`, `UiTheme`, the achievement banners) lived in the
core assembly and were immediate-mode helpers: games placed them by hand in canvas pixels, they answered only to the
keyboard and gamepad through actions, and nothing kept a pointer press on a menu from also reaching gameplay. The views
hook gave the interface its own view at the window's resolution, but no widget used it, and a run's captures recorded
the canvas alone, so a capture of a frame with an interface did not show what the player saw.

## Decision

- **A `MicroEngine.UI` assembly** inside the `MicroEngine` package, referencing only the 2D layer, text, input, the
  graphics contract, content, the kernel and the platform contracts (the architecture test checks it). It holds the
  widgets that need nothing else: `Typewriter`, `SelectionList` and `UiTheme` move there with their namespace
  (`MicroEngine.dll` forwards them), and it adds a retained widget tree: `Widget` (measure, arrange, draw; focusable or
  not; enabled and visible), `Label`, `Button`, `Panel`, `Stack` (vertical or horizontal, spacing, cross-axis
  alignment), `TextField`, `Toasts` (generic banners) and `UiRoot`, which lays a tree out in UI units at an anchor of
  the UI view, keeps the focus and draws the tree.
- **The façade hosts the UI in scenes.** Scenes draw, and the scene stack stays in the core assembly for this phase, so
  a scene owns one root, `Scene.Widgets`, created when first read: the engine updates it before the scene's `Update`,
  draws it after `DrawUi`, counts it in `UiContains` (so the pointer over a widget is on the UI and not in the world),
  adds its focus and its text fields' text to the scene's state hash, and releases the text channel when the scene
  leaves or is covered. A scene that never reads it has no UI, no layer work and no hash change.
- **UI is the highest input layer.** The first root adds one layer at `InputLayerOrder.UI` that gives events to the top
  scene's root: it takes pointer presses that land on the content (and, through the layer capture, everything of those
  presses), the wheel over it, and every key while a text field has the focus; text and composition events go on to the
  text channel. Pointer moves are watched for the hover but left to gameplay. Taken events are applied in the root's
  update, inside the fixed step, so replays and state hashes stay deterministic. Actions are read from the input map
  and consumed when a widget acts on them, so the scene's update after the root never sees them.
- **Engine widget actions are renamable defaults.** A root reads up, down, left, right, confirm and cancel under the
  map's current names (`InputMap.Names`) unless the game sets other actions (`UiActions`): renaming `confirm` keeps the
  widgets working, and a gameplay action that happens to be called `confirm` never presses a button.
- **Focus navigation is spatial**: the direction actions move the focus to the nearest focusable widget in that
  direction by the last layout, wrapping to the far side by default; the first focusable widget takes the focus when
  nothing has it, so a keyboard or a gamepad works at once; a click focuses what it presses.
- **The text field uses the text channel**: focus turns text input on with the field's caret rectangle (the on-screen
  keyboard, the input method's candidates), typed text is inserted at the caret up to a maximum length, the input
  method's composition is shown underlined until committed, and editing keys never reach gameplay.
- **Captures show what the player sees.** The UI view begins at the first thing a scene draws in it, so a frame without
  an interface has none (and a playing transition is drawn over the UI view only when it has one; the canvas shows it
  over the world either way). A platform with a UI view offers `IViewCapture`: a frame that drew a UI is captured as the
  window, the world scaled into its frame with the presentation's filter, the letterbox around it and the UI composited
  over everything (`ScreenCapture.Compose`), at the window's size; a frame without one is captured as the canvas, as
  before. `CaptureWorld` and `SaveWorldScreenshot` keep the world-only capture for tests. The headless platform
  rasterizes its UI view when it rasterizes.
- **What stays in the core**: `DialogueBox` and `PromptText` read `ActionPrompts`, which knows the on-screen touch
  controls, and `AchievementsScene` and `AchievementToasts` read the achievement tracker; the UI may reference neither.
  They use the UI's types (`AchievementToasts` shows its banners through `Toasts`) and move when their dependencies
  allow it.

## Consequences

- Games and samples compile unchanged; the demos draw no interface in the UI view, so their captures and traces do not
  change. A game whose own type is called `Ui` keeps it: the scene's root is `Widgets`, not `Ui`.
- Widgets measure in layout units derived from the font (1 for the built-in 10-pixel font), so a larger font grows the
  margins with the text; the UI scale stays the presentation's.
- A capture of a frame with an interface has the window's size, not the canvas's times the capture scale, and its world
  is resampled into the window as the player saw it.
- Not decided here: the default UI scale (the presentation's value, 1, until it follows the surface's pixel density),
  a dialogue box that takes prompts from the UI assembly, scrolling containers, rich text and right-to-left text.
