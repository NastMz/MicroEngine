# UI-1: UI primitives for any game's interface

Date: 2026-11-06. Branch `docs/ui-1` (from `main` at 0e3408d). Issue: NastMz/MicroEngine#125. Manual:
[UI](../manual/ui.md), [Text](../manual/text.md).

Evidence words are used strictly: **run** means executed and its output read; **desk** means judged from source or
documentation without a prototype. This record is desk evidence: every claim names the code it was read from.

## 1. The question and the rule

The question is not what the four demos use, but whether a developer outside the project can build the interface of
any 2D game with the SDK: a floating or side menu, an RPG menu, a settings screen, an inventory grid, a dialogue box,
a speech bubble over a character, a banner that slides in.

General-purpose engines answer it with a kit of generic controls, a layout model and replaceable looks. Godot's
`Control` nodes and Unity's UGUI ship buttons, labels, sliders, toggles, scrolling containers, grids and text fields.
Their looks come from themes, style boxes and 9-slice images. Neither ships a dialogue box, a speech bubble or an
achievement screen: games compose those, or take them from third-party plugins.

The rule this record applies:

- **The engine owns primitives:** behaviour that is hard to get right or needs the engine's internals (input routing,
  pointer capture, focus, clipping, the text channel, the platform), with a neutral default look that a game can
  replace.
- **Every higher-level component must be buildable from those primitives** by a game, without engine changes.
- **The engine ships no genre components and no designed looks.** Components such as dialogue boxes and achievement
  banners are examples a game copies and restyles.

## 2. The primitives today

| Primitive | State | Source |
| --- | --- | --- |
| Custom widgets | Present: derive from `Widget` (`MeasureContent`, `ArrangeContent`, `DrawContent`), own children (`Adopt`, `Release`), take focus, receive `OnAction` before focus moves (so a focused widget can use left and right), `OnClick`, `OnFocusChanged`, `Update`, state hashing | `src/MicroEngine.UI/Widget.cs` |
| Layout containers | `Stack` (vertical or horizontal) and `Panel` (one child with padding) | `src/MicroEngine.UI/Widgets.cs` |
| Placement | One root (`UiRoot.Content`) with one `Anchor` and `Offset` | `src/MicroEngine.UI/UiRoot.cs` |
| Focus and keyboard | Directional focus, wrap, auto focus, `Focus(widget)`; actions by the map's names | `UiRoot.cs` |
| Pointer | Press and release on the same widget call `OnClick`; the press is captured until release. Widgets never see positions or moves | `UiRoot.cs` (press, release, move handling) |
| Wheel | Taken when over the content and then dropped: no widget receives it | `UiRoot.cs` (`InputEventKind.Wheel`) |
| Clipping | Only `TextField` clips its own drawing (`SetClip`); no container clips its children | `TextField.cs` |
| Text | `Label` draws lines split at `\n`; fonts can wrap (`IFont.Wrap`), but `Label` does not; letter-by-letter reveal lives in `Typewriter`, outside the widget tree | `Widgets.cs`, `src/MicroEngine.Text/IFont.cs`, `Typewriter.cs` |
| Text entry | `TextField`: caret, input method composition, on-screen keyboard; no selection, no clipboard | `TextField.cs` |
| Looks | `UiTheme` and `PanelStyle`: colours only. The renderer has no 9-slice drawing | `UiTheme.cs`, `src/MicroEngine.Graphics2D` |
| World to UI | Present as functions: `Camera2D.WorldToScreen`, `GameViews.CanvasToUi` | `Camera2D.cs`, `GameViews.cs` |
| Touch controls | Behaviour (fingers, sliding, taps that survive a frame, safe area) separated from looks (`TouchStyle`: shapes, colours, images) | `src/MicroEngine.Input.Touch` |

## 3. Higher-level components, decomposed

| Component a game wants | Built from | Missing primitive |
| --- | --- | --- |
| Menu (floating, side, RPG) | `Panel`, `Stack`, focusable item widgets drawing their own focus state | None for a short menu; a long one needs **clipping and scrolling** |
| `SelectionList` today | A `Stack` of focusable items, the cursor drawn by the focused item | None: pointer support comes with the widget tree |
| Dialogue box | `Panel`, a wrapped `Label` revealing letters, paging in game code | **Wrapping in `Label`**, **visible character count** |
| Speech bubble over a character | World point to UI point (present), a `Panel` placed there | **Positioned placement**: a container placing children at points or anchors |
| Banner sliding in (achievements, notifications) | `Panel` and `Label` in a corner, moved by a tween | **Positioned placement**, **per-widget offset and opacity** for animation |
| Settings screen | Focusable rows; a slider and a toggle as custom widgets | **Pointer position, moves and drag** for a slider; **clipping and scrolling** for long screens |
| Inventory or level select | Cells in rows and columns | **A grid layout** (a custom container can do it, but rows and columns are as basic as `Stack`) |
| Achievement list | Rows in a scrolling container | **Clipping and scrolling**, **wheel delivery** |
| Themed panels and buttons | An image stretched around fixed corners | **9-slice drawing** and an image option in `PanelStyle` |
| Name entry with paste | `TextField` | **Selection and clipboard**; the clipboard is a platform capability |
| Translated text in any script | Strings and fonts chosen by the game | **Shaping and right-to-left** for Arabic, Hebrew and Indic scripts |

## 4. Proposal

### Primitives to add

| # | Primitive | Why it is the engine's |
| --- | --- | --- |
| P1 | Pointer events on widgets: press, move and release with positions, capture until release, the wheel | Input routing and capture live in `UiRoot` |
| P2 | Clipping of a container's children, and a scrolling container that follows the focus | Clipping and what the pointer reaches inside it are rendering and routing concerns |
| P3 | A grid layout | Layout, as `Stack` |
| P4 | Positioned placement (children at points or anchors), and an offset and opacity per widget | Layout and drawing |
| P5 | `Label` wrapping to its width and a visible character count | Text layout; replaces `Typewriter` as a separate type |
| P6 | 9-slice drawing in the renderer and an image option in `PanelStyle` | Rendering |
| P7 | Text selection, and a clipboard capability on SDL, raylib and browsers | Platform |
| P8 | Shaping and right-to-left as an optional text module | Text system; a native dependency (HarfBuzz) on every target, WebAssembly included, so it stays optional |

### Components to move out of the engine

These become examples in `samples/` (rebuilt on the primitives, to prove them) or code inside the demos that use them:

| Component | Used by | Replacement |
| --- | --- | --- |
| `DialogueBox`, `Typewriter` | Courtyard Key, Trail Duel, examples | A dialogue example on P4 and P5 |
| `SelectionList` | All four demos, the tutorial, the examples, `AchievementsScene` | A menu example on the widget tree; demos keep a copy or move to widgets |
| `Toasts`, `AchievementToasts`, `AchievementsScene` | `Game` shows achievement banners by itself | The achievements module keeps tracking and its `Unlocked` event; a banner example on P4 shows them |
| Slider, toggle, value selector | Not in the engine | Examples on P1, proving the primitive |

### What stays

`Widget`, `UiRoot`, `Label`, `Button`, `Panel`, `Stack` and `TextField` are primitives or the smallest controls over
them; the touch controls stay, their looks already separated from their behaviour.

### Contract

Moving components out removes public types: a deprecation in one SDK release, migration notes, and an ADR recording
the rule above. The order that keeps every release working: add P1 to P6 with the examples that need them, then
deprecate the components those examples replace, then remove them. P7 and P8 are independent and can follow.
