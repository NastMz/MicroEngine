using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace Examples;

/// <summary>
/// A pixel-art menu and a dialogue box made of widgets on the game's 320x180 canvas: every pixel of them is a canvas
/// pixel, whatever the window's size. The rows and the boxes are ordinary buttons and panels drawn by skins of the
/// example's own (a bar, an underline and a cursor that bobs; a box with a double border), and the dialogue reveals its
/// text with a <see cref="TextReveal"/>. Keyboard, gamepad, mouse and touch all go through the same buttons.
/// </summary>
public sealed class PixelMenuExample : Scene
{
    private static readonly Color Ink = Color.Hex("1a1c2c"), Night = Color.Hex("29366f"), Cream = Color.Hex("f4f4f4"),
        Gold = Color.Hex("ffcd75"), Dusk = Color.Hex("566c86"), Sky = Color.Hex("41a6f6");

    private static readonly string[] Pages =
    [
        "The gems only grow at night, where the moonlight touches the soil of the old garden behind the well.",
        "Take the lantern. Be back before sunrise!",
    ];

    private int _page;
    private Widget? _hovered;

    /// <summary>The menu's box, for tests.</summary>
    public Panel Menu { get; private set; } = null!;
    /// <summary>The menu's rows, top to bottom, for tests.</summary>
    public Button[] Rows { get; private set; } = [];
    /// <summary>The dialogue: the name tag over the box, for tests.</summary>
    public Stack Dialogue { get; private set; } = null!;
    /// <summary>The dialogue's text, for tests.</summary>
    public Label Text { get; private set; } = null!;
    /// <summary>The arrow that goes on with the dialogue, for tests.</summary>
    public Button More { get; private set; } = null!;
    /// <summary>How many letters made a sound, for tests.</summary>
    public int Blips { get; private set; }

    #region canvas
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
    #endregion

    #region skins
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
    #endregion

    #region dialogue
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
    #endregion

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
        // The cursor follows the mouse: the row the pointer reaches takes the focus.
        var hovered = Widgets.Hovered;
        if (!ReferenceEquals(hovered, _hovered) && hovered is Button { IsEnabledInTree: true }) Widgets.Focus(hovered);
        _hovered = hovered;
    }

    /// <summary>A night sky in blocks of one canvas pixel, so the size of a pixel shows in the window.</summary>
    private static void Backdrop(IRenderer r)
    {
        r.FillRect(new RectF(0, 0, r.Width, r.Height), Ink);
        for (int i = 0; i < 60; i++)
            r.FillRect(new RectF(i * 37 % r.Width, 14 + i * 53 % (r.Height - 14), 1, 1), i % 3 == 0 ? Cream : Dusk);
        for (int x = 0; x < r.Width; x += 2) r.FillRect(new RectF(x, r.Height - 3, 1, 3), Night);
    }
}
