# GAPS-UI: menus and dialogue in the SDK

Objective: a developer outside the repository builds a pixel-art menu and a dialogue box from the SDK, with their own
look, without the 335 lines the demos keep in `games/Shared`. Group D of issue NastMz/MicroEngine#218. Branch
`feat/gaps-ui`. Delivery: one pull request (about 2,900 authored lines without the generated API pages; `ask-on-risk`, the owner decides the slices).
Test runner: `dotnet test tests/MicroEngine.Tests -c Release --filter FullyQualifiedName~Interface`,
`dotnet test tests/Samples.Tests -c Release`. Evaluation: [`docs/evaluations/gaps-ui.md`](../../docs/evaluations/gaps-ui.md).
Decision: [ADR 0077](../../docs/decisions/0077-interface-on-the-canvas-skins-reveal-and-cursor.md).

## Specs

- S1 "The four demos share `games/Shared` (`DemoKit.OptionList`, a typewriter reveal, a list cursor): that is engine
  UI kept outside the SDK; the engine's own dialogue example hand-writes its reveal." Decide with the method what
  belongs in the SDK and in which form. `games/Shared/**` and `games/**` are not edited in this round.
- S2 "Widgets draw only in the UI view: a game on a 320x180 canvas cannot use them for its menus." Widgets usable on
  the canvas: laid out and drawn in canvas pixels, input mapped into canvas coordinates (mouse and touch), as a
  choice per root or per scene, on whole canvas pixels.
- S3 "A menu row drawn as a bar, an underline and a bobbing cursor needs a control of its own (`MenuItem` draws the
  theme's panel or a nine-slice)." A game styles a control's drawing without writing a new control.
- S4 A typewriter reveal as engine text behaviour: characters per second on the scene's clock, complete on confirm,
  counted by grapheme cluster on the shaped and localized text (right-to-left, combining marks), a hook per revealed
  character, no allocation per frame.
