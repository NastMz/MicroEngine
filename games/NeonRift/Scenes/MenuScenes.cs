using System.Numerics;
using NeonRift.Content;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace NeonRift.Scenes;

/// <summary>Title: drifting skyline, neon logo, start/controls/quit.</summary>
internal sealed class TitleScene(RiftGame game) : Scene
{
    private readonly SelectionList _menu = new("START", "CONTROLS", "ACHIEVEMENTS", "QUIT");
    private bool _controls, _leaving;
    private DrawBlend _blend;

    public override void Enter()
    {
        _menu.Moved += () => game.Sfx("menu-move", 0.6f);
        game.Music("title");
    }

    public override void Update(float dt)
    {
        _blend.Mark(Game);
        if (_leaving) return;
        if (_controls)
        {
            if (Input.Pressed("confirm") || Input.Pressed("cancel") || Input.Pressed("fire")) { _controls = false; game.Sfx("menu-move", 0.6f); }
            return;
        }
        if (_menu.Update(Input) is not { } choice) return;
        game.Sfx("menu-select", 0.8f);
        switch (choice)
        {
            case 0:
                _leaving = true;
                game.NewGame();
                Game.Scenes.Replace(new StageCardScene(game, 0), Transition.Fade(0.8f));
                break;
            case 1: _controls = true; break;
            case 2:
                var list = new AchievementsScene(game.Achievements, Ui.Theme) { CloseActions = ["cancel", "pause", "confirm", "fire"] };
                list.Moved += () => game.Sfx("menu-move", 0.6f);
                list.Closed += () => game.Sfx("menu-move", 0.6f);
                Game.Scenes.Push(list);
                break;
            default: Game.Quit(); break;
        }
    }

    public override void Draw(IRenderer r)
    {
        var view = new StageView(game.Art, Font);
        view.DrawBackdrop(r, "city", new Vector2(_blend.Age(this) * 40, 0), Age, false);
        r.FillGradient(new RectF(0, 150, r.Width, 120), Ui.Ink.WithAlpha(0f), Ui.Ink.WithAlpha(0.9f), 8);
        r.Draw(game.Art.Frames("hero_idle")[(int)(Age * 6) % 4], new Vector2(70, 250));
        r.Draw(game.Art.Frames("trooper_idle")[(int)(Age * 5) % 3], new Vector2(410, 250));
        Ui.Neon(r, Font, "NEON RIFT", r.Width / 2f, 48, 5, Ui.Magenta, Age);
        Ui.Centered(r, Font, "A RUN & GUN THROUGH HALCYON CITY", r.Width / 2f, 96, Ui.Cyan);
        if (_controls)
        {
            r.Panel(new RectF(110, 118, 260, 120), new PanelStyle(Ui.Ink.WithAlpha(0.92f), Ui.Magenta, Ui.Magenta.WithAlpha(0.4f)));
            // The controls of the device being played: keys, a gamepad's buttons, or the on-screen controls.
            var p = Game.Prompts;
            bool touch = p.Device == InputDevice.Touch;
            string[] keys = touch
                ?
                [
                    "STICK, 8 DIRECTIONS",
                    p.TextAll("jump"),
                    $"{p.TextAll("fire")} (HOLD), AIM WITH STICK",
                    $"SLIDE THUMB FROM {p.Text("fire")}",
                    "STICK DOWN ON THE GROUND",
                    $"STICK DOWN + {p.Text("jump")}",
                    $"{p.TextAll("lock")} (TAP ON, TAP OFF)",
                    p.TextAll("pause"),
                ]
                :
                [
                    p.Directions(separator: " / "),
                    p.TextAll("jump"),
                    $"{p.TextAll("fire")}  (HOLD)",
                    "UP, DIAGONALS, DOWN IN AIR",
                    "DOWN ON THE GROUND",
                    p.Format("{down} + {jump} ON A LEDGE"),
                    p.TextAll("lock"),
                    p.TextAll("pause"),
                ];
            string[] actions = touch
                ? ["MOVE / AIM", "JUMP", "FIRE", "FIRE + JUMP", "KNEEL", "DROP DOWN", "PLANT FEET", "PAUSE"]
                : ["MOVE", "JUMP", "FIRE", "AIM", "KNEEL", "DROP DOWN", "PLANT FEET", "PAUSE"];
            var lines = actions.Zip(keys, (action, key) => (Action: action, Keys: key.ToUpperInvariant())).ToArray();
            for (int i = 0; i < lines.Length; i++)
            {
                Ui.Label(r, Font, lines[i].Action, new Vector2(124, 128 + i * 13), Ui.Cyan);
                Ui.Label(r, Font, lines[i].Keys, new Vector2(204, 128 + i * 13));
            }
        }
        else
        {
            Ui.Menu(r, Font, _menu, r.Width / 2f, 150, Age);
            Ui.Centered(r, Font, "HI " + Ui.N(Math.Max(game.Session.HighScore, 30000), 7), r.Width / 2f, 220, Ui.Muted);
        }
        Ui.Centered(r, Font, "ART & AUDIO: ANSIMUZ, JUHANI JUNKALA (CC0)  -  ENGINE: MICROENGINE", r.Width / 2f, 256, Ui.Muted);
        Ui.Scanlines(r);
    }
}

