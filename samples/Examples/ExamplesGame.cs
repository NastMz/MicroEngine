using System.Numerics;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace Examples;

/// <summary>
/// A list of small examples, one screen each. Every example is an ordinary scene pushed over the list; Esc, P or the
/// gamepad's Start (the "pause" action) pops it and returns here.
/// </summary>
public sealed class ExamplesGame(string? saveDirectory = null) : Game
{
    /// <summary>Name and factory of every example, in the order of the list.</summary>
    public static readonly (string Name, Func<ExamplesGame, Scene> Create)[] All =
    [
        ("Shapes and textures", g => new ShapesExample()),
        ("Sprites from files", g => new SpritesExample()),
        ("Animation", g => new AnimationExample()),
        ("Gamepad and analog input", g => new GamepadExample()),
        ("Touch controls", g => new TouchExample()),
        ("Sound and music", g => new SoundExample()),
        ("Text", g => new TextExample()),
        ("Saving", g => new SaveExample(g.SaveDirectory)),
        ("Camera", g => new CameraExample()),
        ("Platformer: tiles and collision", g => new PlatformerExample()),
        ("Particles and light", g => new ParticlesExample()),
        ("Tweens and coroutines", g => new TweensExample()),
        ("Dialogue", g => new DialogueExample()),
        ("Custom input source", g => new CustomSourceExample()),
        ("Controls and rebinding", g => new RebindExample(g.SaveDirectory)),
        ("Settings screen (widgets)", g => new SettingsExample()),
        ("Long list (scrolling)", g => new LongListExample()),
    ];

    public string? SaveDirectory { get; } = saveDirectory;

    public override GameSettings Settings { get; } = new("MicroEngine Examples") { Background = Color.Hex("1a1c2c") };

    protected override void Load() => Scenes.Push(new MenuScene());
}

/// <summary>The list of examples.</summary>
public sealed class MenuScene : Scene
{
    private readonly SelectionList _menu = new([.. ExamplesGame.All.Select(e => e.Name)]);

    public override void Update(float dt)
    {
        if (_menu.Update(Input) is { } choice)
            Game.Scenes.Push(ExamplesGame.All[choice].Create((ExamplesGame)Game), new CurtainTransition());
    }

    public override void Draw(IRenderer r)
    {
        Font.DrawShadowed(r, "MICROENGINE EXAMPLES", new Vector2(8, 4), UiTheme.Default.Accent, Color.Black);
        _menu.Draw(r, Font, new Vector2(16, 18), UiTheme.Default, Age, lineHeight: 9);
    }
}

/// <summary>The heading every example draws.</summary>
public static class Heading
{
    public static void Draw(IRenderer r, BitmapFont font, string title, string hint = "")
    {
        r.FillRect(new RectF(0, 0, r.Width, 12), Color.Black.WithAlpha(0.6f));
        font.Draw(r, title, new Vector2(4, 3), UiTheme.Default.Accent);
        var back = hint.Length > 0 ? $"{hint}   ESC: BACK" : "ESC: BACK";
        font.Draw(r, back, new Vector2(r.Width - 4 - font.MeasureWidth(back), 3), UiTheme.Default.Muted);
    }
}
