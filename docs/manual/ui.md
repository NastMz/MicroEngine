# UI widgets

The `MicroEngine.UI` namespace provides a small set of pixel-art widgets that games compose inside their own
scenes: a `Typewriter` for revealing text, a paged `DialogueBox`, a keyboard-driven `SelectionList`, a `UiTheme`
record that lets each game keep its own look, and the achievement banners and list screen. Widgets read input
through actions and draw with the built-in `BitmapFont`; there is no layout system or retained widget tree.

## Overview

| Type | Purpose |
| --- | --- |
| `UiTheme` | Panel style plus text, shadow, accent and muted colours shared by the widgets |
| `Typewriter` | Reveals a string character by character; raises `Revealed` for blip sounds |
| `DialogueBox` | Queue of pages in a panel with a typewriter and an optional speaker tag |
| `SelectionList` | Vertical menu navigated with up/down and chosen with confirm |
| `AchievementToasts` | Unlock banners drawn above every scene ([Achievements](achievements.md)) |
| `AchievementsScene` | Overlay listing a game's achievements ([Achievements](achievements.md)) |

Source: [`src/MicroEngine/UI/Widgets.cs`](../../src/MicroEngine/UI/Widgets.cs),
[`src/MicroEngine/UI/AchievementViews.cs`](../../src/MicroEngine/UI/AchievementViews.cs)

## `UiTheme`

```csharp
public sealed record UiTheme(PanelStyle Panel, Color Text, Color TextShadow, Color Accent, Color Muted)
{
    public static readonly UiTheme Default = new(
        new PanelStyle(Color.Hex("1c1b2e"), Color.Hex("f4f1de"), Color.Hex("3a3a5c"), Color.Hex("00000080")),
        Color.Hex("f4f1de"), Color.Hex("0a0a14"), Color.Hex("f2cc60"), Color.Hex("8a8aa8"));
}
```

Source: [`src/MicroEngine/UI/Widgets.cs`](../../src/MicroEngine/UI/Widgets.cs)

