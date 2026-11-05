using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;
using MicroEngine.UI;
using TrailDuel.Rules;
using TrailDuel.Ui;
using TrailDuel.Visuals;

namespace TrailDuel.Scenes;

/// <summary>Title screen: a sunny hilltop with the three starters, the logo, and Continue / New Game.</summary>
internal sealed class TitleScene(TrailDuelGame game) : Scene
{
    private CachedText _pressText;
    /// <summary>The title menu, built on entering (Continue only with a save).</summary>
    private SelectionList _list = null!;
    private bool _menu;
    private bool _confirmOverwrite;
    private ChoiceMenu? _confirm;

    public override void Enter()
    {
        Audio.PlayMusic("title", 0.3f);
        string[] options = ["NEW GAME", "CONTROLS", "ACHIEVEMENTS", "QUIT"];
        _list = UiKit.List(Audio, game.HasSave ? ["CONTINUE", .. options] : options);
    }

    public override void Resume() => Audio.PlayMusic("title");

    /// <summary>State hash: whether the menu is open, its options and cursor, and the overwrite question. Left out: the prompt text cache.</summary>
    public override void HashState(StateHasher hash)
    {
        hash.Add(_menu);
        hash.Add(_list.Options.Count);
        foreach (var option in _list.Options) hash.Add(option);
        hash.Add(_list.Index);
        hash.Add(_confirmOverwrite);
        hash.Add(_confirm);
    }

    public override void Update(float dt)
    {
        if (!_menu)
        {
            if (Input.Pressed("confirm") || Input.Pressed("pause")) { _menu = true; Audio.Play("confirm"); }
            return;
        }
        if (_confirm is not null)
        {
            _confirm.Update(dt, Input);
            if (_confirm.Result is { } r)
            {
                _confirm = null;
                _confirmOverwrite = false;
                if (r == 0) game.StartNewGame();
            }
            return;
        }
        int? chosen = _list.Update(Input);
        if (Input.Pressed("cancel")) { _menu = false; Audio.Play("cancel", 0.6f); return; }
        if (chosen is not { } index) return;
        Audio.Play("confirm");
        switch (_list.Options[index])
        {
            case "CONTINUE": game.ContinueGame(); break;
            case "NEW GAME":
                if (game.HasSave) { _confirmOverwrite = true; _confirm = new ChoiceMenu(Font, Audio, "START OVER", "CANCEL"); }
                else game.StartNewGame();
                break;
            case "QUIT": Game.Quit(); break;
            case "ACHIEVEMENTS":
                var list = new AchievementsScene(game.Achievements, UiKit.Theme);
                list.Moved += () => Audio.Play("move", 0.6f);
                list.Closed += () => Audio.Play("cancel", 0.6f);
                Game.Scenes.Push(list);
                break;
            default: Game.Scenes.Push(new ControlsScene()); break;
        }
    }

    public override void Draw(IRenderer r)
    {
        // The painted meadow (shared with meadow battles), a soft sun and drifting clouds.
        r.FillCircle(new Vector2(262, 34), 18, Color.Hex("fff0b0").WithAlpha(0.5f));
        r.Draw(game.Sprite("backdrop:title"), Vector2.Zero);
        r.FillCircle(new Vector2(262, 34), 13, Color.Hex("fff6d0").WithAlpha(0.9f));
        for (int i = 0; i < 4; i++)
            r.Draw(game.Sprite(i % 2 == 0 ? "cloud" : "cloud2"), new Vector2(MathF.Round((i * 110 + Age * (5 + i * 2)) % 440 - 60), 14 + i * 13 % 40), Pal.White.WithAlpha(0.9f));
        for (int x = 4; x < 320; x += 11) r.Draw(game.Sprite("t:tallover"), new Vector2(x, 162 + (x * 7) % 9));

        // Starters on the hill, each bobbing on its own beat.
        string[] starters = SpeciesDex.Starters;
        for (int i = 0; i < starters.Length; i++)
        {
            // The middle starter stands closer to the viewer so the menu never hides it.
            float x = 100 + i * 60, ground = i == 1 ? 168 : 152, hop = MathF.Max(0, MathF.Sin(Age * 2.4f + i * 2.1f)) * 5;
            r.FillEllipse(new Vector2(x, ground - 1), 11, 2.5f, Pal.Ink.WithAlpha(0.2f));
            r.Draw(game.Sprite(SpriteKeys.Front[starters[i]]), new Vector2(x, MathF.Round(ground - hop)));
        }

        // Logo
        float logoY = 30 + MathF.Round(MathX.Wave(Age, 0.4f, 2));
        UiKit.Title(r, Font, "TRAIL DUEL", new Vector2(161, logoY + 2), 4, Pal.InkSoft);
        UiKit.Title(r, Font, "TRAIL DUEL", new Vector2(160, logoY), 4, Pal.Gold);
        r.FillRect(new RectF(96, logoY + 18, 128, 2), Pal.Coral);
        UiKit.Title(r, Font, "a creature trail adventure", new Vector2(160, logoY + 28), 1, Pal.Cream);

        if (!_menu)
        {
            if ((int)(Age * 2) % 2 == 0) UiKit.Title(r, Font, _pressText.Get(Game.Prompts.Version, Game.Prompts, static p => p.Format("PRESS {confirm} TO START").ToUpperInvariant()), new Vector2(160, 98));
        }
        else
        {
            var options = _list.Options;
            var box = new RectF(112, 66, 96, options.Count * 12 + 10);
            UiKit.Box(r, box);
            for (int i = 0; i < options.Count; i++)
            {
                float y = box.Y + 6 + i * 12;
                UiKit.Text(r, Font, options[i], box.X + 18, y);
                if (i == _list.Index) UiKit.Cursor(r, Font, box.X + 7, y, Age);
            }
            if (_confirmOverwrite)
            {
                var warn = new RectF(40, 132, 240, 22);
                UiKit.Box(r, warn);
                UiKit.Text(r, Font, "Your saved trail will be lost!", warn.X + 10, warn.Y + 7, Pal.CoralDark);
                _confirm?.Draw(r, new Vector2(300, 130));
            }
        }
        Font.DrawShadowed(r, "made with MicroEngine", new Vector2(4, 170), Pal.Cream, Pal.Ink);
    }
}
