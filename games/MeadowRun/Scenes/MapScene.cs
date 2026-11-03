using System.Numerics;
using MeadowRun.Art;
using MeadowRun.Content;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Scenes;

namespace MeadowRun.Scenes;

/// <summary>
/// The world map: Pip walks a dotted path between level nodes. Newly unlocked paths draw
/// themselves in dot by dot before control returns to the player.
/// </summary>
internal sealed class MapScene(MeadowGame game, int justUnlocked, int startNode) : Scene
{
    public static readonly Vector2[] Nodes = [new(58, 122), new(132, 98), new(196, 68), new(238, 128), new(272, 96)];
    private const int DotsPerPath = 7;
    private static readonly string[] NodeSprites = ["node0", "node1", "node2", "node3"];
    private static readonly string[] NodeNumbers = [.. Enumerable.Range(1, Nodes.Length).Select(n => Ui.N(n))];

    private int _node = Math.Clamp(startNode, 0, Nodes.Length - 1);
    private int _target;
    private float _walk = 1;
    private float _revealed = 1;
    private bool _leaving;
    private readonly StepBlend _step = new();
    private Vector2 _pipFrom;
    private readonly NumberLabel _lives = new("×", 2), _score = new(digits: 7);
    private int _infoLevel = -1, _infoBest;
    private string _infoTitle = "", _infoScore = "";
    private int Unlocked => game.Save.Unlocked;

    public int Node => _node;

    /// <summary>
    /// Hashes the node Pip stands on, the node he walks to and how far, the unlock reveal (it holds input back) and
    /// whether the scene is leaving. Leaves out the held position for blending, the step blend and the info panel's
    /// label caches (drawing writes them).
    /// </summary>
    public override void HashState(StateHasher hash)
    {
        hash.Add(_node);
        hash.Add(_target);
        hash.Add(_walk);
        hash.Add(_revealed);
        hash.Add(_leaving);
    }

    public override void Enter()
    {
        _target = _node;
        if (justUnlocked > 0)
        {
            _revealed = 0;
            Tweens.Add(1.2f, t => _revealed = t, Ease.Linear, () =>
            {
                game.Sfx("unlock", 0.9f);
                _target = justUnlocked;
                _walk = 0;
            }, delay: 0.6f);
        }
        game.Music("map");
    }

    public override void Update(float dt)
    {
        _step.Stepped(Game, dt);
        _pipFrom = PipPosition;
        if (_leaving) return;
        if (_walk < 1)
        {
            _walk = MathF.Min(1, _walk + dt * 2.2f);
            if ((int)(_walk * 8) != (int)((_walk - dt * 2.2f) * 8)) game.Sfx("step", 0.4f, 1.2f);
            if (_walk >= 1) _node = _target;
            return;
        }
        if (_revealed < 1) return;
        int dir = (Input.Pressed("right") || Input.Pressed("up") ? 1 : 0) - (Input.Pressed("left") || Input.Pressed("down") ? 1 : 0);
        if (dir != 0)
        {
            int next = _node + dir;
            if (next >= 0 && next < Nodes.Length && next < Unlocked)
            {
                _target = next;
                _walk = 0;
                game.Sfx("menu_move", 0.6f);
            }
            else game.Sfx("thud", 0.5f);
        }
        if (Input.Pressed("confirm"))
        {
            _leaving = true;
            game.Sfx("menu_select");
            Game.Scenes.Replace(new LevelCardScene(game, _node, 0), Transition.Iris(PipPosition, 0.9f, Pal.Ink));
        }
        else if (Input.Pressed("cancel"))
        {
            _leaving = true;
            game.Sfx("menu_back");
            Game.Scenes.Replace(new TitleScene(game), Transition.Diamonds(0.7f, Pal.Ink));
        }
    }

    private Vector2 PipPosition => Vector2.Lerp(Nodes[_node], Nodes[_target], Ease.InOutSine(_walk));

    private static Vector2 PathPoint(int from, float t)
    {
        var a = Nodes[from]; var b = Nodes[from + 1];
        var mid = (a + b) / 2 + new Vector2(0, -14);
        // Quadratic curve so paths arc gently over the island.
        return (1 - t) * (1 - t) * a + 2 * (1 - t) * t * mid + t * t * b;
    }

