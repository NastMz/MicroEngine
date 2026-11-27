# UI widgets

The `MicroEngine.UI` namespace provides the pieces a game builds its interface from: a retained widget tree laid out in
the UI view or on the game's canvas, with focus, pointer and text input
([Widgets in the UI view](#widgets-in-the-ui-view), [Widgets on the canvas](#widgets-on-the-canvas)), a `UiTheme`
record and skins that let each game keep its own look, text that reveals itself, a cursor for lists a scene draws
itself, and prompt glyphs that show the player's own controls. The engine ships behaviour, not designed components: a
dialogue box, a title menu, unlock banners or an achievements list are a few widgets with the game's own look, and
the [examples](../examples/focused.md) build each of them.

## Overview

| Type | Purpose |
| --- | --- |
| `UiRoot`, `Widget`, `Label`, `Button`, `Panel`, `Stack`, `ScrollView`, `Grid`, `Layer`, `TextField` | A widget tree laid out in the UI view, with focus, pointer and text input ([Widgets in the UI view](#widgets-in-the-ui-view)) |
| `UiTheme` | Panel style plus text, shadow, accent and muted colours shared by the widgets, and the skins that draw its buttons and panels |
| `ControlSkin`, `ControlState` | A button or a panel drawn by a function of the game's, from the control's state ([Skins](#skins)) |
| `UiSpace` | Where a root's widgets live: the UI view, or the game's canvas ([Widgets on the canvas](#widgets-on-the-canvas)) |
| `TextReveal` | Text shown a character at a time, for a label or for a box the game draws ([Text that wraps and reveals](#text-that-wraps-and-reveals)) |
| `MenuCursor` | The cursor of a list a scene draws itself ([A list the scene draws](#a-list-the-scene-draws)) |
| `PromptText` | One line of text whose `{action}` placeholders are drawn as the player's buttons ([Prompt glyphs](#prompt-glyphs)) |

Source: [`src/MicroEngine.UI`](../../src/MicroEngine.UI/), [`src/MicroEngine/UI`](../../src/MicroEngine/UI/)

The widgets and `UiTheme` are in `MicroEngine.UI.dll`, inside the `MicroEngine` package.

## `UiTheme`

```csharp
public sealed record UiTheme(PanelStyle Panel, Color Text, Color TextShadow, Color Accent, Color Muted)
{
    public static readonly UiTheme Default = new(
        new PanelStyle(Color.Hex("1c1b2e"), Color.Hex("f4f1de"), Color.Hex("3a3a5c"), Color.Hex("00000080")),
        Color.Hex("f4f1de"), Color.Hex("0a0a14"), Color.Hex("f2cc60"), Color.Hex("8a8aa8"));
}
```

Source: [`src/MicroEngine.UI/UiTheme.cs`](../../src/MicroEngine.UI/UiTheme.cs)

`PanelStyle(Fill, Border, Highlight, Shadow, InnerBorder)` is drawn by the `r.Panel(rect, style)` renderer
extension ([Rendering](rendering.md#irenderer)). Each sample game defines one theme and sets it as `Widgets.Theme`
or passes it to its own drawing code.

## Prompt glyphs

`PromptText.Draw(r, font, prompts, template, position, color, shadow, style)` draws one line in which each `{action}`
is a glyph instead of a word: a key cap on a keyboard, a round button in its colour on Xbox pads (A green, B red, X
blue, Y yellow), the shape on PlayStation pads, a ring with the button's label on a touch screen. `Measure` and
`DrawCentered` lay it out; `DrawGlyph` draws one `ActionPrompt`. `PromptGlyphStyle` sets the caps' colours
(`PromptGlyphStyle.From(theme)` takes them from a `UiTheme`). With the built-in font the glyphs are its nine-pixel
caps; with any other font (`IFont`: a scalable or shaped font at a size) they are as tall as a line of it and
labelled with it, and `rightToLeft: true` lays the pieces of the line from the right. Use it for short hints ("{confirm} Talk"); text that
keeps words goes through `ActionPrompts.Format`. See [Showing controls to the player](input.md#showing-controls-to-the-player).

## Text in several languages

A `Label`, a `Button` and a `TextField`'s placeholder take a `LocalizedText`, a key of the game's string tables with
its values, in place of a string: `new Button(new LocalizedText("menu.play"), Play)`,
`label.Localized = new("hud.gems", ("count", 3))`. They show the text in the language in use and lay themselves out
again when it changes; in a right-to-left language a label's lines start from its right edge
([Localization](localization.md#widgets)).

## Widgets in the UI view

A scene's `Widgets` is a widget tree the engine lays out, draws,
updates and gives input to. Set its `Content` (usually in `Enter`) and react to the widgets' events:

```csharp
public sealed class TitleScene : Scene
{
    public override void Enter()
    {
        var name = new TextField { Placeholder = "YOUR NAME", MaxLength = 12 };
        Widgets.Content = new Panel(new Stack()
            .Add(new Label("NEW GAME"))
            .Add(name)
            .Add(new Button("START", () => Game.Scenes.Replace(new PlayScene(name.Text))))
            .Add(new Button("QUIT", Game.Quit)));
        Widgets.Cancelled += Game.Quit;              // cancel that no widget used
    }

    public override void Update(float dt) { }        // the widgets already took what they used
    public override void Draw(IRenderer world) => DrawBackdrop(world);
}
```

| Type | What it is |
| --- | --- |
| `UiRoot` | The tree's root (`Scene.Widgets`): `Content`, `Anchor` and `Offset` (where the content sits in its view), `Space` (the UI view or the canvas), `Font`, `Theme`, `Actions`, `Focused`, `FocusTime`, `Focus(widget)`, `MoveFocus(direction)`, `AutoFocus`, `WrapFocus`, the `Cancelled` and `FocusChanged` events |
| `Widget` | The base class: `Bounds` (UI units, from the last layout), `Visible`, `Enabled`, fixed `Width` and `Height`, `IsFocused`, `IsHovered`, `IsPressed`; derive from it for a widget of your own (`MeasureContent`, `ArrangeContent`, `DrawContent`, `OnAction`, `OnClick`, the pointer's `OnPointerDown`, `OnPointerMove`, `OnPointerUp` and `OnPointerCancel`, `OnWheel`; see [Controls of your own](#controls-of-your-own)); a container sets `ClipsChildren` to cut its children at its bounds, and `InterceptsDrag` to take a drag that starts on a child ([Long content](#long-content)); `Offset` and `Opacity` move and fade a widget with everything in it ([Placing widgets anywhere](#placing-widgets-anywhere)) |
| `Label` | Text, one or more lines, in the theme's text colour or its own `Color`; with `Wrap` it breaks to its fixed `Width`, and `Reveal` or `VisibleCharacters` shows part of it ([Text that wraps and reveals](#text-that-wraps-and-reveals)) |
| `Button` | A label in a panel; confirm while it has the focus, or a click on it, raises `Clicked`. `Padding` is the room around the label, and a `Skin` draws it the game's way ([Skins](#skins)) |
| `Panel` | A box (the theme's panel, or its own `Style`, either of which can be an image: [Your own look](#your-own-look); or its own `Skin`) with `Padding` around one `Content` |
| `Stack` | Children one after another, `Vertical` or `Horizontal`, with `Spacing` and a cross-axis `Align` |
| `ScrollView` | A window of fixed `Height` (or `Width`, `Orientation.Horizontal`) onto a longer `Content`: `Scroll`, `MaxScroll`, `WheelStep`, `ShowScrollBar` ([Long content](#long-content)) |
| `Grid` | Children in rows of `Columns` equal cells, left to right then top to bottom, with `Spacing` and an `Align` within the cell ([Grids](#grids)) |
| `Layer` | The whole UI view, each child against an edge or corner (`Add(child, anchor, offset)`, `Place`) or at a point (`AddAt(child, point, pivot)`, `Move`); its empty areas let the pointer through ([Placing widgets anywhere](#placing-widgets-anywhere)) |
| `TextField` | One line of editable text: `Text`, `Caret`, `Placeholder`, `MaxLength`, the input method's `Composition`, the selection (`Select`, `SelectAll`, `SelectedText`) and the clipboard (`Copy`, `Cut`, `Paste`); `Changed` and `Submitted` events ([Text entry](#text-entry)) |

**Layout.** Widgets are measured and placed in UI units (window pixels divided by the UI scale, the screen's pixel
density unless `Presentation.UiScale` sets one, see
[The world view and the UI view](rendering.md#the-world-view-and-the-ui-view)) every frame, so a menu stays centred
when the window changes size. Padding and spacing default to multiples of a layout unit taken from the font: 1 UI unit
for the built-in 10-pixel font, more for a larger one, so a `ScalableFont` set as `Widgets.Font` grows the margins with
the text.

**Keyboard and gamepad.** The first enabled focusable widget takes the focus. The up, down, left and right actions move
it to the nearest widget in that direction (from the last one to the first when `WrapFocus` is on); confirm presses the
focused button; cancel raises `Cancelled`. The widgets read these actions under the input map's current names
(`InputMap.Names`, see [Default actions](input.md#default-actions)): a game that renames `confirm` keeps working
widgets, and a gameplay action that happens to be called `confirm` never presses a button. `Widgets.Actions` gives the
widgets other actions (`new UiActions { Confirm = "select" }`). An action a widget acts on is consumed, so the scene's
`Update`, which runs after the widgets, does not see it.

**Mouse and touch.** The widgets are the highest input layer: a press on the content (a click or a tap) is theirs, with
everything that press does until it is released, and never reaches gameplay; the wheel over them does not either.
Releasing on the widget that was pressed clicks it, and a press focuses what it lands on. While the pointer is over the
content, `Game.Views.PointerOnUi` is true and the world pointer is not present.

The press goes to the focusable widget under the pointer, or to its nearest focusable container, which then holds it:
`OnPointerDown` with the position, `OnPointerMove` for every move of that pointer (also outside the widget), then
`OnPointerUp` and, when the release is on the widget, `OnClick`. A press the system cancels, or one whose widget leaves
the tree, ends with `OnPointerCancel` instead. Positions are in UI units, like `Bounds`. The wheel goes to the widget
under the pointer: `OnWheel` returns true to take it, otherwise its container is asked, up to the root.

**Text.** A focused `TextField` turns the text channel on (the on-screen keyboard on a phone; the input method's
candidate window next to the field), inserts what is typed at the caret and shows what an input method is still
composing, underlined. While it has the focus it takes the keyboard from gameplay: Backspace, Delete, the arrows, Home
and End edit, Enter raises `Submitted`, Escape leaves the field, Tab and the up and down keys move the focus. Text input
stops when the field loses the focus or its scene leaves or is covered. Selection and the clipboard: [Text entry](#text-entry).

The widgets' state (the focus, the fields' text) is part of the scene's state hash, so scripted runs and replays see
the same interface. A scene that never reads `Widgets` has none and pays nothing.

### Menus

A menu is a column of focusable widgets: the focus is the cursor, `WrapFocus` takes it from the last item to the first,
and an item with `Enabled` off is drawn muted and skipped. `FocusChanged` reports every move of the focus with the
widget it left (`From`) and the one it reached (`To`), for a cursor sound or a description that follows the cursor; the
first focus a scene gives has no `From`. `Hovered` is the widget under the pointer, so the cursor can follow the mouse.
This title menu does all of it:

<!-- code: samples/Examples/Examples/MenuExample.cs#menu -->
```csharp
public override void Enter()
{
    Result = new Label();
    Items =
    [
        new Button("NEW GAME", () => Result.Text = "A NEW ADVENTURE BEGINS"),
        new Button("CONTINUE") { Enabled = false },                       // no save yet: muted, and the cursor skips it
        new Button("OPTIONS", () => Result.Text = "OPTIONS WOULD OPEN HERE"),
        new Button("QUIT", () => Game.Scenes.Pop()),
    ];
    var column = new Stack { Spacing = 3, Align = Align.Start };
    foreach (var item in Items)
    {
        item.Skin = MenuLine;                                             // the game's look for a line of the menu
        item.Padding = new Vector2(5, 0);                                 // as tall as its text, room for the cursor
        column.Add(item);
    }
    Widgets.Content = new Stack { Spacing = 12 }.Add(column).Add(Result);
    Widgets.FocusChanged += change =>
    {
        if (change.From is null) return;                                 // the first focus is not a move
        Moves++;
        Audio.TryPlay("select", 0.3f);                                   // a blip, if the game registered one
    };
}

public override void Update(float dt)
{
    if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
    // The cursor follows the mouse: when the pointer reaches another item, that item takes the focus.
    var hovered = Widgets.Hovered;
    if (!ReferenceEquals(hovered, _hovered) && hovered is { Focusable: true }) Widgets.Focus(hovered);
    _hovered = hovered;
}
```

Each item is a `Button` whose look is the game's: a skin draws its text and a cursor that bobs beside it while it has
the focus, and the button keeps the focus, confirm and the click ([Skins](#skins)):

<!-- code: samples/Examples/Examples/MenuExample.cs#item -->
```csharp
/// <summary>
/// A line of the menu: its text, and a cursor that bobs beside it while it has the focus. A skin draws all of a
/// button from its state; the button keeps the focus, confirm and the click.
/// </summary>
private static void MenuLine(IRenderer r, in UiContext context, in ControlState state)
{
    var theme = context.Theme;
    float u = context.Unit;
    var color = !state.Enabled ? theme.Muted : state.Focused ? theme.Accent : theme.Text;
    if (state.Focused)
    {
        float bob = MathF.Round(MathX.Wave(context.Time, 2f, 1f) * u);
        context.Font.Draw(r, "▶", new Vector2(state.Bounds.X + bob, state.Bounds.Y), theme.Accent);
    }
    context.Font.DrawShadowed(r, state.Text, new Vector2(state.Bounds.X + 10 * u, state.Bounds.Y), color, theme.TextShadow, new Vector2(u));
}
```

### A list the scene draws

A scene that draws its rows itself (a bag, a shop, a save list, a two-by-two battle menu) keeps its cursor in a
`MenuCursor`. It holds no labels and draws nothing: the scene reads `Index` and draws.

```csharp
private readonly MenuCursor _menu = new(3);                   // RESUME, OPTIONS, QUIT

public override void Enter() => _menu.Moved += () => Audio.TryPlay("move");

public override void Update(float dt)
{
    if (_menu.Update(Input) is { } chosen) Choose(chosen);    // moves, then confirm chooses
}

public override void Draw(IRenderer r)
{
    for (int i = 0; i < Labels.Length; i++)
        Font.Draw(r, Labels[i], new Vector2(120, 60 + i * 12), i == _menu.Index ? Gold : Cream);
}
```

| Member | What it does |
| --- | --- |
| `Count`, `Columns` | How many options, and how many in a row (1: a vertical list). In a grid, left and right move within the row and up and down between rows; a cell missing from a shorter last row is never reached. Both can change while the list is shown |
| `Index` | The highlighted option; setting it is silent |
| `Wrap` | Whether moving past an end goes on from the other end (true by default) |
| `Update(input, actions)` | Moves by the direction actions pressed in this update, then returns the highlighted option when confirm is pressed on it. The press that chooses is consumed |
| `Move(input, actions)`, `Move(direction)` | The moves alone, from the actions or from code |
| `SetEnabled(index, enabled)` | An option that cannot be chosen. The cursor still rests on it; a confirm there returns nothing and is not consumed, so the scene can answer with a sound |
| `MoveTo(index)` | The cursor put on a row by the player's pointer: raises `Moved` when it changes |
| `Moved` | Raised when a direction or `MoveTo` changes the option, for the cursor's sound |
| `Top(visible)` | The first row to draw of a list that shows `visible` rows: it moves just enough to keep the cursor in the window |

The actions are read under the input map's names, or the ones given in a `UiActions`, like the widgets'. A cursor is
part of the scene's state when the scene adds it to its state hash (`hash.Add(_menu)`).

Choose between the two by what the screen is. Widgets lay themselves out, take the mouse and touch, skip what is
disabled and scroll; a `MenuCursor` is a number the scene draws around, for screens whose rows are drawn by hand.

### Text that wraps and reveals

A `Label` keeps the lines its text has (`\n` starts a new one). With `Wrap` set and a fixed `Width`, it also breaks
the text between words so no line is wider, and grows in height to hold them; a word wider than the label on its own
breaks between characters. `Lines` gives the lines it draws, as of the last layout.

A label with a `Reveal` shows its text a character at a time, the typewriter of a dialogue box. The label keeps the
size and the lines of its whole text, so no word jumps to the next line as it appears; `VisibleLines` gives what
shows. A `TextReveal` holds the rate (`CharactersPerSecond`, 45 by default), raises `CharacterRevealed` for every
character that is not a space (the place for a typing sound), shows the rest at once with `Finish`, and says when
all of it shows (`Complete`). The label starts it again whenever its text changes, by a new page or a change of
language, and advances it on the scene's clock.

Characters are counted as a reader counts them: a letter with its accents or vowel marks, an emoji made of several
code points and a line break are one each, so text in any script is never cut inside a character. They appear in
reading order, and a right-to-left line grows from its right edge. `PauseAfter` holds the reveal after a character:
`rune => rune.Value is '.' or '!' or '?' ? 0.3f : 0` pauses at the end of a sentence.

The dialogue of the [focused examples](../examples/focused.md#dialogue) is a panel, a name tag and two labels. Its
label reveals each box of text, and its update only blinks the arrow and reads confirm:

<!-- code: samples/Examples/Examples/DialogueExample.cs#reveal -->
```csharp
public override void Update(float dt)
{
    if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
    if (!Dialogue.Visible) return;                                // the buttons take confirm themselves
    var reveal = Text.Reveal!;                                    // advanced by the label, on the scene's clock
    _more.Opacity = reveal.Complete && (int)(Age * 3) % 2 == 0 ? 1 : 0;   // a blinking arrow that keeps its room: confirm goes on
    if (!Input.Pressed("confirm")) return;
    if (!reveal.Complete) reveal.Finish();                        // the first confirm shows the rest
    else if (_chunks.Count > 0) NextBox();                        // then the rest of a long page
    else NextPage();                                              // then the next page
}
```

A box the game draws itself uses the same `TextReveal` without a label: `Start(text)`, `Update(dt)` every update,
then `Visible` (the part that shows, without a copy) or `VisibleLines` (what shows of each line of the text) to
draw. Revealing allocates nothing: a line that shows whole is the line itself, and only the line being revealed is a
new string, when another character of it shows. `VisibleCharacters` on a label sets the count by hand instead, in
`char`s of the text as written (`TextReveal.VisibleLength` is that count); a label with a `Reveal` does not read it.

A page that does not fit the box is split into boxes of three lines with the font's own `Wrap`, the same wrapping a
label does, so no text is lost:

<!-- code: samples/Examples/Examples/DialogueExample.cs#pages -->
```csharp
/// <summary>
/// The next page, cut into boxes of three lines with the same wrapping the label does: the font breaks the text to
/// the label's width, and every three lines become one box.
/// </summary>
private void NextPage()
{
    if (!_pages.TryDequeue(out var page))
    {
        Dialogue.Visible = false;                                 // the last page: the question
        Answers.Visible = true;
        return;
    }
    Speaker.Text = page.Speaker;
    var lines = Font.Wrap(page.Text, (int)TextWidth);
    for (int i = 0; i < lines.Count; i += LinesPerBox)
        _chunks.Enqueue(string.Join('\n', lines.Skip(i).Take(LinesPerBox)));
    NextBox();
}

private void NextBox()
{
    Text.Text = _chunks.Dequeue();                                // a new text starts its reveal
}
```

### Text entry

A `TextField` selects as text boxes do: Shift with the arrows, Home or End moves the caret and selects what it passes,
a press puts the caret where it lands and a drag selects, and Ctrl (Command on macOS) with A selects everything. Typing,
Backspace and Delete replace or delete the selection; an arrow without Shift ends it at that side. `Select(anchor,
caret)` and `SelectAll` select from code, and `SelectionStart`, `SelectionLength` and `SelectedText` read it.

Ctrl or Command with C, X and V copy, cut and paste through `Widgets.Clipboard`, the platform's clipboard
(`IClipboard`, also yours through `Game.TryGetCapability`). A paste is one line: line breaks and tabs become spaces,
and it is cut to `MaxLength`. `Copy`, `Cut` and `Paste` do the same from code, for buttons of the game's own on screens
without a keyboard. This name entry greets on Enter and has copy and paste buttons for the whole field:

<!-- code: samples/Examples/Examples/TextEntryExample.cs#field -->
```csharp
public override void Enter()
{
    Status = new Label("TYPE A NAME, THEN ENTER") { Color = UiTheme.Default.Muted };
    Field = new TextField { Placeholder = "YOUR NAME", MaxLength = 16, Columns = 120 };
    Field.Submitted += name => Status.Text = name.Length > 0 ? $"HELLO, {name.ToUpperInvariant()}" : "THE NAME IS EMPTY";
    CopyButton = new Button("COPY", Copy);
    PasteButton = new Button("PASTE", Paste);
    Widgets.Content = new Stack { Spacing = 8 }
        .Add(Field)
        .Add(new Stack { Orientation = Orientation.Horizontal, Spacing = 8 }.Add(CopyButton).Add(PasteButton))
        .Add(Status);
    Widgets.Focus(Field);
}

// The buttons act on the whole field: select it all, then copy, or paste over it.
private void Copy()
{
    Field.SelectAll();
    Status.Text = Field.Copy() ? "COPIED" : "NOTHING TO COPY";
}

private void Paste()
{
    Field.SelectAll();
    Status.Text = Field.Paste() ? "PASTED" : "NOTHING TO PASTE";
}
```

In a browser, a page cannot read the clipboard without asking the player, so the game's clipboard holds the text of
the page's latest paste (Ctrl+V or Command+V while a text field has the focus) or of the game's latest copy; a paste
button of the game's own pastes that text. Copies reach the system clipboard, which browsers allow right after a key
press or a click.

### Widgets on the canvas

A pixel-art game's interface is part of its picture: a menu on a 320x180 canvas has pixels the size of the game's.
Set `Widgets.Space = UiSpace.Canvas` first thing in `Enter`, and the scene's widgets live on the canvas:

- They are laid out in canvas pixels, in the canvas's size, on whole pixels: a centred menu or a fractional offset
  never lands between two.
- The pointer reaches them in canvas pixels, mouse and touch alike, and a pointer over them is on the UI
  (`Game.Views.PointerOnUi`) as in the UI view. The keyboard and the gamepad work the same in both.
- The scene draws them: call `Widgets.Draw(renderer)` from the scene's `Draw`, at the point of its picture where the
  interface belongs (over the world, under a flash). They are drawn in canvas pixels whatever camera the scene's
  world uses, they go through the scene's post-processing with the rest of the picture, and a capture of the canvas
  shows them. The engine does not draw them in the UI view.

The same widgets, focus, skins and text reveal work in both spaces. This menu and dialogue box are buttons, panels
and a label on the canvas; `ButtonSkin` and `PanelSkin` give every one of them the game's look ([Skins](#skins)):

<!-- code: samples/Examples/Examples/PixelMenuExample.cs#canvas -->
```csharp
public override void Enter()
{
    Widgets.Space = UiSpace.Canvas;                       // canvas pixels: the interface is part of the picture
    Widgets.Theme = new UiTheme(new PanelStyle(Night, Cream, Night, Ink), Cream, Ink, Gold, Dusk)
    {
        ButtonSkin = Row,                                 // every button is a menu row
        PanelSkin = Box,                                  // every panel is the game's box
    };

    Rows =
    [
        new Button("TALK", Talk),
        new Button("TRADE") { Enabled = false },          // nothing to trade yet: muted, and the cursor skips it
        new Button("LEAVE", () => Game.Scenes.Pop()),
    ];
    var rows = new Stack { Spacing = 2 };
    foreach (var row in Rows)
    {
        row.Width = 96;
        row.Padding = new Vector2(0, 2);                  // a row is a line of text and two pixels above and below
        rows.Add(row);
    }
    Menu = new Panel(rows) { Padding = 6 };

    Text = new Label
    {
        Width = 236, Height = 3 * Font.LineHeight + 2, Wrap = true,
        Reveal = new TextReveal { CharactersPerSecond = 45 },
    };
    Text.Reveal.CharacterRevealed += _ =>
    {
        Blips++;
        Audio.TryPlay("select", 0.3f);                    // a blip per letter, if the game has the sound
    };
    More = new Button("▼", Advance) { Padding = new Vector2(2, 0), Skin = Arrow };
    Dialogue = new Stack { Spacing = 0, Align = Align.Start, Visible = false }
        .Add(new Panel(new Label("GARDENER") { Color = Ink, Shadow = false }) { Padding = 3, Skin = Tag })
        .Add(new Panel(new Stack { Spacing = 0, Align = Align.End }.Add(Text).Add(More)) { Padding = 6 });

    Widgets.Content = new Layer()
        .Add(Menu, Anchor.Center, new Vector2(0, -24))
        .Add(Dialogue, Anchor.Bottom, new Vector2(0, -6));
}

public override void Draw(IRenderer r)
{
    Backdrop(r);
    Widgets.Draw(r);                                      // on the canvas the scene says where its widgets go
    Heading.Draw(r, Font, "PIXEL MENU");
}
```

The dialogue is a few lines over the label's reveal. The arrow under the text is a button that has the focus while
the dialogue is open, so confirm, a click and a tap all go on through it:

<!-- code: samples/Examples/Examples/PixelMenuExample.cs#dialogue -->
```csharp
private void Talk()
{
    _page = 0;
    Text.Text = Pages[0];
    Text.Reveal!.Start(Pages[0]);                         // from the first letter, also the second time
    Dialogue.Visible = true;
    Menu.Enabled = false;                                 // the menu waits under the dialogue
    Widgets.Focus(More);                                  // confirm, a click or a tap on the arrow goes on
}

/// <summary>The first press shows the rest of the page; the next one turns it; the last one closes the box.</summary>
private void Advance()
{
    if (!Text.Reveal!.Complete) Text.Reveal.Finish();
    else if (++_page < Pages.Length) Text.Text = Pages[_page];   // a new text starts its reveal
    else
    {
        Dialogue.Visible = false;
        Menu.Enabled = true;
        Widgets.Focus(Rows[0]);
    }
}
```

A scene has one root, in one space: a game with a canvas menu and a sharp overlay at the window's resolution draws
the overlay from `DrawUi`, or keeps it in another scene pushed over this one.

### Long content

A `ScrollView` shows content longer than the space it has: a level select, a list of saves, a shop, the credits. Give
it a fixed height (a fixed width for a horizontal one); its content keeps the size it measures and is cut at the
view's edges. The list below holds thirty buttons in a view of 110 UI units:

<!-- code: samples/Examples/Examples/LongListExample.cs#list -->
```csharp
public override void Enter()
{
    var levels = new Stack();
    for (int i = 1; i <= 30; i++)
    {
        string name = $"LEVEL {i:00}";
        levels.Add(new Button(name, () => Picked = name) { Width = 120 });
    }
    // A fixed height: the list is longer, so the view scrolls and cuts it at its edges.
    List = new ScrollView(levels) { Height = 110 };
    Widgets.Content = new Panel(List);
    Widgets.Cancelled += () => Game.Scenes.Pop();
}
```

The view scrolls in three ways:

- **The focus.** When a widget inside takes the focus, the view scrolls just enough to show it, so up and down walk
  the whole list.
- **The wheel.** The wheel over the view scrolls it, three lines of the font per notch (`WheelStep`).
- **A drag.** A press that moves more than a few units scrolls the content with the pointer, also when it started on a
  button inside. The button's press is then cancelled and it does not click, so a finger can flick through a list of
  buttons on a phone. A tap without a drag still clicks.

While the content does not fit, a thin bar in a strip along the view's edge shows where the window is
(`ShowScrollBar`). A widget scrolled out of view is cut, and the pointer reaches it only inside the view: what shows
around the view takes the clicks there.

The view is built on two members any container can use. `ClipsChildren` makes the base class cut what the children
draw at the widget's bounds, and keep the pointer outside from reaching them. `InterceptsDrag` lets the container take
a press from the child it started on once it becomes a drag. A widget that draws within a smaller area of its own sets
the clip with `UiContext.ClipTo`, which nests inside the clips around it and restores them when disposed;
`IRenderer.SetClip` alone would remove them.

### Grids

A `Grid` places its children in rows of `Columns` cells: an inventory, a level select, a roster, a keyboard on screen.
Every cell is as large as the largest child, and a child fills its cell unless `Align` says otherwise; hidden
children take no cell. Focus moves between cells by direction like anywhere else, so down goes to the cell below. This
inventory puts twelve slots in four columns, with the focused slot's description under them:

<!-- code: samples/Examples/Examples/InventoryExample.cs#grid -->
```csharp
public override void Enter()
{
    _about = new Label(" ");
    var grid = new Grid(columns: 4);
    foreach (var (name, count, about) in Items)
    {
        var slot = new ItemSlot(name, count);
        slot.Focused += () => _about.Text = about;                // the description follows the focus
        slot.Used += () => _about.Text = slot.Count > 0 ? $"USED {name}, {slot.Count} LEFT" : $"NO {name} LEFT";
        grid.Add(slot);
    }
    Widgets.Content = new Panel(new Stack().Add(grid).Add(_about));
    Widgets.Cancelled += () => Game.Scenes.Pop();
}
```

The slot is a widget of the example's own: the item's name and count in a box, used by confirm or a click. A long
grid goes inside a `ScrollView` ([Long content](#long-content)).

<!-- code: samples/Examples/Examples/InventoryExample.cs#slot -->
```csharp
/// <summary>One inventory slot: the item's name and count in a square box. Confirm or a click uses one.</summary>
public sealed class ItemSlot(string name, int count) : Widget
{
    public string Name { get; } = name;
    public int Count { get; private set; } = count;
    public event Action? Focused;
    public event Action? Used;
    public override bool Focusable => true;

    protected override Vector2 MeasureContent(UiContext context) => new(40 * context.Unit);

    protected override bool OnAction(UiAction action)
    {
        if (action != UiAction.Confirm) return false;
        Use();
        return true;
    }

    protected override void OnClick() => Use();

    protected override void OnFocusChanged(bool focused)
    {
        if (focused) Focused?.Invoke();
    }

    private void Use()
    {
        if (Count == 0) return;
        Count--;
        Used?.Invoke();
    }

    protected override void DrawContent(IRenderer r, UiContext context)
    {
        var theme = context.Theme;
        r.Panel(Bounds, IsFocused ? theme.Panel with { Border = theme.Accent } : theme.Panel);
        if (Count == 0) return;
        var at = new Vector2(Bounds.X + 3 * context.Unit, Bounds.Y + 3 * context.Unit);
        context.Font.Draw(r, Name, at, IsFocused ? theme.Accent : theme.Text);
        string n = $"x{Count}";
        context.Font.Draw(r, n, new Vector2(Bounds.Right - 3 * context.Unit - context.Font.MeasureWidth(n), Bounds.Bottom - 3 * context.Unit - context.Font.LineHeight), theme.Muted);
    }
}
```

### Placing widgets anywhere

A `Layer` covers the UI view and places each child on its own, at the size the child measures: against an edge or a
corner, or with a pivot point of the child at a point of the layer. Where no child is, presses and the wheel pass
through to gameplay. This scene puts a speech bubble over a character and a banner in the top-right corner:

<!-- code: samples/Examples/Examples/LayerExample.cs#layer -->
```csharp
public override void Enter()
{
    Bubble = new Panel(new Label("HELLO! A FINE NIGHT FOR A WALK.") { Width = 70, Wrap = true });
    Banner = new Panel(new Label("ACHIEVEMENT\nFIRST STEPS")) { Opacity = 0 };
    _layer = new Layer()
        .AddAt(Bubble, Head(), Anchor.Bottom)                       // its bottom centre over the head
        .Add(Banner, Anchor.TopRight, new Vector2(-4, 16));         // 4 units from the right, under the heading
    Widgets.Content = _layer;
}

/// <summary>The character's head in UI units: from the world to the canvas (no camera here), then to the UI view.</summary>
private Vector2 Head() => Game.Views.CanvasToUi(Character - new Vector2(0, 2));
```

The character lives in the world. Its position goes to the UI view through `Game.Views.CanvasToUi`, after
`Camera2D.WorldToScreen` when a camera moves the world, and `Move` puts the bubble there every update:

<!-- code: samples/Examples/Examples/LayerExample.cs#follow -->
```csharp
public override void Update(float dt)
{
    if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
    _x += _direction * 40 * dt;
    if (_x is > 260 or < 60) _direction = -_direction;
    _layer.Move(Bubble, Head());                                    // every update: the bubble follows
    if (Input.Pressed("confirm")) ShowBanner();
}
```

Every widget has an `Offset`, a shift from where the layout puts it, and an `Opacity`. Both apply to everything inside
the widget, and the pointer follows the offset. Tweens animate them, here sliding the banner in as it fades in:

<!-- code: samples/Examples/Examples/LayerExample.cs#banner -->
```csharp
/// <summary>Slides the banner in from the right as it fades in, then fades it out two seconds later.</summary>
private void ShowBanner()
{
    Tweens.Add(0.4f, t =>
    {
        Banner.Offset = new Vector2((1 - t) * 60, 0);
        Banner.Opacity = t;
    }, Ease.OutCubic);
    Tweens.Add(0.4f, t => Banner.Opacity = 1 - t, delay: 2.4f);
}
```

Opacity multiplies down the tree. The theme a widget draws with is already faded (`UiContext.Theme`), so widgets that
draw in theme colours fade without code of their own; a colour of a widget's own goes through `UiContext.Fade`.

### Your own look

The widgets draw with the theme's colours, or with images of the game's own. A `NineSlice` is the part of a texture
used as a frame: its border (`Left`, `Top`, `Right`, `Bottom` pixels, or one number for all four) is drawn at its size
in the corners and stretched along the edges, and its centre is stretched to fill, so one small image frames a widget
of any size. `Scale` draws the border thicker. A `PanelStyle` with an `Image` draws that image instead of its box of
colours, multiplied by `ImageTint` when set; its `Shadow` still draws, in the image's shape. Buttons draw the theme's
panel, and a `Panel` its own `Style` when it has one. This menu gives its buttons a wooden frame and its panel
parchment, both from one image:

<!-- code: samples/Examples/Examples/SkinExample.cs#skin -->
```csharp
public override void Enter()
{
    // Content/frames.png holds two 12x12 frames side by side, each with a 4-pixel border: wood, then parchment.
    _frames = Game.Graphics.CreateTexture(new ContentFolder().Image("frames.png"), TextureFilter.Nearest);
    var wood = new NineSlice(_frames, new RectF(0, 0, 12, 12), 4);
    var parchment = new NineSlice(_frames, new RectF(12, 0, 12, 12), 4);

    // The theme's panel is what buttons draw: here the wooden frame, its text light on the dark wood.
    var theme = UiTheme.Default with { Accent = Color.Hex("ffd66b"), Text = Color.Hex("f4e4c1") };
    Widgets.Theme = theme with { Panel = theme.Panel with { Image = wood } };

    var buttons = new Stack { Spacing = 2 };
    foreach (var name in new[] { "CONTINUE", "OPTIONS", "QUIT TO TITLE" })
        buttons.Add(new Button(name, () => Chosen = name) { Width = 100 });
    Menu = new Panel(new Stack { Spacing = 4 }
        .Add(new Label("PAUSED") { Color = Color.Hex("4a3426"), Shadow = false })
        .Add(buttons))
    {
        Padding = 10,                                       // clear of the 4-pixel border
        Style = theme.Panel with { Image = parchment },     // a panel of its own look
    };
    Widgets.Content = Menu;
}

public override void Exit() => _frames.Dispose();
```

A button with an image shows its state by tinting it: with the theme's accent while it has the focus, darker while it
is pressed. For other looks per state, give it a skin ([Skins](#skins)); the renderer's `DrawNineSlice` draws a
nine-slice anywhere. Keep a nine-slice's centre plain: it is stretched, so a pattern in it is stretched too.

### Skins

A `ControlSkin` is a function that draws a control: `(renderer, context, state) => ...`. Set it on a button or a
panel (`Skin`), or on the theme for every button or panel of a root (`UiTheme.ButtonSkin`, `UiTheme.PanelSkin`). The
control keeps its behaviour, its size and its place in the layout, and the skin draws all of its look from the
state it is given:

| `ControlState` | |
| --- | --- |
| `Bounds` | Where the control is, in the root's units |
| `Text` | A button's label in the language in use; empty for a panel |
| `Enabled`, `Focused`, `Hovered`, `Pressed` | The control's state: disabled, under the cursor of the menu, under the pointer, held |
| `FocusTime` | Seconds the control has had the focus, zero without it: a cursor that starts its motion when it lands |

`context` carries the font, the theme, the layout unit and the root's clock (`Time`, which never starts again). Its
theme is already faded by the opacity around the control; a colour of the skin's own goes through `context.Fade`.
`Button.Padding` is the room around the label, each side, so a skin without a box sets the button's size: a row as
tall as a line of text is `new(5, 0)`. A panel's skin replaces its box; its content is drawn over it. A panel with a
`Style` of its own keeps that style rather than the theme's skin.

The rows and boxes of the pixel menu are three skins: a bar, an underline and a cursor that bobs a pixel twice a
second; a box with a double border; a plain plate for the speaker's name.

<!-- code: samples/Examples/Examples/PixelMenuExample.cs#skins -->
```csharp
/// <summary>A menu row: a bar and an underline behind the focused one, a cursor that bobs beside it, the text centred.</summary>
private static void Row(IRenderer r, in UiContext ui, in ControlState s)
{
    var b = s.Bounds;
    if (s.Focused)
    {
        r.FillRect(new RectF(b.X, b.Y, b.Width, b.Height - 1), ui.Theme.Accent.WithAlpha(0.22f));
        r.FillRect(new RectF(b.X, b.Bottom - 1, b.Width, 1), ui.Theme.Accent.WithAlpha(0.6f));
        int bob = (int)(s.FocusTime * 4) % 2;             // a pixel to the right twice a second, from the moment it lands
        ui.Font.Draw(r, "▶", new Vector2(b.X + 4 + bob, b.Y + 2), ui.Theme.Accent);
    }
    var color = !s.Enabled ? ui.Theme.Muted : s.Pressed ? Sky : s.Focused ? ui.Theme.Accent : ui.Theme.Text;
    float x = MathF.Round(b.X + (b.Width - ui.Font.MeasureWidth(s.Text)) / 2);
    ui.Font.DrawShadowed(r, s.Text, new Vector2(x, b.Y + 2), color, ui.Theme.TextShadow);
}

/// <summary>The game's box: a dark shadow a pixel down and right, a light border, a dark line inside it, the fill.</summary>
private static void Box(IRenderer r, in UiContext ui, in ControlState s)
{
    var b = s.Bounds;
    var p = ui.Theme.Panel;
    r.FillRect(new RectF(b.X + 1, b.Y + 1, b.Width, b.Height), p.Shadow);
    r.FillRect(b, p.Border);
    r.FillRect(new RectF(b.X + 1, b.Y + 1, b.Width - 2, b.Height - 2), ui.Theme.TextShadow);
    r.FillRect(new RectF(b.X + 2, b.Y + 2, b.Width - 4, b.Height - 4), p.Fill);
}

/// <summary>The speaker's tag: a plain plate in the accent colour.</summary>
private static void Tag(IRenderer r, in UiContext ui, in ControlState s) => r.FillRect(s.Bounds, ui.Theme.Accent);

/// <summary>The arrow under the text: it blinks once the page is all shown, and keeps its room while it is not.</summary>
private void Arrow(IRenderer r, in UiContext ui, in ControlState s)
{
    if (Text.Reveal!.Complete && (int)(ui.Time * 3) % 2 == 0)
        ui.Font.Draw(r, s.Text, s.Bounds.Position, ui.Theme.Accent);
}
```

A control with behaviour of its own (a slider, a toggle) is still a widget of the game's
([Controls of your own](#controls-of-your-own)).

### Controls of your own

Sliders, toggles and value selectors are not engine types: their behaviour fits in a small widget, and their look is
the game's. The settings screen of the [focused examples](../examples/focused.md#settings-screen) builds all three.
This slider steps with left and right while it has the focus, follows a press and a drag, and steps with the wheel:

<!-- code: samples/Examples/Examples/SettingsExample.cs#slider -->
```csharp
/// <summary>
/// A value from 0 to 1. Left and right step it while it has the focus; a press on it and a drag set it from the pointer's
/// position (the slider holds the press, so the drag may leave it); the wheel steps it while it has the focus.
/// </summary>
public sealed class Slider(float value = 0, float step = 0.1f) : Widget
{
    private float _value = Math.Clamp(value, 0, 1);

    /// <summary>The value, 0..1.</summary>
    public float Value
    {
        get => _value;
        set
        {
            float v = Math.Clamp(value, 0, 1);
            if (v == _value) return;
            _value = v;
            Changed?.Invoke(v);
        }
    }

    public float Step { get; set; } = step;
    public event Action<float>? Changed;
    public override bool Focusable => true;

    protected override Vector2 MeasureContent(UiContext context) => new(80 * context.Unit, context.Font.LineHeight + 4 * context.Unit);

    protected override bool OnAction(UiAction action) => action switch
    {
        UiAction.Left => Nudge(-1),
        UiAction.Right => Nudge(1),
        _ => false,
    };

    protected override void OnPointerDown(Vector2 position) => Follow(position);
    protected override void OnPointerMove(Vector2 position) => Follow(position);
    protected override bool OnWheel(Vector2 delta) => IsFocused && delta.Y != 0 && Nudge(MathF.Sign(delta.Y));

    private void Follow(Vector2 position) => Value = (position.X - Bounds.X) / Bounds.Width;

    private bool Nudge(int steps)
    {
        Value = MathF.Round(Value / Step + steps) * Step;   // on the step grid, however the value was set
        return true;
    }

    protected override void DrawContent(IRenderer r, UiContext context)
    {
        var theme = context.Theme;
        float u = context.Unit, mid = MathF.Round(Bounds.Y + Bounds.Height / 2);
        r.FillRect(new RectF(Bounds.X, mid - u, Bounds.Width, 2 * u), theme.Muted);
        r.FillRect(new RectF(Bounds.X, mid - u, MathF.Round(Bounds.Width * Value), 2 * u), theme.Accent);
        float x = MathF.Round(Bounds.X + Bounds.Width * Value);
        r.FillRect(new RectF(x - 2 * u, Bounds.Y + u, 4 * u, Bounds.Height - 2 * u), IsFocused ? theme.Accent : theme.Text);
    }

    protected override void HashState(StateHasher hash) => hash.Add(_value);
}
```

The screen puts each control beside a label. Focus moves to the nearest widget in a direction, centre to centre, so
controls of one width in one column keep up and down going straight from row to row:

<!-- code: samples/Examples/Examples/SettingsExample.cs#screen -->
```csharp
public override void Enter()
{
    var music = new Slider(Audio.MusicVolume);
    music.Changed += value => Audio.MusicVolume = value;
    var effects = new Slider(Audio.SfxVolume);
    effects.Changed += value =>
    {
        Audio.SfxVolume = value;
        Audio.TryPlay("select");                       // hear the new level, if the game has the sound
    };
    _hints = new Toggle(on: true);
    _difficulty = new Choice("EASY", "NORMAL", "HARD") { Index = 1 };

    Widgets.Content = new Panel(new Stack()
        .Add(Row("MUSIC", music))
        .Add(Row("EFFECTS", effects))
        .Add(Row("HINTS", _hints))
        .Add(Row("DIFFICULTY", _difficulty))
        .Add(new Button("BACK", () => Game.Scenes.Pop())));
    Widgets.Cancelled += () => Game.Scenes.Pop();
}

/// <summary>
/// A label and a control, both of fixed width. Focus moves to the nearest widget in a direction, centre to centre, so
/// controls of one width in one column make up and down go straight from row to row.
/// </summary>
private static Widget Row(string text, Widget control)
{
    control.Width = 80;
    return new Stack { Orientation = Orientation.Horizontal, Align = Align.Center }
        .Add(new Label(text) { Width = 64 })
        .Add(control);
}
```

## Limitations

- Text uses the root's font in one colour per label: there is no rich text. A text field edits one line, and its
  caret moves by characters, never by words.
- A scene has one widget root, in one space: the UI view or the canvas. Widgets on the canvas are drawn by the
  scene's own `Draw`, not by the engine.
- A text reveal shows whole characters at one rate, with optional pauses; it has no effect per character (a wave, a
  colour), and letters of a script that joins them (Arabic) take their joined shape as their neighbours appear.
- A `MenuCursor` reads presses, not held keys: a held direction moves once. It does not read the pointer by itself;
  the scene maps its rows through `MoveTo`.
- Skins draw buttons and panels. A text field, a scroll bar and a label keep the theme's colours.
- Sliders, toggles and value selectors are widgets of your own
  ([Controls of your own](#controls-of-your-own)).
- A scroll view moves at once to its new position: there is no smooth scrolling or momentum after a flick.
- Opacity fades each thing a widget draws, not the widget as one picture: where its own draws overlap (a panel's border
  under its fill), the lower one shows through.
