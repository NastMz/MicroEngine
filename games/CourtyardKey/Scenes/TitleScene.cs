using System.Numerics;
using CourtyardKey.Model;
using CourtyardKey.UI;
using CourtyardKey.Visuals;
using CourtyardKey.World;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Kernel;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace CourtyardKey.Scenes;

/// <summary>Shared dusk backdrop: gradient sky, drifting clouds, hills and the ruined courtyard.</summary>
public static class Backdrop
{
    private static readonly string[] Clouds = ["cloud0", "cloud1", "cloud2"];

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
            r.Draw(art[Clouds[i]], new Vector2(MathF.Round(x), 18 + i * 17));
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
    private readonly DemoKit.OptionList _menu = new("New Game", "Continue", "Controls", "Achievements", "Quit");
    private readonly ParticleSystem _motes = new(200, 21) { Additive = true };
    private bool _showControls;
    private float _spawn;
    private (string Key, string What)[]? _controlRows;
    private int _controlRowsVersion;

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

    /// <summary>
    /// State hash: the menu cursor, whether the controls panel shows, and the mote spawn timer (each spawn draws from
    /// the game's generator). The motes themselves and the control rows' text are drawing state.
    /// </summary>
    public override void HashState(StateHasher hash)
    {
        hash.Add(_menu.Index);
        hash.Add(_showControls);
        hash.Add(_spawn);
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
                var list = new DemoKit.AchievementList(_game.Achievements, UiStyle.Theme);
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
        // The controls of the device being played: keys, a gamepad's buttons, or the on-screen controls.
        var prompts = Game.Prompts;
        // Built again only when the prompts may have changed (another device or pad family).
        if (_controlRows is null || prompts.Version != _controlRowsVersion)
        {
            bool touch = prompts.Device == InputDevice.Touch;
            _controlRows =
            [
                (prompts.Directions(separator: " / "), touch ? "Move; push less, go slow" : "Move"),
                (prompts.TextAll("confirm"), "Sword, talk, read, open"),
                (prompts.TextAll("action"), "Throw the boomerang"),
                (prompts.TextAll("pause"), "Pause, map and inventory"),
            ];
            _controlRowsVersion = prompts.Version;
        }
        var rows = _controlRows;
        // Columns sized from the text, so long key names never run into their descriptions or out of the panel.
        int keys = 0, texts = 0;
        foreach (var row in rows)
        {
            keys = Math.Max(keys, UiStyle.KeyCapWidth(Font, row.Key));
            texts = Math.Max(texts, Font.MeasureWidth(row.What));
        }
        float width = MathF.Min(r.Width - 8, 10 + keys + 8 + texts + 10);
        var panel = new RectF(MathF.Round((r.Width - width) / 2), 112, width, 64);
        UiStyle.Frame(r, panel);
        for (int i = 0; i < rows.Length; i++)
        {
            UiStyle.KeyCap(r, Font, rows[i].Key, new Vector2(panel.X + 10, panel.Y + 8 + i * 13));
            Font.DrawShadowed(r, rows[i].What, new Vector2(panel.X + 10 + keys + 8, panel.Y + 9 + i * 13), Pal.White, Pal.Ink);
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
        "CC0 art: ArMM1998, Pixel-boy",
        "Every sound synthesized live",
        "",
        "Thank you for playing!",
    ];

    private string? _stats;
    private (double, int, int) _statsOf;
    private CachedPrompt? _theEnd;

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
            var shown = (_progress.PlaySeconds, _progress.Gems, _progress.Deaths);
            if (_stats is null || shown != _statsOf)
            {
                var time = TimeSpan.FromSeconds(_progress.PlaySeconds);
                _stats = $"Time {(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}  ·  Gems {_progress.Gems}  ·  Falls {_progress.Deaths}";
                _statsOf = shown;
            }
            Font.DrawCentered(r, _stats, new Vector2(160, 150), Pal.Gold[2].WithAlpha(a), 1, Pal.Ink);
            _theEnd ??= new CachedPrompt(Game.Prompts, "THE END  ·  press {confirm}");
            if (t > 14 && (int)(Age * 2) % 2 == 0) Font.DrawCentered(r, _theEnd.Text, new Vector2(160, 166), Pal.White, 1, Pal.Ink);
        }
    }
}