    public override void Draw(IRenderer r)
    {
        r.Draw(game.Art["map"], Vector2.Zero);
        float time = _step.Time(Game, Age);
        // Water shimmer.
        for (int i = 0; i < 14; i++)
        {
            float x = (i * 53 + time * 6) % 330 - 5, y = 10 + (i * 37) % 160;
            if (x < 20 || x > 300 || y < 24 || y > 170) r.FillRect(new RectF(MathF.Round(x), y, 4, 1), Pal.Cloud.WithAlpha(0.5f + MathX.Wave(time + i, 0.5f, 0.3f)));
        }
        for (int p = 0; p < Nodes.Length - 1; p++)
        {
            bool open = p + 1 < Unlocked;
            bool revealing = p + 1 == justUnlocked && _revealed < 1;
            for (int d = 1; d < DotsPerPath; d++)
            {
                float t = d / (float)DotsPerPath;
                if (revealing && t > _revealed) continue;
                var pt = PathPoint(p, t);
                var c = open || revealing ? Pal.Cream : Pal.Slate;
                r.FillRect(new RectF(MathF.Round(pt.X) - 1, MathF.Round(pt.Y) - 1, 3, 2), Pal.Ink);
                r.FillRect(new RectF(MathF.Round(pt.X) - 1, MathF.Round(pt.Y) - 2, 3, 2), c);
            }
        }
        for (int i = 0; i < Nodes.Length; i++)
        {
            int state = i >= Unlocked || (i == justUnlocked && _revealed < 1) ? 0 : game.Save.BestScores[i] > 0 ? 2 : i == Nodes.Length - 1 ? 3 : 1;
            float pop = i == justUnlocked && _revealed >= 1 ? MathF.Max(0, 1 - (Age - 1.8f) * 3) * 3 : 0;
            r.Draw(game.Art[NodeSprites[state]], Nodes[i] + new Vector2(0, -MathF.Round(pop)));
            Ui.Centered(r, Font, NodeNumbers[i], Nodes[i].X, Nodes[i].Y + 7, state == 0 ? Pal.Dusk : Pal.Cream);
        }

        var pip = Motion.Between(_pipFrom, PipPosition, _step.Blend(Game));
        bool walking = _walk < 1;
        var frames = game.Art.Pip(game.Session.Form, walking ? "run" : "idle");
        float hop = walking ? MathF.Abs(MathF.Sin(_walk * MathF.PI * 4)) * 3 : 0;
        bool flip = walking && Nodes[_target].X < Nodes[_node].X;
        r.FillRect(new RectF(MathF.Round(pip.X) - 5, MathF.Round(pip.Y) + 1, 10, 2), Pal.Ink.WithAlpha(0.4f));
        r.Draw(frames[(int)(Age * (walking ? 10 : 1.6f)) % frames.Count], new Vector2(MathF.Round(pip.X), MathF.Round(pip.Y - hop + 2)), null, flip);

        DrawHeader(r);
        DrawInfo(r);
    }

    private void DrawHeader(IRenderer r)
    {
        r.FillGradient(new RectF(0, 0, 320, 18), Pal.Ink.WithAlpha(0.7f), Pal.Ink.WithAlpha(0f), 4);
        Ui.Text(r, Font, "MEADOW ISLE", new Vector2(8, 5), Pal.Butter);
        r.Draw(game.Art["icon_life"], new Vector2(212, 4));
        Ui.Text(r, Font, _lives.For(game.Session.Lives), new Vector2(225, 5));
        string score = _score.For(game.Session.Score);
        Ui.Text(r, Font, score, new Vector2(312 - Font.MeasureWidth(score), 5));
    }

    private void DrawInfo(IRenderer r)
    {
        int i = _walk < 1 ? _target : _node;
        var def = Levels.All[i];
        var box = new RectF(8, 146, 304, 28);
        r.Panel(box, Ui.Card);
        int bestScore = game.Save.BestScores[i];
        if (i != _infoLevel || bestScore != _infoBest)
        {
            _infoLevel = i;
            _infoBest = bestScore;
            _infoTitle = $"{def.Number}  {def.Name}";
            _infoScore = bestScore > 0 ? "Best " + Ui.N(bestScore) : "Not cleared yet";
        }
        Ui.Text(r, Font, _infoTitle, new Vector2(16, 151), Pal.Butter);
        Ui.Text(r, Font, def.Subtitle, new Vector2(16, 162), Pal.Haze);
        string best = _infoScore;
        Ui.Text(r, Font, best, new Vector2(304 - Font.MeasureWidth(best), 151), game.Save.BestScores[i] > 0 ? Pal.Cream : Pal.Dusk);
        if ((int)(Age * 2) % 2 == 0 && _walk >= 1 && _revealed >= 1)
        {
            string hint = Game.Prompts.Format("{confirm}: play   {cancel}: title");
            Ui.Text(r, Font, hint, new Vector2(304 - Font.MeasureWidth(hint), 162), Pal.Gold);
        }
    }
}
