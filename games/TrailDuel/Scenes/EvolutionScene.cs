using System.Collections;
using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using TrailDuel.Rules;
using TrailDuel.Ui;
using TrailDuel.Visuals;

namespace TrailDuel.Scenes;

/// <summary>Evolution: the old and new silhouettes trade places ever faster, a flash, then the reveal.</summary>
internal sealed class EvolutionScene(TrailDuelGame game, Creature creature, Action onDone) : Scene
{
    private readonly ParticleSystem _sparkles = new(300, 51) { Additive = true };
    private TextPanel _text = null!;
    private string _shownSpecies = "";
    private bool _white;
    private float _flash, _glow;
    private readonly string _from = creature.SpeciesId;
    private readonly string _to = creature.Species.EvolvesInto!;

    private static readonly ParticleSpec Sparkle = new()
    {
        StartColor = Color.Hex("fff6c8"), EndColor = Color.Hex("f8a0c000"), Life = (0.6f, 1.2f), Speed = (20, 60),
        Size = (3, 1), Gravity = -20, Round = false,
    };

    public override void Enter()
    {
        _text = new TextPanel(Font, Audio);
        _shownSpecies = _from;
        Audio.PlayMusic("evolution", 0.2f);
        Coroutines.Start(Run());
    }

    private IEnumerator Run()
    {
        string oldName = creature.Name;
        yield return Wait.Seconds(0.5f);
        yield return _text.Say($"What? {oldName} is evolving!");
        Audio.Play("evolve");
        _white = true;
        float interval = 0.5f;
        float elapsed = 0;
        while (elapsed < 4.2f)
        {
            _shownSpecies = _shownSpecies == _from ? _to : _from;
            _sparkles.Emit(new Vector2(160, 84), Sparkle, 6);
            yield return Wait.Seconds(interval);
            elapsed += interval;
            interval = MathF.Max(0.05f, interval * 0.8f);
            _glow = MathF.Min(1, elapsed / 4.2f);
        }
        creature.EvolveInto(_to);
        game.State.Seen.Add(_to);
        game.State.Caught.Add(_to);
        game.Achievements.Unlock(TrailAchievements.Evolution);
        _shownSpecies = _to;
        _flash = 1;
        Audio.Play("sendout");
        _sparkles.Emit(new Vector2(160, 84), Sparkle, 60);
        yield return Wait.Seconds(0.4f);
        _white = false;
        Audio.Play("jingle:catch");
        yield return _text.Say($"Congratulations! {oldName} evolved into {creature.Name}!");
        onDone();
        Game.Scenes.Pop(Transition.Fade(0.6f, Color.White));
    }

    public override void Update(float dt)
    {
        _text.Update(dt, Input);
        _sparkles.Update(dt);
        _flash = MathF.Max(0, _flash - dt * 1.5f);
    }

    public override void Draw(IRenderer r)
    {
        r.FillGradient(new RectF(0, 0, 320, 180), Color.Hex("2b2140"), Color.Lerp(Color.Hex("5a3a7a"), Color.Hex("f2a6c2"), _glow * 0.6f), 16);
        for (int i = 0; i < 16; i++)
        {
            float a = i / 16f * MathF.Tau + Age * 0.3f;
            var c = new Vector2(160, 84);
            r.FillTriangle(c, c + MathX.FromAngle(a, 260), c + MathX.FromAngle(a + 0.12f, 260), Pal.White.WithAlpha(0.04f + _glow * 0.06f));
        }
        r.Draw(game.Sprite("platform:enemy"), new Vector2(160, 104));
        string prefix = _white ? "white:" : "front:";
        float bob = MathF.Round(MathX.Wave(Age, 1, 1.5f));
        r.Draw(game.Sprite(prefix + _shownSpecies), new Vector2(160, 106 + bob));
        _sparkles.Draw(r);
        _text.Draw(r);
        if (_flash > 0) r.FillRect(new RectF(0, 0, 320, 180), Pal.White.WithAlpha(_flash));
    }
}
