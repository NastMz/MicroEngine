namespace MicroEngine.Scenes;

/// <summary>
/// A world's own time: a time scale and freezes (hitstop) on top of the engine's fixed step. It advances as part of its
/// world's step (<see cref="SceneWorld.BeginStep"/>), once per step: a world that does not step (paused, under an
/// overlay) consumes no time and no freeze.
/// </summary>
public sealed class WorldClock
{
    private float _scale = 1;
    private float _freeze;

    /// <summary>Multiplies the step (0 pauses, 0.5 is slow motion); 1 by default.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
    public float Scale
    {
        get => _scale;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            _scale = value;
        }
    }

    /// <summary>Scaled seconds this world has advanced (freezes and scale applied).</summary>
    public double Time { get; private set; }
    /// <summary>Scaled length of the latest step (0 while frozen or at scale 0).</summary>
    public float Delta { get; private set; }
    /// <summary>
    /// Seconds of freeze left. Steps subtract their unscaled length while it is positive, so the last frozen step may
    /// leave it slightly below zero (the whole step was frozen); it is not frozen when it is 0 or less.
    /// </summary>
    public float FreezeRemaining => _freeze;
    /// <summary>Whether the next step is frozen.</summary>
    public bool Frozen => _freeze > 0;

    /// <summary>Freezes the world for at least <paramref name="seconds"/> of unscaled step time (the longer freeze wins). Hitstop is a freeze.</summary>
    public void Freeze(float seconds) => _freeze = MathF.Max(_freeze, seconds);

    /// <summary>Ends a freeze now.</summary>
    public void Unfreeze() => _freeze = 0;

    /// <summary>
    /// Advances one step of <paramref name="dt"/> unscaled seconds and returns the scaled length (0 while frozen).
    /// Exactly <paramref name="dt"/> at scale 1.
    /// </summary>
    internal float Advance(float dt)
    {
        if (_freeze > 0)
        {
            _freeze -= dt;
            Delta = 0;
            return 0;
        }
        Delta = _scale == 1 ? dt : dt * _scale;
        Time += Delta;
        return Delta;
    }
}
