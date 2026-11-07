using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace Examples;

/// <summary>
/// A dialogue box built from widgets: a panel with a name tag over it, pages revealed letter by letter with a blip and
/// advanced with confirm, then a yes/no answer with two buttons. A page longer than the box continues on the next
/// confirm, so no text is cut. Nothing here is a dialogue type of the engine: the look and the rules are the game's.
/// </summary>
public sealed class DialogueExample : Scene
{
    private const float TextWidth = 280;
    private const int LinesPerBox = 3;
    private const float LettersPerSecond = 45;

    private readonly Queue<(string Speaker, string Text)> _pages = new();
    private readonly Queue<string> _chunks = new();
    private float _shown;
    private Label _more = null!;

    /// <summary>The dialogue: the name tag over the box, for tests.</summary>
    public Stack Dialogue { get; private set; } = null!;
    /// <summary>The speaker's name in the tag, for tests.</summary>
    public Label Speaker { get; private set; } = null!;
    /// <summary>The text of the box, for tests.</summary>
    public Label Text { get; private set; } = null!;
    /// <summary>The yes and no buttons, for tests.</summary>
    public Stack Answers { get; private set; } = null!;
    /// <summary>What the gardener says to the answer, for tests.</summary>
    public Label Result { get; private set; } = null!;

    #region build
    public override void Enter()
    {
        var theme = UiTheme.Default;
        Speaker = new Label { Color = theme.Panel.Fill, Shadow = false };
        // Three lines high whatever the box holds, so the box never jumps.
        Text = new Label { Width = TextWidth, Height = LinesPerBox * Font.LineHeight + (LinesPerBox - 1) };
        _more = new Label("▼") { Color = theme.Accent, Shadow = false };
        Dialogue = new Stack { Spacing = 0, Align = Align.Start }
            .Add(new Panel(Speaker) { Padding = 3, Style = theme.Panel with { Fill = theme.Accent, Highlight = default } })
            .Add(new Panel(new Stack { Spacing = 0, Align = Align.End }.Add(Text).Add(_more)));
        Answers = new Stack { Orientation = Orientation.Horizontal, Visible = false }
            .Add(new Button("YES", () => Answer("THANK YOU!")))
            .Add(new Button("NO", () => Answer("OH... MAYBE TOMORROW.")));
        Result = new Label { Color = theme.Accent };
        Widgets.Content = new Layer()
            .Add(Dialogue, Anchor.Bottom, new Vector2(0, -4))
            .Add(Answers, Anchor.Center, new Vector2(0, -20))
            .Add(Result, Anchor.Center, new Vector2(0, 10));

        Say("GARDENER", "Welcome! The gems only grow at night, and only where the moonlight touches the soil of the " +
            "old garden behind the well. My lantern broke last winter, so I cannot find them on my own any more.",
            "Will you help me collect them before sunrise?");
    }
    #endregion

    /// <summary>Queues pages; the first one shows at once if the box was closed.</summary>
    private void Say(string speaker, params string[] pages)
    {
        foreach (var page in pages) _pages.Enqueue((speaker, page));
        if (Text.Text.Length == 0) NextPage();
    }

    #region pages
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
        Text.Text = _chunks.Dequeue();
        Text.VisibleCharacters = 0;
        _shown = 0;
    }
    #endregion

    #region reveal
    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
        if (!Dialogue.Visible) return;                                // the buttons take confirm themselves
        int before = Text.VisibleCharacters ?? Text.Text.Length;
        _shown = MathF.Min(Text.Text.Length, _shown + LettersPerSecond * dt);
        Text.VisibleCharacters = (int)_shown;
        for (int i = before; i < (int)_shown; i++)
            if (!char.IsWhiteSpace(Text.Text[i])) Audio.TryPlay("select", 0.3f);   // a blip per letter, if the game has one
        bool complete = (int)_shown >= Text.Text.Length;
        _more.Opacity = complete && (int)(Age * 3) % 2 == 0 ? 1 : 0;    // a blinking arrow that keeps its room: confirm goes on
        if (!Input.Pressed("confirm")) return;
        if (!complete) _shown = Text.Text.Length;                     // the first confirm shows the rest
        else if (_chunks.Count > 0) NextBox();                        // then the rest of a long page
        else NextPage();                                              // then the next page
        Text.VisibleCharacters = (int)_shown;
    }
    #endregion

    private void Answer(string reply)
    {
        Answers.Visible = false;
        Result.Text = reply;
    }

    public override void Draw(IRenderer r) => Heading.Draw(r, Font, "DIALOGUE");
}
