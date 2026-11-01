# Text

MicroEngine draws text with one built-in pixel font, `BitmapFont`, made for small virtual screens: every game has it as
`Game.Font` (and `Font` inside a scene) without loading anything. This page covers drawing, measuring and wrapping text,
the characters the font has, and what it does not do.

## When to use it

For everything a pixel-art game writes on screen: titles, menus, HUDs, dialogue. Its glyphs are about 5x7 pixels, so at
the default 320x180 canvas a line holds around 50 characters. For larger text, draw it scaled by whole pixels. For a
different typeface, see [Limits](#limits).

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
not split: it stays on its own line and overflows. The [UI widgets](ui.md) (`DialogueBox`, `Typewriter`) wrap and page
text for you.

## Characters

The font is proportional (narrow `i`, wide `m`) and covers:

- printable ASCII: `A-Z`, `a-z`, `0-9`, space and ``!"#$%&'()*+,-./:;<=>?@[\]^_`{|}~``;
- symbols games use: `♥ ▶ ▼ ★ ♪ → ← ↑ ↓ × © ·`.

Any other character (accented letters, `ñ`, other alphabets, emoji) is drawn as `?`. Games written in languages that
need other letters must replace them (`é` with `e`) or draw their own glyphs (below).

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

- One typeface. There is no API to load TTF/OTF fonts or bitmap font files in 0.4.0-alpha.2.
- No rich text (colour or style changes inside a string), no kerning, no right-to-left or complex scripts, no text
  input widget.
- To use another pixel font, draw its glyphs as sprites yourself: put a glyph sheet in an atlas with `AddGrid`, map
  characters to frame names, and draw them one by one, advancing by each glyph's width.

## Common mistakes

- **Text that blurs or wobbles.** It is drawn at a fractional position with the camera; draw HUD text after
  `PopCamera`, and keep `scale` a whole number.
- **`?` instead of letters.** The character is not in the font (see [Characters](#characters)).
- **Text cut at the screen edge.** Measure it and wrap or move it; nothing clips text for you except
  `IRenderer.SetClip`.

## Related

- [Tutorial step 9: Text and HUD](../tutorial/09-hud.md)
- [UI widgets](ui.md): dialogue boxes, typewriter text, menus
- API: [`BitmapFont`](../api/MicroEngine.Graphics.BitmapFont.md)
