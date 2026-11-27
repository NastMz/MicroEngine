# 0077 — The interface on the canvas, skins, a text reveal and a list cursor

Status: Accepted (2026-11-26). The SDK is in Alpha: new public types and members carry no compatibility commitment.
Evidence: [GAPS-UI](../evaluations/gaps-ui.md). Issue NastMz/MicroEngine#218, group D. Record:
[`odd/tasks/gaps-ui.md`](../../odd/tasks/gaps-ui.md). Amends [ADR 0050](0050-ui-primitives-not-components.md): its
rule stands, and two behaviours it removed together with their looks return without one.

## Context

The four demos were moved onto the engine's capabilities and kept their menus and dialogue on 335 lines of shared
code outside the SDK, for three reasons their records measured: widgets live only in the UI view, at the window's
resolution, so a 320x180 pixel-art game cannot use them for its menus; a button draws the theme's panel or a
nine-slice, so a row with a look of its own needs a new widget type; and the reveal of a text and the cursor of a
list a scene draws are written again by every game, the engine's own dialogue example included. The reveal over
`Label.VisibleCharacters` counts `char`s, which cuts a letter from its marks.

ADR 0050 removed `Typewriter`, `SelectionList`, `DialogueBox` and the banner types as designed components. Each
mixed a behaviour with a look; the looks were the reason to remove them.

## Decision

- **A root lives in a space.** `UiRoot.Space` is `UiSpace.UiView` (the default) or `UiSpace.Canvas`. On the canvas a
  scene's widgets are laid out in canvas pixels in the canvas's size, placed on whole pixels
  (`UiContext.PixelSnap`), given the pointer in canvas pixels, counted by `GameViews.PointerOnUi`, and drawn in the
  canvas under a camera of their own: by the engine right after the scene's `Draw`, inside the scene's
  post-processing, unless the scene drew them itself in that frame (`Widgets.Draw(renderer)` from `Draw`, to put
  them under something). What takes input is therefore always visible. The engine's pass over the UI view draws
  nothing for them. Update, input routing and the state hash are the same in both spaces.
- **A control's look is a function.** `ControlSkin(renderer, in context, in state)` draws a button or a panel from a
  `ControlState` (bounds, text, enabled, focused, hovered, pressed, the time with the focus). It is set on a control
  (`Button.Skin`, `Panel.Skin`) or on a theme (`UiTheme.ButtonSkin`, `UiTheme.PanelSkin`). `Button.Padding` sets the
  room around the label, so a skin without a box decides the size. The engine ships no skin.
- **A text reveals itself by whole characters.** `TextReveal` holds a text, a rate, the count shown, a call per
  revealed character, an optional pause after one, `Finish`, lines for a box drawn by hand, and a state hash. It
  counts extended grapheme clusters with the base library and allocates nothing while it runs. `Label.Reveal` reveals
  a label's text with one and starts again when the text changes. Its arithmetic is the demos' typewriter's (a float
  count that grows by rate times step), so a game moving onto it shows the same characters on the same updates.
- **A self-drawn list has a cursor.** `MenuCursor` holds a count, columns, the index, wrap, options that cannot be
  chosen (the cursor rests on them), `Moved`, `Update(input)` returning the chosen option with the press consumed,
  `MoveTo` for a pointer the scene maps, `Top(visible)` for a scrolling window, and a state hash. No labels, no
  drawing.
- **Designed components stay examples.** No dialogue box, achievements list or banner type. The examples build them;
  the pixel menu example builds a menu and a dialogue on the canvas.

## Alternatives considered

- **Only the scene draws canvas widgets.** The first version: a scene that forgot the call had widgets that took
  input and showed nothing. Rejected after verification; the scene manager now calls the root after each scene's
  draw, and the explicit call remains for placing the interface under an effect.
- **A second root per scene, one per space.** More to route and to hash, for a case (a canvas menu and a sharp
  overlay together) that `DrawUi` or a pushed scene covers.
- **Skins as theme parts only, or as nine-slices only.** A nine-slice cannot draw an underline or a cursor that
  moves; a theme-only skin cannot give one button another look.
- **A skin type with a `Measure`.** A delegate is what a game writes in one method; the one size a row needs is its
  padding.
- **Bring `SelectionList` and `Typewriter` back as they were.** They carried a default drawing and labels; the cursor
  ignored the pointer. The new types hold neither.
- **A UI library (Dear ImGui, Myra).** Their own renderer, fonts, input and look; none of the gaps is one they close.

## Consequences

- A pixel-art game builds its interface from the engine's widgets with its own look, with the mouse and touch for
  free; the demos' second pass can delete `games/Shared/OptionList.cs` and `Typewriter.cs` for `MenuCursor` and
  `TextReveal` with the same values on every update, and move menus to skinned widgets where it wants the pointer.
- The public API grows by five types and about a dozen members. `UiRoot.Contains` now takes a point of the UI view
  for a root the engine made on the canvas; `UiRoot.WidgetAt` keeps the root's units.
- A scene's state hash includes a revealing label's reveal.
