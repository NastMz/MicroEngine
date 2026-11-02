using System.Numerics;
using MeadowRun.Gameplay;
using MeadowRun.Gameplay.Actors;
using MicroEngine;
using MicroEngine.Core;

namespace MeadowRun.Scenes;

/// <summary>
/// Drawing between updates: updates run at a fixed rate and frames at the display's, so a frame is drawn
/// <see cref="Game.FrameBlend"/> of the way from the previous update to the current one. Only what is drawn
/// moves; the rules never see these positions.
/// </summary>
internal static class Motion
{
    /// <summary>A move longer than this in one update is a jump (respawn, eruption), drawn at once instead of streaking.</summary>
    public const float SnapDistance = 16;

    /// <summary>Where to draw something that moved from <paramref name="previous"/> to <paramref name="current"/> in the last update.</summary>
    public static Vector2 Between(Vector2 previous, Vector2 current, float blend)
    {
        if (blend >= 1 || Vector2.DistanceSquared(previous, current) > SnapDistance * SnapDistance) return current;
        return Vector2.Lerp(previous, current, MathF.Max(0, blend));
    }
}

/// <summary>
/// Knows whether a scene stepped in the game's latest update, so its frames blend only motion that just
/// happened: while it is paused, frozen under an overlay or leaving in a transition it draws its last state.
/// </summary>
internal sealed class StepBlend
{
    private long _steppedAt = -1;
    private float _step;

    /// <summary>Call from the scene's Update once it has advanced.</summary>
    public void Stepped(Game game, float dt) => Stepped(game.UpdateCount, dt);

    /// <summary>Records that the scene advanced by <paramref name="dt"/> in update number <paramref name="updateCount"/>.</summary>
    public void Stepped(long updateCount, float dt)
    {
        _steppedAt = updateCount;
        _step = dt;
    }

    /// <summary>How far the frame is between the previous update and the current one (1: draw the current state).</summary>
    public float Blend(Game game) => Blend(game.UpdateCount, game.FrameBlend);

    /// <summary><paramref name="frameBlend"/> when the scene stepped in update <paramref name="updateCount"/>, otherwise 1.</summary>
    public float Blend(long updateCount, float frameBlend) => _steppedAt == updateCount ? frameBlend : 1;

    /// <summary>The scene clock to draw with: <paramref name="age"/> one step back at blend 0, exactly <paramref name="age"/> at 1.</summary>
    public float Time(Game game, float age) => Time(age, Blend(game));

    /// <summary><paramref name="age"/> moved back by the part of the last step not reached yet at <paramref name="blend"/>.</summary>
    public float Time(float age, float blend) => age - (1 - blend) * _step;
}

/// <summary>
/// Keeps where the camera, Pip, actors and platforms were before the last stage update, and places them
/// between that and their current positions while a frame is drawn, then puts them back.
/// </summary>
internal sealed class StageMotion
{
    private Vector2 _cameraFrom, _pipFrom;
    private readonly List<Actor> _movers = [];
    private readonly List<Vector2> _held = [];
    private Vector2 _cameraHeld, _pipHeld;
    private bool _placed;

    /// <summary>Call just before every stage update.</summary>
    public void Remember(Stage stage)
    {
        _cameraFrom = stage.Camera.Position;
        _pipFrom = stage.Player.Position;
        foreach (var a in stage.Actors) a.PreviousPosition = a.Position;
        var platforms = stage.Platforms;
        for (int i = 0; i < platforms.Count; i++) platforms[i].PreviousPosition = platforms[i].Position;
    }

    /// <summary>Moves everything to where it is drawn at <paramref name="blend"/>; <see cref="Restore"/> undoes it. Nothing moves at blend 1.</summary>
    public void Place(Stage stage, float blend)
    {
        if (_placed) throw new InvalidOperationException("Restore the stage before placing it again.");
        if (blend >= 1) return;
        _placed = true;
        _movers.Clear();
        _held.Clear();
        foreach (var a in stage.Actors) _movers.Add(a);
        var platforms = stage.Platforms;
        for (int i = 0; i < platforms.Count; i++) _movers.Add(platforms[i]);
        foreach (var a in _movers)
        {
            _held.Add(a.Position);
            a.Position = Motion.Between(a.PreviousPosition, a.Position, blend);
        }
        _cameraHeld = stage.Camera.Position;
        _pipHeld = stage.Player.Position;
        stage.Camera.Position = Motion.Between(_cameraFrom, _cameraHeld, blend);
        stage.Player.Position = Motion.Between(_pipFrom, _pipHeld, blend);
    }

    /// <summary>Puts everything back where the rules left it.</summary>
    public void Restore(Stage stage)
    {
        if (!_placed) return;
        _placed = false;
        for (int i = 0; i < _movers.Count; i++) _movers[i].Position = _held[i];
        stage.Camera.Position = _cameraHeld;
        stage.Player.Position = _pipHeld;
        _movers.Clear();
    }
}
