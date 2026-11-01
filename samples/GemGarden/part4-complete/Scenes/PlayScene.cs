using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using GemGarden.World;

namespace GemGarden.Scenes;

/// <summary>One round: collect the gems before the time runs out, avoiding the slimes.</summary>
public sealed class PlayScene : Scene
{
    #region fields
    public const float RoundSeconds = 60, HitPenalty = 5;

    private static readonly ParticleSpec Sparkle = new()
    {
        StartColor = Color.Hex("73eff7"), EndColor = Color.Hex("41a6f600"), Speed = (20, 60), Life = (0.3f, 0.6f),
    };

    private readonly MainGame _game;
    private readonly Level _level;
    private readonly Player _player;
    private readonly List<Vector2> _gems;
    private readonly List<Slime> _slimes;
    private readonly Camera2D _camera;
    private readonly ParticleSystem _sparkles = new(capacity: 128);
    private float _timeLeft = RoundSeconds;
    private bool _finished;

    public PlayScene(MainGame game)
    {
        _game = game;
        _level = new Level(game.Sprites);
        _player = new Player(game.Sprites, _level.PlayerStart);
        _gems = [.. _level.GemSpots];
        _slimes = [.. _level.SlimeStarts.Select(p => new Slime(game.Sprites, p))];
        // The camera shows a 320x180 window of the 640x320 garden and never looks outside it.
        _camera = new Camera2D(game.Width, game.Height) { Bounds = _level.Area };
        _camera.CenterOn(_player.Position);
    }

    public int Gems { get; private set; }
    public int TotalGems => _level.GemSpots.Count;
    public float TimeLeft => _timeLeft;
    #endregion

    #region update
    public override void Update(float dt)
    {
        if (Input.Pressed("pause"))
        {
            Game.Scenes.Push(new PauseScene(_game));
            return;
        }

        _player.Update(dt, Input, _level);
        foreach (var slime in _slimes) slime.Update(dt, _level);
        CollectGems();
        CheckSlimes();

        _sparkles.Update(dt);
        _camera.Follow(_player.Position, dt);
        _camera.Update(dt);

        _timeLeft = MathF.Max(0, _timeLeft - dt);
        if (_timeLeft == 0 || _gems.Count == 0) Finish();
    }
    #endregion

    #region collisions
    private void CollectGems()
    {
        for (int i = _gems.Count - 1; i >= 0; i--)
        {
            var gem = RectF.FromCenter(_gems[i], 8, 8);
            if (!gem.Intersects(_player.Bounds)) continue;
            _gems.RemoveAt(i);
            Gems++;
            Audio.Play("pickup", pitchJitter: 0.05f);
            _sparkles.Emit(gem.Center, Sparkle, 12);
        }
    }

    private void CheckSlimes()
    {
        foreach (var slime in _slimes)
        {
            if (!_player.CanBeHit || !slime.Bounds.Intersects(_player.Bounds)) continue;
            _player.Hit(slime.Position);
            _timeLeft = MathF.Max(0, _timeLeft - HitPenalty);
            _camera.Shake(0.5f);
            Audio.Play("hit");
        }
    }
    #endregion

    #region finish
    private void Finish()
    {
        if (_finished) return;
        _finished = true;
        bool newBest = _game.RecordScore(Gems);
        Game.Scenes.Replace(new ResultsScene(_game, Gems, TotalGems, newBest), Transition.Fade(0.8f));
    }
    #endregion

    #region draw
    public override void Draw(IRenderer r)
    {
        // World: everything between PushCamera and PopCamera is drawn in garden coordinates.
        r.PushCamera(_camera);
        _level.Draw(r, _camera);
        foreach (var gem in _gems) r.Draw(_game.Sprites["gem"], gem + new Vector2(0, MathX.Wave(Age, 1.5f, 1.5f)));
        foreach (var slime in _slimes) slime.Draw(r);
        _player.Draw(r);
        _sparkles.Draw(r);
        r.PopCamera();

        // HUD: drawn after PopCamera, so it stays in screen coordinates.
        DrawHud(r);
    }
    #endregion

    #region hud
    private void DrawHud(IRenderer r)
    {
        r.FillRect(new RectF(0, 0, Game.Width, 13), Color.Hex("1a1c2c").WithAlpha(0.75f));
        r.Draw(_game.Sprites["gem"], new Vector2(9, 6));
        Font.DrawShadowed(r, $"{Gems}/{TotalGems}", new Vector2(17, 3), Color.White, Color.Black);

        var clock = $"TIME {MathF.Ceiling(_timeLeft)}";
        var color = _timeLeft < 10 ? Color.Hex("ef7d57") : Color.White;
        Font.DrawShadowed(r, clock, new Vector2(Game.Width - 6 - Font.MeasureWidth(clock), 3), color, Color.Black);
    }
    #endregion
}
