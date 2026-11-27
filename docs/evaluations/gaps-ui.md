# GAPS-UI: menus and dialogue in the SDK

Group D of issue NastMz/MicroEngine#218 (DEMO-GAPS-1). Decision: [ADR 0077](../decisions/0077-interface-on-the-canvas-skins-reveal-and-cursor.md).
Record: [`odd/tasks/gaps-ui.md`](../../odd/tasks/gaps-ui.md). Level: **Extend** the UI module.

## A. Need

Every game with a menu or a text box. The four demos keep 335 lines of interface outside the SDK in `games/Shared`
(`OptionList` 97, `Typewriter` 41, `AchievementList` 112, `AchievementBanners` 85), used from about 30 call sites, plus a
dialogue box each in Courtyard Key (`UI/DialogueBox.cs`) and Trail Duel (`Ui/TextPanel.cs`). The engine's own dialogue
example wrote its reveal loop by hand. The demos' records say why they did not move to the widgets:

- Meadow Run, gap 11; Courtyard Key: widgets are laid out and drawn only in the UI view, at the window's resolution.
  A game whose picture is a 320x180 canvas would get menus with a different pixel size over its picture.
- Neon Rift, gap 6: a row drawn as a tinted bar, an underline and a bobbing cursor is not a `Button` (the theme's
  panel or a nine-slice), so it needs a widget type of the game's own. (`MenuItem`, which the records name, is a
  widget of the engine's example, not an engine type: checked in `samples/Examples/Examples/MenuExample.cs`.)
- Trail Duel, G4: the reveal (letters per second, a call per letter, finish on confirm) and the cursor of a screen
  that draws itself (wrap, a sound per move) are written by every game; its battle menus are two-by-two grids the
  shared list does not cover (`BattleScene.GridMove`).

What the call sites need from the helpers, read in the four games: an index the scene draws around and hashes; wrap;
options that cannot be chosen but that the cursor rests on (Meadow Run plays a thud on confirm there; three games
disable an option); a move sound; the chosen index on confirm, the press consumed; action names of the map; lists
that change length (Trail Duel's bag); a reveal at 45 and 60 characters a second, restartable with characters kept
(text laid out again mid-reveal), a call per non-space character, finish without calls; a list cursor that stops at
the ends with a window that follows it; banners one at a time for 3.4 s with a 0.25 s slide.

## B. Current experience

A developer outside the repository making a pixel-art game: either draws the interface at the window's resolution
with widgets (the wrong pixel size), or writes the cursor, the reveal and the rows by hand as the demos do (about
140 lines before any look). A reveal over `Label.VisibleCharacters` counts `char`s: it cuts a letter from its
combining marks and a surrogate pair in two, and `Label.VisibleLines` allocated an array and a string per line on
every draw while a count was set.

## C. Alternatives

- **A UI library.** Dear ImGui through ImGui.NET (MIT, a native library per platform) and Myra (MIT, back ends for
  MonoGame, FNA and Stride) are toolkits with their own renderers, fonts and input; neither draws through `IRenderer`
  nor reads the engine's actions, and each brings a look that is not pixel art. They were judged from what they are,
  not installed or measured, and their current versions were not checked: none can close a gap of this group, because
  the missing pieces are inside the engine's own tree (which view a root lives in, who draws a button, how a label
  counts characters). Rejected.
- **Grapheme segmentation.** .NET's `System.Globalization.StringInfo.GetNextTextElementLength` implements extended
  grapheme clusters (UAX #29) without allocating, in the base library on every target. Used; no dependency added.
- **The engine's own.** Widgets, focus, the pointer layer, `Layer`, `Label.Wrap` exist (ADR 0044, 0050). Everything
  below is added to them.
- **Bring the removed components back** (`SelectionList`, `Typewriter`, `DialogueBox`, `Toasts`, removed by ADR 0050
  three weeks ago as designed components). Rejected as they were: each carried a look. What returns is their
  behaviour, without one.

## D. Target experience

Written before the API (now `samples/Examples/Examples/PixelMenuExample.cs`):

```csharp
Widgets.Space = UiSpace.Canvas;
Widgets.Theme = theme with { ButtonSkin = Row, PanelSkin = Box };
Widgets.Content = new Panel(new Stack().Add(new Button("TALK", Talk)).Add(new Button("LEAVE", Leave)));
// Draw
Widgets.Draw(renderer);

static void Row(IRenderer r, in UiContext ui, in ControlState s) { /* bar, underline, cursor from s.FocusTime */ }

var text = new Label { Width = 236, Wrap = true, Reveal = new TextReveal { CharactersPerSecond = 45 } };
text.Reveal.CharacterRevealed += _ => Audio.TryPlay("blip");
if (!text.Reveal.Complete) text.Reveal.Finish(); else text.Text = NextPage();
```

and for a screen that draws its rows: `if (_menu.Update(Input) is { } chosen) ...` with `_menu.Index` in `Draw`.

## E. Responsibility limit

The engine owns behaviour that needs its internals or is easy to get wrong: the view a root lives in and its pointer
mapping, whole-pixel placement, the state a control is drawn from, counting characters, the state hash. The game
owns every look and every component: rows, boxes, name tags, the dialogue's paging, banners and their timing, the
achievements screen. ADR 0050's rule stands; this unit moves its line for two behaviours (the reveal, the list
cursor) that were removed together with their looks.

