using System.Numerics;
using CourtyardKey.Model;
using CourtyardKey.UI;
using CourtyardKey.Visuals;
using CourtyardKey.World;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace CourtyardKey.Scenes;

/// <summary>Shared dusk backdrop: gradient sky, drifting clouds, hills and the ruined courtyard.</summary>
public static class Backdrop
{
    public static void Draw(IRenderer r, Sprites art, float time, float sunset = 0)
    {
        var top = Color.Lerp(Color.Hex("2a2050"), Color.Hex("1a1a3a"), sunset);
        var mid = Color.Lerp(Color.Hex("8a4a7a"), Color.Hex("e08a6a"), sunset);
        r.FillGradient(new RectF(0, 0, r.Width, 70), top, mid, 14);
        r.FillGradient(new RectF(0, 70, r.Width, 50), mid, Color.Lerp(Color.Hex("f0b070"), Color.Hex("ffd890"), sunset), 10);
        // Sun low on the horizon
        r.FillCircle(new Vector2(232, 100), 16, Color.Hex("ffd890").WithAlpha(0.6f));
        r.FillCircle(new Vector2(232, 100), 11, Color.Hex("fff0c0"));
        for (int i = 0; i < 3; i++)
        {
            float x = (i * 130 + time * (5 + i * 3)) % 400 - 60;
            r.Draw(art[$"cloud{i}"], new Vector2(MathF.Round(x), 18 + i * 17));
        }
        r.Draw(art["hills_far"], new Vector2(0, 76));
        r.Draw(art["ruins"], new Vector2(80, 84));
        r.Draw(art["hills_near"], new Vector2(0, 120));
        r.FillRect(new RectF(0, 178, r.Width, 2), Pal.Leaf[0]);
    }
}

/// <summary>The title: animated backdrop, the emblem, and New Game / Continue / Controls / Quit.</summary>
public sealed class TitleScene : Scene
{
    private readonly CourtyardKeyGame _game;
    private readonly SelectionList _menu = new("New Game", "Continue", "Controls", "Achievements", "Quit");
    private readonly ParticleSystem _motes = new(200, 21) { Additive = true };
    private bool _showControls;
    private float _spawn;

    public TitleScene(CourtyardKeyGame game)
    {
        _game = game;
        _menu.Moved += () => Audio.Play("menu_move");
    }

    public override void Enter()
    {
        Audio.PlayMusic("title", 0.6f);
        bool hasSave = _game.Saves.HasSave;
        _menu.SetEnabled(1, hasSave);
        _menu.Index = hasSave ? 1 : 0;
    }

    public override void Update(float dt)
    {
        _motes.Update(dt);
        if ((_spawn -= dt) <= 0)
        {
            _spawn = 0.15f;
            _motes.Emit(new Vector2(_game.Random.Next(0, 320), _game.Random.Next(110, 180)), Fx.Firefly);
        }
        if (_showControls)
        {
            if (Input.Pressed("confirm") || Input.Pressed("cancel")) { _showControls = false; Audio.Play("menu_back"); }
            return;
        }
        if (Age < 0.4f || Game.Scenes.InTransition) return;
        if (_menu.Update(Input) is not { } choice) return;
        Audio.Play("menu_ok");
        switch (choice)
        {
            case 0: Start(new Progress()); break;
            case 1: Start(_game.Saves.Load() ?? new Progress()); break;
            case 2: _showControls = true; break;
            case 3:
                var list = new AchievementsScene(_game.Achievements, UiStyle.Theme);
                list.Moved += () => Audio.Play("menu_move");
                list.Closed += () => Audio.Play("menu_back");
                Game.Scenes.Push(list);
                break;
            default: Game.Quit(); break;
        }
    }

    private void Start(Progress progress)
    {
        Audio.StopMusic(0.6f);
        Game.Scenes.Replace(new PlayScene(_game, progress), Transition.Iris(new Vector2(160, 70), 1.0f));
    }

    public override void Draw(IRenderer r)
    {
        Backdrop.Draw(r, _game.Art, Age);
        _motes.Draw(r);
        // Emblem with a breathing glow
        var center = new Vector2(160, 46);
        r.SetBlendMode(BlendMode.Additive);
        r.DrawPivoted(_game.Art["glow_l"], center, 0, new Vector2(1.3f + MathF.Sin(Age * 2) * 0.08f), Pal.Spirit[2].WithAlpha(0.35f));
        r.SetBlendMode(BlendMode.Alpha);
        r.DrawPivoted(_game.Art["emblem"], center + new Vector2(0, MathF.Round(MathX.Wave(Age, 0.4f, 2))));
        Font.DrawOutlined(r, "COURTYARD KEY", new Vector2(160 - Font.MeasureWidth("COURTYARD KEY") * 1.5f, 76), Pal.Gold[3], Pal.Ink, 3);
        r.FillRect(new RectF(108, 99, 104, 11), Pal.Ink.WithAlpha(0.45f));
        Font.DrawCentered(r, "a tale of Willowmere", new Vector2(160, 104), Pal.Cream, 1, Pal.Ink);

        if (_showControls) { DrawControls(r); return; }
        var panel = new RectF(110, 113, 92, 64);
        UiStyle.Frame(r, panel);
        _menu.Draw(r, Font, new Vector2(panel.X + 8, panel.Y + 6), UiStyle.Theme, Age, 11);
        const string footer = "made with MicroEngine";
        Font.DrawShadowed(r, footer, new Vector2(r.Width - 4 - Font.MeasureWidth(footer), 170), Pal.Leaf[4], Pal.Ink);
    }