- S5 Proof without editing the demos: for each of the four files of `games/Shared`, a test or an example that
  produces through the SDK what the helper produces ("the same items selected by the same inputs on the same
  updates; the same number of characters revealed on each update for the typewriter, using the demos' rates"), and
  one example in `samples/Examples` with a pixel-art menu and a dialogue box on a low-resolution canvas, driven by
  keyboard, gamepad, mouse and touch, its capture looked at at two window scales.
- S6 Where the SDK version cannot be pixel-identical to a demo's hand-drawn menu, say what differs and whether a
  skin can express it.

## Tasks

| Task | Specs | Route | Status | Commit / evidence |
| --- | --- | --- | --- | --- |
| T1 Widgets on the canvas (`UiSpace`, `UiRoot.Space`, whole pixels, pointer, `PointerOnUi`) and control skins (`ControlSkin`, `ControlState`, `Button.Skin`, `Button.Padding`, `Panel.Skin`, theme skins, `FocusTime`) | S2 S3 | inline | done | `eb4cd6ef`; RED 6 of 9 and 7 of 7, then GREEN 68/68 UI tests |
| T2 `TextReveal` and `Label.Reveal`; a label's visible lines cached | S1 S4 S5 | inline | done | `bc53c60d`, `2e916e63`; RED 13 of 13, GREEN 82/82 |
| T3 `MenuCursor` | S1 S5 | inline | done | `0ef88cb5`; RED 8 of 8, GREEN 90/90 |
| T4 `PixelMenuExample`, its script and test; the dialogue example on `Label.Reveal`; the title menu's lines as skinned buttons | S5 | inline | done | `e2fe7d03`; `Samples.Tests` 119 passed, 5 skipped |
| T5 Manual, release notes, limits, API pages, evaluation, ADR 0077, this record | S1 S6 | inline | done | `7d3583c9`, `7f45c7ba` |

Routes are inline: one writer, one area.

## Log

- L1 (2026-11-24) Issue #218, group D, as filed: "### D. Menus and dialogue in the SDK (GAPS-UI) - The four demos
  share `games/Shared` (`DemoKit.OptionList`, a typewriter reveal, a list cursor): that is engine UI kept outside
  the SDK; the engine's own dialogue example hand-writes its reveal. - Widgets draw only in the UI view: a game on a
  320x180 canvas cannot use them for its menus. - A menu row drawn as a bar, an underline and a bobbing cursor needs
  a control of its own (`MenuItem` draws the theme's panel or a nine-slice)."
- L2 Read: the four files of `games/Shared`, their call sites (about 30), Courtyard Key's `DialogueBox`, Trail Duel's
  `TextPanel` and `GridMove`, the four records of `odd/tasks/demo-modern-1`, ADR 0050 and `odd/tasks/ui-1.md`.
  Found: `MenuItem` is the example's widget, not an engine type; `Button` is what draws the panel. ADR 0050 removed
  the typewriter and the selection list three weeks ago as designed components; what returns here holds no look.
- L3 The scene's files (`src/MicroEngine/Scenes/**`) belong to another writer in this round. A root on the canvas is
  therefore wired from `Game.CreateUi` and `UiRoot` alone: the size and the pointer through hooks the engine sets,
  the engine's interface pass skipped, and the draw called by the scene from `Draw`. Follow-up, not done: three
  lines in `SceneManager.Draw` would draw a canvas root after its scene's `Draw` without the call.
- L4 `CaptureScreen` of a frame without a UI view is the canvas, and `--captures` writes the canvas three times as
  large whatever `--scale` is. The proof of whole pixels at two window scales is therefore: the layout is whole
  canvas pixels (tests), nothing is drawn at the window's resolution (tests), and the seven captures of
  `pixel-menu.script` at `--scale 2` and `--scale 3` (SDL, hidden) are byte-identical and were looked at. The window
  itself, as a platform composes it, was not captured.
- L5 The script's `touch` and `tap` commands go to the on-screen touch controls, not to widgets: a finger on a
  widget is sent by the tests (`PointerKind.Touch`), not by the script.
- L6 Not done, and why: the site's tests (`site/tests`) were not run, the worktree has no `node_modules`; the
  right-to-left reveal was tested with the built-in font, not with a shaped font; no desktop-window test was added.
- L7 For the drawing writer: `TextReveal.Visible` is a `ReadOnlySpan<char>`. `Label.DrawContent` and
  `TextReveal.VisibleLines` make one string per newly revealed character because fonts draw `string`; with span
  entry points on `IFont` the label can draw `line.AsSpan(0, shown)` and a box drawn by hand `reveal.Visible`, and
  that last allocation goes. `ControlState.Text` and `Button` measure with `string` and need no change.
- L8 (2026-11-27) Correction batch after an independent verification, which confirmed the canvas mapping, the skins,
  the reveal against the compiled `Typewriter.cs` and the cursor against `OptionList.cs`, and found six defects. All
  fixed, each with a test seen failing first (`tests/MicroEngine.Tests/UI/InterfaceCorrectionTests.cs`, 10 of 12 red
  before the code). D1: a canvas root the scene did not draw was invisible and took input; the scene manager now
  draws it after the scene's `Draw` (two lines in `SceneManager.Draw`, which supersedes L3's follow-up), and a scene
  that draws it itself is left alone. D2: a rate or a step that is not a finite number above zero is ignored by
  `TextReveal`, and `Revealed` stays in 0..`Count`. D3: `Label.VisibleLines` reuses its list; what remains is one
  string per newly revealed character (the line in progress), because fonts draw `string` (L7). D4:
  `MenuCursor.Top` leaves no blank rows after the list shrinks. D5: `PixelSnap` rounds both edges, so sizes are
  whole. D6: `FocusTime` starts again when the content is shown again. The manual now states the cursor's and the
  focus's rules for opposite directions, disabled rows and key repeat (none: no demo repeats and the map has no
  repeat to read).
