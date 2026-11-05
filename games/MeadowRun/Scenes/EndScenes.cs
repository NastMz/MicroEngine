using System.Numerics;
using MeadowRun.Art;
using MeadowRun.Gameplay;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace MeadowRun.Scenes;

/// <summary>Game over: offers a continue (lives refill, score resets, unlocks kept) or the title.</summary>
internal sealed class GameOverScene(MeadowGame game) : Scene
{
    private readonly SelectionList _menu = new("Continue", "Title screen");
    private bool _leaving;

    /// <summary>Hashes the menu cursor and whether the scene is leaving.</summary>
    public override void HashState(StateHasher hash)
    {
        hash.Add(_menu.Index);
        hash.Add(_leaving);
    }

    public override void Enter()
    {
        _menu.Moved += () => game.Sfx("menu_move", 0.7f);
        game.Music("gameover");
    }

    public override void Update(float dt)
    {
        if (_leaving || Age < 1.2f) return;
        if (_menu.Update(Input) is not { } choice) return;
        _leaving = true;
        game.Sfx("menu_select");
        game.Session.Continue();
        Game.Scenes.Replace(choice == 0 ? new MapScene(game, -1, Math.Max(0, game.Save.Unlocked - 1)) : new TitleScene(game), Transition.Fade(0.8f, Pal.Ink));
    }

    public override void Draw(IRenderer r)
    {
        r.FillGradient(new RectF(0, 0, 320, 180), Pal.Ink, Pal.Plum, 12);
        float drop = Ease.OutBounce(Math.Clamp(Age / 1.0f, 0, 1));
        Ui.Centered(r, Font, "GAME OVER", 160, MathX.Lerp(-20, 50, drop), Pal.Coral, 3);
        var pip = game.Art.Pip(PipForm.Sprout, "hurt")[0];
        r.Draw(pip, new Vector2(160, 112));
        Ui.Centered(r, Font, "The meadow still waits for you.", 160, 118, Pal.Haze);
        if (Age > 1.2f) Ui.Menu(r, Font, _menu, 160, 136, Age, 13, 104);
    }
}

/// <summary>The ending: the meadow restored, credits rolling over it, then back to the title.</summary>
internal sealed class CreditsScene(MeadowGame game) : Scene
{
    private static readonly (string Text, Color Color, float Scale)[] Lines =
    [
        ("THE SUNSTONE SHINES AGAIN", Pal.Butter, 1),
        ("", Pal.Cream, 1),
        ("Grumbold the Mole King has fled", Pal.Cream, 1),
        ("and the meadow is in bloom.", Pal.Cream, 1),
        ("", Pal.Cream, 1),
        ("", Pal.Cream, 1),
        ("MEADOW RUN", Pal.Gold, 2),
        ("", Pal.Cream, 1),
        ("Game design, levels & code", Pal.Haze, 1),
        ("written for the MicroEngine samples", Pal.Cream, 1),
        ("", Pal.Cream, 1),
        ("Pixel art", Pal.Haze, 1),
        ("painted procedurally at load time", Pal.Cream, 1),
        ("", Pal.Cream, 1),
        ("Music & sound", Pal.Haze, 1),
        ("synthesized note by note", Pal.Cream, 1),
        ("", Pal.Cream, 1),
        ("Starring", Pal.Haze, 1),
        ("Pip the sprout spirit", Pal.Lime, 1),
        ("Bumblets, Thornbacks, Hopkins", Pal.Cream, 1),
        ("Mothwings, Spitpods", Pal.Cream, 1),
        ("and Grumbold, the Cinder Mole King", Pal.Coral, 1),
        ("", Pal.Cream, 1),
        ("", Pal.Cream, 1),
        ("THANK YOU FOR PLAYING!", Pal.Butter, 1),
    ];

    private const float Speed = 14f;
    private bool _leaving;

    public override void Enter()
    {
        game.Music("credits");
        _ = Objects;   // the scene's world, created here because drawing may not create it: frames animate at its blend (DrawTime)
    }

    /// <summary>Hashes whether the credits are leaving; the scroll follows the scene's age, which the engine hashes.</summary>
    public override void HashState(StateHasher hash) => hash.Add(_leaving);

    private float EndTime => (Lines.Length * 14 + 220) / Speed;

    public override void Update(float dt)
    {
        if (_leaving) return;
        if (Age > EndTime || (Age > 2 && Input.Pressed("confirm")))
        {
            _leaving = true;
            game.Sfx("menu_select");
            Game.Scenes.Reset(new TitleScene(game), Transition.Fade(1.2f, Pal.Ink));
        }
    }

    public override void Draw(IRenderer r)
    {
        float t = DrawTime.Of(this);
        Diorama.Draw(r, game.Art, t, t * 12);
        // The Sunstone hangs in the sky, radiating.
        var sun = game.Art.Frames("sunstone")[(int)(t * 6) % 4];
        r.SetBlendMode(BlendMode.Additive);
        r.Draw(game.Art["glow"], new Vector2(250, 40), Pal.Gold.WithAlpha(0.8f));
        r.SetBlendMode(BlendMode.Alpha);
        r.Draw(sun, new Vector2(250, 40 + MathF.Round(MathX.Wave(t, 0.4f, 2))));
        var pip = game.Art.Pip(game.Session.Form, "victory");
        r.Draw(pip[(int)(t * 3) % pip.Count], new Vector2(64, 164));
        r.FillRect(new RectF(40, 0, 240, 180), Pal.Ink.WithAlpha(0.35f));
        float y = 180 - t * Speed;
        foreach (var (text, color, scale) in Lines)
        {
            if (y > -20 && y < 190 && text.Length > 0) Ui.Centered(r, Font, text, 160, MathF.Round(y), color, scale);
            y += 14 * scale;
        }
        float scoreY = MathF.Max(80, y + 30);
        if (scoreY < 190)
        {
            Ui.Centered(r, Font, "FINAL SCORE", 160, scoreY, Pal.Haze);
            Ui.Centered(r, Font, Ui.N(game.Session.Score, 7), 160, scoreY + 12, Pal.Butter, 2);
        }
    }
}
