# Text

MicroEngine draws text with a built-in pixel font, `BitmapFont`, made for small virtual screens: every game has it as
`Game.Font` (and `Font` inside a scene) without loading anything. Games can also load TrueType and OpenType fonts and draw them at any size
([Scalable fonts](#scalable-fonts-ttf-and-otf)). This page covers drawing, measuring and wrapping text, the characters
the built-in font has, and what text does not do.

## When to use it

The built-in font suits everything a pixel-art game writes on screen: titles, menus, HUDs, dialogue. Its glyphs are
about 5x7 pixels, so at the default 320x180 canvas a line holds around 50 characters. For larger text, draw it scaled by
whole pixels. For an HD game, another typeface, accented letters or other alphabets, use a
[scalable font](#scalable-fonts-ttf-and-otf).

## Drawing

<!-- code: samples/Examples/Examples/TextExample.cs#text -->
```csharp
Font.Draw(r, "Plain text, upper and lower case: 0123456789 !?.,:;()+-*/", new Vector2(8, 20), Color.White);
Font.DrawShadowed(r, "Shadowed text reads on any background", new Vector2(8, 32), Color.Hex("ffcd75"), Color.Black);
Font.DrawOutlined(r, "Outlined", new Vector2(8, 44), Color.White, Color.Hex("b13e53"));
Font.Draw(r, "x2", new Vector2(70, 40), Color.Hex("a7f070"), scale: 2);
Font.DrawCentered(r, "CENTRED ON A POINT", new Vector2(240, 48), Color.Hex("73eff7"), shadow: Color.Black);

// Measure: the size of a string, for boxes and right alignment.
var score = $"SCORE {Game.UpdateCount}";
Font.Draw(r, score, new Vector2(312 - Font.MeasureWidth(score), 62), Color.White);

// Wrap: lines that fit a width; LineHeight spaces them.
var box = new RectF(8, 78, 180, 60);
r.Panel(box, MicroEngine.UI.UiTheme.Default.Panel);
var lines = Font.Wrap(Story, (int)box.Width - 12);
for (int i = 0; i < lines.Count; i++)
    Font.Draw(r, lines[i], new Vector2(box.X + 6, box.Y + 6 + i * (Font.LineHeight + 1)), Color.White);
```

| Method | Draws |
| --- | --- |
| `Draw(r, text, position, color, scale = 1)` | text whose top-left corner is at `position` |
| `DrawShadowed(r, text, position, color, shadow, scale = 1)` | the text over a copy offset by one (scaled) pixel down and right |
| `DrawOutlined(r, text, position, color, outline, scale = 1)` | the text over a one-pixel outline in eight directions |
| `DrawCentered(r, text, center, color, scale = 1, shadow = null)` | text centred on a point, optionally shadowed |

Positions are virtual pixels, like everything the renderer draws. Inside `PushCamera`/`PopCamera` text is in world
coordinates (a name over a character); outside, in screen coordinates (a HUD). `scale` multiplies the glyphs' size:
use whole numbers (2, 3) to keep pixels square. `\n` starts a new line, `LineHeight` pixels lower.

## Measuring and wrapping

| Member | Gives |
| --- | --- |
| `MeasureWidth(text)` | the width in pixels at scale 1 (the widest line) |
| `Measure(text, scale)` | width and height |
| `Wrap(text, maxWidth)` | the lines that fit `maxWidth` pixels, broken at spaces |
| `LineHeight`, `CapHeight`, `Spacing`, `Advance(c)` | the font's metrics |

Right-aligning a score: `new Vector2(Game.Width - 6 - Font.MeasureWidth(text), 4)`. A word longer than `maxWidth` is
broken between characters, so no line is wider than `maxWidth`. A `Label` with `Wrap` wraps to its width for you, and `VisibleCharacters` reveals it ([UI widgets](ui.md#text-that-wraps-and-reveals)).

## Characters

The font is proportional (narrow `i`, wide `m`) and covers:

- printable ASCII: `A-Z`, `a-z`, `0-9`, space and ``!"#$%&'()*+,-./:;<=>?@[\]^_`{|}~``;
- symbols games use: `♥ ▶ ▼ ★ ♪ → ← ↑ ↓ × © ·`, and `○ □ △` for gamepad prompts.

Any other character (accented letters, `ñ`, other alphabets, emoji) is drawn as `?`. Games written in languages that
need other letters must replace them (`é` with `e`) or draw their own glyphs (below).

## Scalable fonts (TTF and OTF)

`Game.Assets.Font(path)` loads a TrueType or
OpenType font (`.ttf`, or `.otf` with TrueType or CFF outlines) as a `ScalableFont`, which draws at any size in pixels
per em. Put the file under `Content/` with its licence ([Assets](assets.md#credits-convention)).

```csharp
protected override void Load()
{
    Ui = Assets.Font("fonts/Inter-Regular.ttf");
    Title = Assets.Font("fonts/Inter-Bold.otf");
    Ui.Prepare(ScalableFont.Ascii, 18, 28);       // rasterize now the sizes the game uses
    Title.Prepare(ScalableFont.Ascii, 64);
}

public override void Draw(IRenderer r)
{
    game.Title.Draw(r, "Paused", new Vector2(40, 28), 64, Color.White);
    var body = game.Ui.At(28);                   // the font at one size: an IFont
    body.DrawShadowed(r, "Resume", new Vector2(44, 120), Color.White, Color.Black, new Vector2(2));
    foreach (var line in body.Wrap(story, 600)) { /* ... */ }
}
```

| Member | Does |
| --- | --- |
| `Draw(r, text, position, size, color)` | draws text whose top-left is at `position` |
| `Measure(text, size)`, `LineHeight(size)` | the text's size and the distance between lines, in canvas pixels |
| `At(size)` | the font at one size as a `SizedFont`, an `IFont`; the same object for the same size |
| `Prepare(characters, sizes...)` | rasterizes those characters now, so drawing them later does no font work |
| `AddFallback(bytes)` | another face for characters this one lacks (another alphabet, symbols) |
| `Filter`, `Pages`, `TextureBytes`, `GlyphsRasterized`, `Reset()` | sampling, glyph-page memory and work, freeing the pages |

Glyphs are rasterized the first time they are drawn at a size and kept in shared glyph pages (512×512 textures, 1 MB
each, added as they fill up); every later draw is one quad per glyph, like a sprite. Rasterizing a glyph takes a
fraction of a millisecond, so call `Prepare` while loading for the characters and sizes a scene shows, and text costs no
font work during play. Kerning is applied and missing characters draw as `?`.

`FontOptions` chooses how outlines become pixels: the default is antialiased, with each glyph placed on whole canvas
pixels so unscaled text stays sharp, and samples glyph pages with the game's default filter (linear for high-resolution
games, nearest for pixel art). `FontOptions.Pixel` gives hard edges and nearest sampling, for pixel fonts distributed as
TTF and drawn at their design size or whole multiples of it, on a pixel-art canvas. Text drawn in a scene's `DrawUi`
has the window's resolution ([The world view and the UI view](rendering.md#the-world-view-and-the-ui-view)).

`BitmapFont` and a `ScalableFont` at a size both implement `IFont` (`LineHeight`, `Measure`, `Draw`), with `Wrap`,
`MeasureWidth`, `DrawCentered` and `DrawShadowed` for any `IFont`. The [UI widgets](ui.md) take any `IFont`
(`Widgets.Font`) and scale their margins with its line height, so the same widget serves both styles.

## Shaped text: Arabic, Hebrew and Indic scripts

Some scripts cannot be drawn one character at a time. Arabic letters take a different form depending on their
neighbours and join into words, Indic scripts merge consonants into conjuncts and place vowel signs around them, and
Arabic and Hebrew run right to left. The optional `MicroEngine.Text.Shaping` package draws them: `ShapedFont` is a
TTF/OTF font whose text is shaped by HarfBuzz, the shaping engine browsers and operating systems use. Add the package
next to the engine:

```xml
<PackageReference Include="MicroEngine.Text.Shaping" Version="0.5.0-alpha.2" />
```

It is a package of its own because HarfBuzz is a native library: games that only write Latin text do not carry it.
It runs on every platform the engine does, browsers included, and `microengine package` adds HarfBuzz's licence to the
packages of games that use it.

`ShapedFont` opens like a scalable font (`FromBytes`, `Load`) and gives an `IFont` at a size with `At`, so widgets,
`Wrap` and `Measure` work with it unchanged. Fallback faces fill in the letters the faces before them lack, a run of
text at a time, and fallback letters are sized to the main face's em. This example draws English, Arabic and Hebrew from
Inter with Noto Sans Arabic and Noto Sans Hebrew after it, lines that mix both directions, each line aligned to the side
it starts from, and the same font in a wrapping label:

<!-- code: samples/Examples/Examples/ShapingExample.cs#shaping -->
```csharp
public override void Enter()
{
    // Latin from Inter; the letters it lacks from Noto Sans Arabic, then Noto Sans Hebrew.
    _font = ShapedFont.FromBytes(Game.Graphics, Game.Assets.Bytes("fonts/Inter-Regular.ttf"), name: "Inter")
        .AddFallback(Game.Assets.Bytes("fonts/NotoSansArabic-Regular.ttf"), "Noto Sans Arabic")
        .AddFallback(Game.Assets.Bytes("fonts/NotoSansHebrew-Regular.ttf"), "Noto Sans Hebrew");
    Text = _font.At(14);
    // An IFont like any other: widgets measure, wrap and draw with it.
    Widgets.Font = _font.At(12);
    var label = new Label("مرحبا بك في المثال. هذه الجملة تلتف داخل اللوحة.") { Wrap = true, Width = 140 };
    Widgets.Content = new Layer().Add(new Panel(label), Anchor.Bottom, new Vector2(0, -12));
}

public override void DrawUi(IRenderer ui)
{
    // Inside the game's frame (the UI view also covers any letterbox around it).
    var frame = Game.Views.CanvasToUi(Vector2.Zero);
    var end = Game.Views.CanvasToUi(new Vector2(Game.Settings.VirtualWidth, Game.Settings.VirtualHeight));
    float left = frame.X + 16, right = end.X - 16, y = frame.Y + 20;
    foreach (var line in Lines)
    {
        // A line that starts with a right-to-left letter is aligned to the right edge.
        float x = Text.IsRightToLeft(line) ? right - Text.MeasureWidth(line) : left;
        Text.Draw(ui, line, new Vector2(x, y), Color.White);
        y += Text.LineHeight + 2;
    }
}
```

| Member | What it gives |
| --- | --- |
| `ShapedFont.FromBytes(device, bytes)`, `Load(device, path)` | a font from a TTF or OTF file, with optional `FontOptions` |
| `AddFallback(bytes)` | another face for the letters the faces before it lack |
| `At(size)` | a `ShapedFontSize`: `LineHeight`, `Baseline`, `Measure`, `Draw`, `Prepare`, and `IsRightToLeft(line)` for alignment |
| `ShapedFontSize.Shape(line)` | the glyphs of a line as drawn, left to right: face, glyph, the text index it shows (`Cluster`), position and advance, for effects of a game's own |
| `Faces`, `Filter`, `Pages`, `TextureBytes`, `GlyphsRasterized`, `Reset()` | as for scalable fonts |

Lines that mix directions are laid out by Unicode's bidirectional algorithm, as browsers lay them out: a line reads in
the direction of its first letter that has one, and the runs of the other direction inside it read in theirs. An
English name or a number inside an Arabic sentence reads left to right in its place, a Hebrew phrase inside an English
line right to left, and digits keep their order inside a right-to-left line. Text is stored in reading order (the order
it is typed and translated in); drawing turns it into the order on screen. Align right-to-left lines yourself:
`IsRightToLeft(line)` says which lines start from the right, as the example uses; labels and text fields align to the
left. Glyphs are cached per size as for scalable fonts, and shaped lines are kept, so drawing the same text every frame
costs no shaping work.

## Performance

The font is one small texture created at start-up; each visible glyph is one `IRenderer.Draw` call, like a sprite.
A few hundred characters per frame cost little. `DrawOutlined` draws the text nine times and `DrawShadowed`
twice; keep them for short strings. `Wrap` and `Measure` allocate: in a dialogue box, wrap a page once when it opens,
not every frame.

## On every platform

Text is pixels drawn by the engine, not by the system: it looks the same on SDL, raylib, in browsers and in
screenshots, and needs no font installed. Touch controls are the exception: their labels are drawn by the backend's own
small font (SDL's 8x8 debug font, raylib's default font).

## Limits

- The built-in font has one typeface; other typefaces come from [TTF/OTF files](#scalable-fonts-ttf-and-otf) (next
  SDK version). Bitmap font files (AngelCode BMFont) are not read.
- No rich text (colour or style changes inside a string) and no colour emoji. Right-to-left and complex scripts need
  the optional shaping package ([Shaped text](#shaped-text-arabic-hebrew-and-indic-scripts)). Its bidirectional layout
  ignores explicit direction marks (the embedding, override and isolate controls) and does not pair brackets across
  directions, and text fields move their caret left to right whatever the script.
- Scalable text is rasterized at each size it is drawn: text drawn at many sizes (a zooming title) fills glyph pages;
  draw it at one size and scale the drawing, or `Reset` the font when a scene ends.
- No signed-distance-field text: very large or freely zoomed text is rasterized per size, not scaled from one bitmap.
- Font files are trusted content shipped with the game: truncated or foreign files are refused, but fonts from players
  or the network should not be loaded.

## Common mistakes

- **Text that blurs or wobbles.** It is drawn at a fractional position with the camera; draw HUD text after
  `PopCamera`, and keep `scale` a whole number.
- **`?` instead of letters.** The character is not in the font (see [Characters](#characters)).
- **Text cut at the screen edge.** Measure it and wrap or move it; nothing clips text for you except
  `IRenderer.SetClip`.

## Related

- [Tutorial step 9: Text and HUD](../tutorial/09-hud.md)
- [UI widgets](ui.md): dialogue boxes, typewriter text, menus
- [Assets](assets.md): loading font files
- API: [`BitmapFont`](../api/MicroEngine.Graphics.BitmapFont.md)