    private void DrawControls(IRenderer r)
    {
        var panel = new RectF(56, 112, 208, 64);
        UiStyle.Frame(r, panel);
        // On a touch screen the on-screen controls are explained instead of the keys.
        (string Key, string What)[] rows = Game.Touch.Shown
            ?
            [
                ("Stick", "Move; push less, go slow"),
                ("A", "Sword, talk, read, open"),
                ("C", "Throw the boomerang"),
                ("II", "Pause, map, inventory"),
            ]
            :
            [
                ("Arrows/WASD", "Move"),
                ("Z / Space", "Sword, talk, read, open"),
                ("C / X", "Throw the boomerang"),
                ("Esc / P", "Pause, map and inventory"),
            ];
        for (int i = 0; i < rows.Length; i++)
        {
            UiStyle.KeyCap(r, Font, rows[i].Key, new Vector2(panel.X + 10, panel.Y + 8 + i * 13));
            Font.DrawShadowed(r, rows[i].What, new Vector2(panel.X + 82, panel.Y + 9 + i * 13), Pal.White, Pal.Ink);
        }
    }
}

/// <summary>The ending: the key returns, the bells go quiet, then credits and your journey's stats.</summary>
public sealed class EndingScene : Scene
{
    private readonly CourtyardKeyGame _game;
    private readonly Progress _progress;
    private readonly ParticleSystem _motes = new(300, 31) { Additive = true };

    private static readonly string[] Story =
    [
        "Wren carried the Courtyard Key home to Willowmere.",
        "Its light settled the river, and the old bells fell silent.",
        "Beneath the ruins, the Warden sleeps once more...",
    ];

    private static readonly string[] Credits =
    [
        "COURTYARD KEY",
        "",
        "Design, code, pixel art and music",
        "authored in code with MicroEngine",
        "",
        "Every sprite drawn procedurally",
        "Every sound synthesized live",
        "",
        "Thank you for playing!",
    ];

    public EndingScene(CourtyardKeyGame game, Progress progress)
    {
        _game = game;
        _progress = progress;
    }

    public override void Enter() => Audio.PlayMusic("ending", 0.4f);

    private float CreditsStart => 13f;

    public override void Update(float dt)
    {
        _motes.Update(dt);
        if (_game.Random.NextDouble() < dt * 8)
            _motes.Emit(new Vector2(_game.Random.Next(0, 320), 180), Fx.Sparkle with { Gravity = -30, Life = (2f, 4f) });
        if (Age > CreditsStart + 14 && (Input.Pressed("confirm") || Input.Pressed("pause")))
        {
            Audio.StopMusic(0.8f);
            Game.Scenes.Reset(new TitleScene(_game), Transition.Fade(1f));
        }
    }

    public override void Draw(IRenderer r)
    {
        Backdrop.Draw(r, _game.Art, Age, sunset: MathF.Min(1, Age / 12f));
        _motes.Draw(r);
        if (Age < CreditsStart)
        {
            int page = Math.Min(Story.Length - 1, (int)(Age / 4.2f));
            float local = Age - page * 4.2f;
            float a = Math.Clamp(MathF.Min(local, 4.2f - local) * 1.5f, 0, 1);
            r.FillRect(new RectF(0, 136, r.Width, 26), Pal.Ink.WithAlpha(0.55f * a));
            Font.DrawCentered(r, Story[page], new Vector2(160, 149), Pal.White.WithAlpha(a), 1, Pal.Ink.WithAlpha(a));
            r.DrawPivoted(_game.Art["courtyard_key"], new Vector2(160, 52 + MathF.Round(MathX.Wave(Age, 0.5f, 3))), 0, new Vector2(2));
            return;
        }
        float t = Age - CreditsStart;
        r.FillRect(new RectF(60, 0, 200, r.Height), Pal.Ink.WithAlpha(MathF.Min(0.7f, t * 0.4f)));
        for (int i = 0; i < Credits.Length; i++)
        {
            float y = 190 + i * 14 - t * 16;
            float clampedY = MathF.Max(y, 20 + i * 14);
            var color = i == 0 ? Pal.Gold[3] : Pal.Cream;
            Font.DrawCentered(r, Credits[i], new Vector2(160, clampedY), color, i == 0 ? 2 : 1, Pal.Ink);
        }
        if (t > 9)
        {
            float a = MathF.Min(1, (t - 9) / 1.5f);
            var time = TimeSpan.FromSeconds(_progress.PlaySeconds);
            Font.DrawCentered(r, $"Time {(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}  ·  Gems {_progress.Gems}  ·  Falls {_progress.Deaths}", new Vector2(160, 150), Pal.Gold[2].WithAlpha(a), 1, Pal.Ink);
            if (t > 14 && (int)(Age * 2) % 2 == 0) Font.DrawCentered(r, "THE END  ·  press Z", new Vector2(160, 166), Pal.White, 1, Pal.Ink);
        }
    }
}
