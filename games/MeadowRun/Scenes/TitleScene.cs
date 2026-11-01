using System.Numerics;
using MeadowRun.Art;
using MeadowRun.Content;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace MeadowRun.Scenes;

/// <summary>Draws a scrolling meadow diorama shared by the title and credits screens.</summary>
internal static class Diorama
{
    public static void Draw(IRenderer r, Assets art, float time, float scroll)
    {
        r.Draw(art["sky_meadow"], Vector2.Zero);
        foreach (var layer in BackdropArt.Layers(Biome.Meadow))
        {
            var s = art[layer.Sprite];
            float x = -((scroll * layer.FactorX + time * layer.Drift) % s.Width);
            float y = layer.Bottom - s.Height - 6;
            for (float px = MathF.Round(x); px < 320; px += s.Width) r.Draw(s, new Vector2(px, y));
        }
        // A strip of real meadow ground with tufts and flowers.
        float gx = -(scroll % 16);
        for (int i = -1; i < 22; i++)
        {
            float x = MathF.Round(gx + i * 16);
            int world = i + (int)(scroll / 16);
            r.Draw(art.Ground(Biome.Meadow, 2 | 8 | 4, (int)(MicroEngine.Graphics.Art.Hash(world, 1, 3) * 3)), new Vector2(x, 164));
            r.Draw(art.Ground(Biome.Meadow, 1 | 2 | 8 | 4, 1), new Vector2(x, 180));
            float h = MicroEngine.Graphics.Art.Hash(world, 2, 9);
            var decos = DecoArt.For(Biome.Meadow);
            if (h < 0.6f) r.Draw(art[decos[(int)(MicroEngine.Graphics.Art.Hash(world, 7, 4) * decos.Length)]], new Vector2(x + 8, 164));
        }
    }
}

/// <summary>Title screen: animated logo, living diorama and the main menu.</summary>
internal sealed class TitleScene(MeadowGame game) : Scene
{
    private readonly SelectionList _menu = new("Start", "Continue", "Controls", "Achievements", "Quit");
    private bool _controls, _leaving;
    private float _logoDrop;

    public override void Enter()
    {
        _menu.SetEnabled(1, game.HasSave && (game.Save.Unlocked > 1 || game.Save.Finished));
        _menu.Index = _menu.IsEnabled(1) ? 1 : 0;
        _menu.Moved += () => game.Sfx("menu_move", 0.7f);
        game.Music("title");
        Tweens.Add(1.1f, t => _logoDrop = t, Ease.OutBounce);
    }

    public override void Update(float dt)
    {
        if (_leaving) return;
        if (_controls)
        {
            if (Input.Pressed("confirm") || Input.Pressed("cancel")) { _controls = false; game.Sfx("menu_back", 0.7f); }
            return;
        }
        if (Age < 0.3f) return;
        int? choice = _menu.Update(Input);
        if (choice is null && Input.Pressed("confirm") && !_menu.IsEnabled(_menu.Index)) game.Sfx("thud", 0.6f);
        switch (choice)
        {
            case 0:
                game.Sfx("menu_select");
                game.NewGame();
                Go(new MapScene(game, -1, 0));
                break;
            case 1:
                game.Sfx("menu_select");
                game.ContinueGame();
                Go(new MapScene(game, -1, Math.Max(0, game.Save.Unlocked - 1)));
                break;
            case 2:
                game.Sfx("menu_select", 0.8f);
                _controls = true;
                break;
            case 3:
                game.Sfx("menu_select", 0.8f);
                var list = new AchievementsScene(game.Achievements, Ui.Theme) { Title = "ACHIEVEMENTS" };
                list.Moved += () => game.Sfx("menu_move", 0.7f);
                list.Closed += () => game.Sfx("menu_back", 0.7f);
                Game.Scenes.Push(list);
                break;
            case 4:
                game.Quit();
                break;
        }
    }

    private void Go(Scene next)
    {
        _leaving = true;
        Game.Scenes.Replace(next, Transition.Diamonds(0.8f, Pal.Ink));
    }

    public override void Draw(IRenderer r)
    {
        Diorama.Draw(r, game.Art, Age, Age * 30);
        var logo = game.Art["logo"];
        float y = MathX.Lerp(-60, 22, _logoDrop) + MathF.Round(MathX.Wave(Age, 0.4f, 1.5f));
        r.Draw(logo, new Vector2(MathF.Round(160 - logo.Width / 2f), MathF.Round(y)));
        Ui.Centered(r, Font, "~ a quest for the Sunstone ~", 160, y + logo.Height + 2, Pal.Cream);

        // Pip trots along the meadow while the world scrolls.
        var run = game.Art.Pip(PipForm.Sprout, "run");
        r.Draw(run[(int)(Age * 11) % run.Length], new Vector2(64, 164 + MathF.Round(MathF.Abs(MathX.Wave(Age, 2.75f, 1.2f)) * -1)));
        var coin = game.Art.Frames("coin");
        for (int i = 0; i < 4; i++)
        {
            float cx = 330 - ((Age * 30 + i * 90) % 380);
            r.Draw(coin[(int)(Age * 8 + i) % coin.Length], new Vector2(MathF.Round(cx), 140 + MathF.Round(MathX.Wave(Age + i, 1, 3))));
        }

        if (_controls) { DrawControls(r); return; }
        r.Panel(new RectF(112, 92, 96, 70), Ui.Card);
        Ui.Menu(r, Font, _menu, 160, 98, Age, 12, 88);
        Ui.Centered(r, Font, "(c) 2026 · an original game built on MicroEngine", 160, 171, Pal.Cream.WithAlpha(0.8f));
    }

    private void DrawControls(IRenderer r)
    {
        r.FillRect(new RectF(0, 0, 320, 180), Pal.Ink.WithAlpha(0.5f));
        var box = new RectF(48, 30, 224, 124);
        r.Panel(box, Ui.Card);
        Ui.Centered(r, Font, "CONTROLS", 160, 37, Pal.Butter);
        // On a touch screen the on-screen controls are explained instead of the keys.
        bool touch = Game.Touch.Shown;
        (string Key, string What)[] rows = touch
            ?
            [
                ("D-pad ← →", "Walk"),
                ("A", "Jump (hold for higher)"),
                ("B", "Run  ·  throw seeds"),
                ("D-pad ↓ + A", "Drop through planks"),
                ("B, roll to A", "Jump while running"),
                ("II", "Pause"),
            ]
            :
            [
                ("← →  /  A D", "Walk"),
                ("Z  Space  J", "Jump (hold for higher)"),
                ("X  C  Shift", "Run  ·  throw seeds"),
                ("↓ + Jump", "Drop through planks"),
                ("Esc  P", "Pause"),
                ("F11", "Fullscreen"),
            ];
        for (int i = 0; i < rows.Length; i++)
        {
            Ui.Text(r, Font, rows[i].Key, new Vector2(60, 54 + i * 13), Pal.Gold);
            Ui.Text(r, Font, rows[i].What, new Vector2(144, 54 + i * 13));
        }
        Ui.Centered(r, Font, touch ? "Keyboards and gamepads work too" : "Gamepad: A jump · X/B run · Start pause", 160, 136, Pal.Haze);
    }
}