/// <summary>Stage introduction card with the hero running in place.</summary>
internal sealed class StageCardScene(RiftGame game, int stageIndex) : Scene
{
    private bool _leaving;

    public override void Enter() => game.Music("");

    public override void Update(float dt)
    {
        if (_leaving || Age < 2.8f && !(Age > 0.6f && Input.Pressed("confirm"))) return;
        _leaving = true;
        Game.Scenes.Replace(new PlayScene(game, stageIndex), Transition.Fade(0.7f));
    }

    public override void Draw(IRenderer r)
    {
        var def = Stages.All[stageIndex];
        r.FillRect(new RectF(0, 0, r.Width, r.Height), Ui.Ink);
        Ui.Centered(r, Font, $"STAGE {stageIndex + 1}", r.Width / 2f, 70, Ui.Cyan, 2);
        Ui.Neon(r, Font, def.Name, r.Width / 2f, 96, 3, Ui.Magenta, Age);
        Ui.Centered(r, Font, def.Subtitle, r.Width / 2f, 128, Ui.Muted);
        r.Draw(game.Art.Frames("hero_run")[(int)(Age * 12) % 8], new Vector2(r.Width / 2f, 214));
        r.FillRect(new RectF(r.Width / 2f - 60, 214, 120, 1), Ui.Magenta.WithAlpha(0.6f));
        Ui.Centered(r, Font, $"REST {game.Session.Lives}   SCORE {Ui.N(game.Session.Score, 7)}", r.Width / 2f, 232, Ui.Text);
        Ui.Scanlines(r);
    }
}

/// <summary>Game over: continue from the last checkpoint while continues remain, otherwise back to the title.</summary>
internal sealed class GameOverScene(RiftGame game, int stageIndex, int checkpoint = 0) : Scene
{
    private readonly SelectionList _menu = new("CONTINUE", "QUIT");
    private bool _leaving;

    public override void Enter()
    {
        _menu.SetEnabled(0, game.Session.Continues > 0);
        if (game.Session.Continues <= 0) _menu.Index = 1;
        _menu.Moved += () => game.Sfx("menu-move", 0.6f);
        game.Music("");
    }

    public override void Update(float dt)
    {
        if (_leaving || Age < 1f || _menu.Update(Input) is not { } choice) return;
        game.Sfx("menu-select", 0.8f);
        _leaving = true;
        if (choice == 0 && game.Session.UseContinue())
            Game.Scenes.Replace(new PlayScene(game, stageIndex, checkpoint), Transition.Fade(0.7f));
        else
            Game.Scenes.Reset(new TitleScene(game), Transition.Fade(0.8f));
    }

