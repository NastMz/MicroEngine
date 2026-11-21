using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Localization;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace Examples;

/// <summary>
/// Localization: the texts of a small screen in English, Spanish, Russian and Arabic, from the string tables in
/// <c>Content/text</c>. Confirm changes the language of everything on screen at once: labels and the button follow it
/// by themselves, the count takes each language's plural forms, the hint names keys in the language, Arabic reads
/// from the right in the fonts its table names, and the arrow has a variant that points the other way. The choice is
/// saved with the game's settings.
/// </summary>
public sealed class LocalizationExample : Scene
{
    private readonly Dictionary<string, ShapedFont> _fonts = [];
    private int _gems, _gemsShown = -1, _hintOf = -1;

    #region localization
    // A widget made with a key shows that key's text in the language in use; the others get theirs below.
    public Label Title { get; } = new(new LocalizedText("loc.title"));
    public Label Language { get; } = new();
    public Label Gems { get; } = new();
    public Label Hint { get; } = new() { Wrap = true, Width = 200 };
    public Button Next { get; private set; } = null!;

    public override void Enter()
    {
        var text = Game.Localization;
        text.Load("text");                                                    // Content/text/<language>.strings.json
        text.TrySetLanguage(GameSaves.LoadSettings(Game.Saves).Language);     // the saved choice, else the system's language
        text.Changed += UseFonts;
        UseFonts();

        Next = new Button(new LocalizedText("loc.next"), NextLanguage);
        Widgets.Content = new Panel(new Stack { Width = 200 }.Add(Title).Add(Language).Add(Gems).Add(Hint).Add(Next));
    }

    private void NextLanguage()
    {
        var text = Game.Localization;
        int at = 0;
        while (text.Languages[at].Code != text.Language) at++;
        text.Language = text.Languages[(at + 1) % text.Languages.Count].Code;

        var settings = GameSaves.LoadSettings(Game.Saves);
        settings.Language = text.Language;
        GameSaves.SaveSettings(Game.Saves, settings);
    }

    /// <summary>The fonts the language's table names: the first face, then those that fill in the letters it lacks.</summary>
    private void UseFonts()
    {
        var language = Game.Localization.Current;
        if (!_fonts.TryGetValue(language.Code, out var font))
        {
            font = ShapedFont.FromBytes(Game.Graphics, Game.Assets.Bytes(language.Fonts[0]));
            foreach (var more in language.Fonts.Skip(1)) font.AddFallback(Game.Assets.Bytes(more));
            _fonts[language.Code] = font;
        }
        Widgets.Font = font.At(12);
        Language.Localized = new("loc.language", ("name", language.Name));
    }

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
        if (Input.Pressed("action")) _gems++;

        // A value that changes is given again: the plural form is chosen for the number, in the language in use.
        if (_gems != _gemsShown) Gems.Localized = new("loc.gems", ("count", _gemsShown = _gems));

        // Text written by hand: a value of the game's own ({keys}), then the control that presses {action}. Both change
        // with the language and with the device in the player's hands.
        var text = Game.Localization;
        int version = HashCode.Combine(text.Version, Game.Prompts.Version);
        if (version == _hintOf) return;
        _hintOf = version;
        Hint.Text = Game.Prompts.Format(text.Format("loc.hint", ("keys", Game.Prompts.TextAll("confirm"))));
    }

    /// <summary>The image for the language in use: <c>next.ar.png</c> in Arabic, <c>next.png</c> in the others.</summary>
    public string ImagePath => Game.Localization.Path("localization/next.png");

    public override void DrawUi(IRenderer ui)
    {
        var frame = Game.Views.CanvasToUi(Vector2.Zero);
        var end = Game.Views.CanvasToUi(new Vector2(Game.Settings.VirtualWidth, Game.Settings.VirtualHeight));
        var arrow = Game.Assets.Texture(ImagePath);
        ui.Draw(arrow, new Vector2(MathF.Round((frame.X + end.X - arrow.Width) / 2), end.Y - arrow.Height - 6));

        // A hint whose control is a glyph (a key cap, a pad button), drawn with the language's font; the pieces of a
        // right-to-left line follow each other from the right.
        var font = Widgets.Font!;
        PromptText.DrawCentered(ui, font, Game.Prompts, Game.Localization["loc.glyph"], (frame.X + end.X) / 2,
            end.Y - arrow.Height - 12 - font.LineHeight, UiTheme.Default.Text, rightToLeft: Game.Localization.RightToLeft);
    }
    #endregion

    public override void Exit()
    {
        Game.Localization.Changed -= UseFonts;
        // The other examples are written in English.
        Game.Localization.TrySetLanguage("en");
        foreach (var font in _fonts.Values) font.Dispose();
        _fonts.Clear();
    }

    public override void Draw(IRenderer r) => Heading.Draw(r, Font, "LOCALIZATION");
}