`PanelStyle(Fill, Border, Highlight, Shadow, InnerBorder)` is drawn by the `r.Panel(rect, style)` renderer
extension ([Rendering](rendering.md#irenderer)). Each sample game defines one theme and passes it to its widgets
and to `UseAchievements`.

## `Typewriter`

| Member | Behaviour |
| --- | --- |
| `Start(text)` | Resets and starts revealing `text` |
| `Update(dt)` | Reveals `CharactersPerSecond` characters per second (default 45) |
| `Finish()` | Reveals everything at once |
| `Visible`, `VisibleCount`, `Complete` | Current state |
| `Revealed` event | Raised for each newly revealed non-whitespace character |

## `DialogueBox`

`Show(speaker, params pages)` queues pages (and opens the box if it was closed). `Update(dt, input, confirm =
"confirm")` advances the typewriter; confirm first completes the current page, then moves to the next. It returns
`true` while the box is active, so a scene can skip its own input handling. Next SDK version (not published yet:
arrives with the next SDK version): `confirm` defaults to the map's own confirm action (`InputMap.Names`), so a game
that renamed it needs no argument; the same holds for `SelectionList.Update` and the achievements screen. The press a
widget acts on (a dialogue advancing, a menu choosing, the achievements screen closing) is consumed
(`InputMap.Consume`), so code that reads the same action after it in that update, such as the scene under an
overlay, does not act on it too. `Draw(r, font, area)` wraps text to
the area, shows up to `Lines` lines (default 3), a speaker name tag and a blinking arrow when the page is complete.
Events: `Blip` (per revealed character) and `Closed` (after the last page).

A page that wraps to more than `Lines` lines continues on the next confirm instead of being cut (`HasMore` says
whether more lines follow; `VisibleLines` are the lines in the box). With `Prompts` set (usually to `Game.Prompts`),
`{action}` in a page names the player's own control, and changes if the player switches device while the page shows:
`"Press {confirm} to swing it."` reads "Press Z" on a keyboard, "Press A" on a gamepad or on the touch button labelled A.
Both are not in a published SDK yet: the published SDK cuts the extra lines and prints `{confirm}` as written.

Courtyard Key wraps the box in an overlay scene that freezes the world:

```csharp
public sealed class DialogueScene : Scene
{
    private readonly DialogueBox _box = new() { Theme = UiStyle.Theme, Lines = 3 };
    // ...
    public override bool IsOverlay => true;

    public override void Enter()
    {
        _box.Blip += _ => { if (_blips++ % 2 == 0) Audio.Play("blip", 0.6f, 1f, 0.04f); };
        _box.Closed += () => _done = true;
        // {confirm} and {action} in a page name the player's own controls (key, pad button or touch button).
        _box.Prompts = Game.Prompts;
        _box.Show(_speaker, _pages);
    }

    public override void Update(float dt)
    {
        if (_done) return;
        _box.Update(dt, Input);
        if (_done)
        {
            Game.Scenes.Pop();
            _closed?.Invoke();
        }
    }

    public override void Draw(IRenderer r)
    {
        var area = new RectF(10, 128, 300, 46);
        _box.Draw(r, Font, area);
    }
}
```

Source: [`games/CourtyardKey/Scenes/DialogueScene.cs`](../../games/CourtyardKey/Scenes/DialogueScene.cs)

Long text needs no manual splitting, but a page per thought still reads better than one long page.

## `SelectionList`

`new SelectionList(params options)` needs at least one option. `Update(input, up, down, confirm)` (the actions
default to the map's names, see [Default actions](input.md#default-actions)) moves the highlight (wrapping when `Wrap` is true, the default) and returns the chosen index on confirm, otherwise `null`.
`SetEnabled(index, false)` greys an option out and makes it unselectable; `SetOptions` replaces the list. The
`Moved` event fires when the highlight changes. `Draw(r, font, position, theme, time, lineHeight = 12)` draws the
list with a bobbing cursor.

```csharp
private readonly SelectionList _menu = new("RESUME", "QUIT TO TITLE");
// ...
public override void Enter() => _menu.Moved += () => game.Sfx("menu-move", 0.6f);

public override void Update(float dt)
{
    if (Input.Pressed("pause")) { Close(); return; }
    if (_menu.Update(Input) is { } choice)
    {
        game.Sfx("menu-select", 0.7f);
        if (choice == 0) Close();
        else Game.Scenes.Reset(new TitleScene(game), Transition.Fade(0.6f));
    }
}
```

Source: [`games/NeonRift/Scenes/PlayScene.cs`](../../games/NeonRift/Scenes/PlayScene.cs)

Games can keep the list for its logic and draw it their own way: Neon Rift passes `_menu` to its own `Ui.Menu`
helper instead of calling `SelectionList.Draw`.

## Prompt glyphs

> **Not in a published SDK yet**: `PromptText` arrives with the next SDK release.

`PromptText.Draw(r, font, prompts, template, position, color, shadow, style)` draws one line in which each `{action}`
is a glyph instead of a word: a key cap on a keyboard, a round button in its colour on Xbox pads (A green, B red, X
blue, Y yellow), the shape on PlayStation pads, a ring with the button's label on a touch screen. `Measure` and
`DrawCentered` lay it out; `DrawGlyph` draws one `ActionPrompt`. `PromptGlyphStyle` sets the caps' colours
(`PromptGlyphStyle.From(theme)` takes them from a `UiTheme`). Use it for short hints ("{confirm} Talk"); dialogue text
uses `DialogueBox.Prompts`, which keeps words. See [Showing controls to the player](input.md#showing-controls-to-the-player).

## Achievement views

`AchievementToasts.Bottom` (not in a published SDK yet) shows the banners at the bottom of the screen instead of the
top, for games whose HUD is at the top.

`AchievementToasts` and `AchievementsScene` are documented with the rest of the system in
[Achievements](achievements.md#banners).

## Limitations

- Keyboard and gamepad navigation only; widgets do not respond to mouse or touch.
- Text uses the single built-in font; there is no rich text, right-to-left text or localisation support.
- Layout is manual (rectangles and positions in virtual pixels).