    public override void Draw(IRenderer r)
    {
        r.FillRect(new RectF(0, 0, r.Width, r.Height), Ui.Ink);
        Ui.Neon(r, Font, "GAME OVER", r.Width / 2f, 80, 4, Color.Hex("ff3050"), Age);
        Ui.Centered(r, Font, $"SCORE {Ui.N(game.Session.Score, 7)}", r.Width / 2f, 130, Ui.Text);
        Ui.Centered(r, Font, $"CONTINUES LEFT {game.Session.Continues}", r.Width / 2f, 146, Ui.Muted);
        Ui.Menu(r, Font, _menu, r.Width / 2f, 180, Age);
        Ui.Scanlines(r);
    }
}

/// <summary>Ending: the Leviathan falls, the city skyline scrolls by with the credits.</summary>
internal sealed class EndingScene(RiftGame game) : Scene
{
    private static readonly string[] Credits =
    [
        "MISSION COMPLETE",
        "",
        "THE LEVIATHAN IS DOWN.",
        "HALCYON'S LIGHTS GO DARK ONE BY ONE,",
        "AND VELA WALKS HOME AT DAWN.",
        "",
        "- CREDITS -",
        "",
        "GAME DESIGN & CODE",
        "BUILT ON MICROENGINE",
        "",
        "PIXEL ART",
        "LUIS ZUNO (ANSIMUZ) - WARPED SERIES, CC0",
        "",
        "MUSIC",
        "ANSIMUZ - CYBER RUNNER, UP AND RIGHT,",
        "JUMP RUN DUCK, BOSS FIGHT 2 (CC0)",
        "JUHANI JUNKALA - RETRO GAME MUSIC PACK (CC0)",
        "",
        "SOUND EFFECTS",
        "JUHANI JUNKALA - 512 SOUND EFFECTS (CC0)",
        "",
        "THANK YOU FOR PLAYING",
    ];

    private bool _leaving;
    private DrawBlend _blend;

    public override void Enter() => game.Music("ending");

    public override void Update(float dt)
    {
        _blend.Mark(Game);
        if (_leaving || Age < 4 || !(Input.Pressed("confirm") || Input.Pressed("pause"))) return;
        _leaving = true;
        Game.Scenes.Reset(new TitleScene(game), Transition.Fade(1f));
    }

    public override void Draw(IRenderer r)
    {
        var view = new StageView(game.Art, Font);
        float age = _blend.Age(this);
        view.DrawBackdrop(r, "city", new Vector2(age * 20, 0), Age, false);
        r.FillRect(new RectF(0, 0, r.Width, r.Height), Ui.Ink.WithAlpha(0.45f));
        float y = r.Height - age * 22;
        for (int i = 0; i < Credits.Length; i++)
        {
            float ly = y + i * 16;
            if (ly < -16 || ly > r.Height) continue;
            var color = i == 0 ? Ui.Gold : Credits[i].StartsWith('-') ? Ui.Magenta : Credits[i].Length > 0 && char.IsUpper(Credits[i][0]) && i is 8 or 11 or 14 or 19 ? Ui.Cyan : Ui.Text;
            Ui.Centered(r, Font, Credits[i], r.Width / 2f, ly, color, i == 0 ? 2 : 1);
        }
        float end = Credits.Length * 16 + r.Height;
        if (Age * 22 > end - 60)
        {
            Ui.Centered(r, Font, $"FINAL SCORE {Ui.N(game.Session.Score, 7)}   DEATHS {game.Session.Deaths}", r.Width / 2f, 120, Ui.Gold);
            if ((int)(Age * 2) % 2 == 0) Ui.Centered(r, Font, Game.Prompts.Format("PRESS {confirm}").ToUpperInvariant(), r.Width / 2f, 150, Ui.Text);
        }
        r.Draw(game.Art.Frames("hero_run")[(int)(Age * 10) % 8], new Vector2(60 + (age * 8 % 400), 262));
        Ui.Scanlines(r);
    }
}