## F. Integration

- **Scenes.** `Scene.Widgets` is the root in either space; the engine still updates it before `Scene.Update`, routes
  input to it and hashes it. On the canvas the scene manager draws it after the scene's `Draw`, inside
  the scene's post-processing; a scene that calls `Widgets.Draw` from its `Draw` places it itself and it is not drawn
  again.
- **Input.** The pointer arrives in canvas pixels; a root on the canvas takes it as it is. `GameViews.PointerOnUi`
  is true over canvas widgets (`UiRoot.Contains` maps the UI point). Actions are read under the map's names.
- **State hash and determinism.** A label's reveal and a `MenuCursor` are `IStateHashed`; skins and `FocusTime` only
  draw. Nothing reads the machine.
- **Localization.** A label restarts its reveal when the language changes its text; right-to-left lines grow from
  the right edge, as labels already draw them.
- **Post-processing and captures.** Canvas widgets are part of the canvas: inside the scene's chain and in canvas
  captures; the frame has no UI view.

## G. Trade-offs

- Two ways to make a menu (widgets, `MenuCursor`). They are two kinds of screen: one laid out by the engine with the
  pointer built in, one drawn by hand around an index. The manual says when to pick each.
- Canvas widgets are drawn after the scene's `Draw` unless the scene drew them itself: two paths, so that a root
  that takes input is never invisible.
- `UiRoot.Contains` takes a point of the UI view; `WidgetAt` takes the root's units. They only differ on the canvas.
- A skin is a delegate, not a type: it cannot change a control's size. `Button.Padding` covers the size.

## H. Decision

**Extend.** `UiSpace` and `UiRoot.Space`; `ControlSkin`, `ControlState`, `Button.Skin`, `Button.Padding`,
`Panel.Skin`, `UiTheme.ButtonSkin`, `UiTheme.PanelSkin`, `FocusTime`; `TextReveal` and `Label.Reveal`; `MenuCursor`.

Declined, with reasons:

- **An achievements list and banners as engine types.** They are designs (which rows, which timing, one banner or a
  stack), and what they are made of is in the SDK: `ScrollView`, `Layer`, `Offset` and `Opacity`, tweens, and now a
  `MenuCursor` with `Wrap` off and `Top(visible)` for a list drawn by hand. `AchievementsExample` builds both. The
  tracker stays in `MicroEngine.Achievements`, without views (ADR 0050).
- **A dialogue box type.** Paging rules differ in the two demos that have one (three lines and prompts; two lines,
  auto-advance, a last page that stays). The reveal was the part both copied.
- **An option to let the focus rest on a disabled widget.** With a skin, a game keeps the button enabled, draws it
  muted from its own flag and answers the click; a `MenuCursor` rests on such options by rule.
- **Key repeat in `MenuCursor`.** No demo repeats; the map has no repeat to read. A limit, stated.

## I. Validation

- `tests/MicroEngine.Tests/UI/CanvasWidgetTests.cs` (9), `ControlSkinTests.cs` (7), `TextRevealTests.cs` (14),
  `MenuCursorTests.cs` (8); `tests/Samples.Tests/PixelMenuExampleTests.cs` (3).
- Against the demos' helpers, with the helper's rule written in the test and the same presses on the same updates:

| Helper | Proof | Result |
| --- | --- | --- |
| `Typewriter` | `TextReveal` at 45 and 60 characters a second, 240 updates of 1/60 s: the count on every update, the visible text, the calls | identical |
| `OptionList` | `MenuCursor`, five options, one that cannot be chosen, 600 updates of scripted presses, wrapping and not: the index, the chosen option and the move sounds on every update; confirm consumed when it chooses and left alone when it cannot | identical |
| `AchievementList` | `MenuCursor` with `Wrap` off and `Top(4)`, eleven rows, 900 updates: the highlighted row and the first row shown on every update. Its picture: `AchievementsExample` | cursor and window identical; the picture is the example's |
| `AchievementBanners` | `AchievementsExample`: one banner per unlock, queued, one at a time | same behaviour; timings are the example's (0.3 s in, 2 s, 0.3 s out against 3.4 s with 0.25 s slides), not compared step by step |
| Trail Duel's `GridMove` | `MenuCursor(4, columns: 2)`, 600 updates | identical, except updates with two opposite directions pressed together: the cursor cancels them, the demo lets left win |

- What a demo's hand-drawn menu needs to be pixel-identical on widgets: the rows' positions are the layout's
  (`Stack.Spacing`, `Button.Padding`, a fixed `Width`), whole pixels on the canvas; everything inside a row is the
  skin's and can be the demo's drawing call for call (bar, underline, cursor offset, centred text), including a bob
  from the scene's time (`UiContext.Time` is the root's clock, which starts with the scene's, or `FocusTime`).
  Differences a skin cannot express: none found for Meadow Run's and Neon Rift's `Ui.Menu`. Differences in
  behaviour: the focus skips a disabled row where `OptionList` rests on it (keep the row enabled and draw it muted,
  or use `MenuCursor`); up and down pressed together move the focus up then down (two sounds) where the list does
  nothing; the widgets act before `Scene.Update`, the list inside it; the scene's hash gains the root's.
- The example's scripted run (`samples/Examples/scripts/pixel-menu.script`, SDL hidden, `--scale 2` and `--scale 3`):
  seven captures each, looked at, byte-identical between the two scales.
