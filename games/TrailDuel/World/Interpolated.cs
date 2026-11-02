using System.Numerics;

namespace TrailDuel.World;

/// <summary>
/// A position kept from the previous update so it can be drawn between updates (<c>Game.FrameBlend</c>): on displays
/// whose rate is not a multiple of the update rate, moving things then advance evenly instead of repeating a picture.
/// Record it at the end of every update and <see cref="Snap"/> it when the thing jumps (warps, new maps), so nothing
/// streaks across the screen. It only changes what is drawn, never the rules.
/// </summary>
internal struct Interpolated
{
    private Vector2 _previous, _last;
    private long _update;
    private bool _valid;

    /// <summary>Records <paramref name="value"/> at the end of update <paramref name="update"/> (<c>Game.UpdateCount</c>).
    /// After a <see cref="Snap"/> or a skipped update (a scene covered by a menu) the motion starts again from here.</summary>
    public void Record(Vector2 value, long update)
    {
        _previous = _valid && _update == update - 1 ? _last : value;
        _last = value;
        _update = update;
        _valid = true;
    }

    /// <summary>Forgets the previous value: the thing is drawn where it is until the next recorded update.</summary>
    public void Snap() => _valid = false;

    /// <summary>
    /// Where to draw <paramref name="current"/>: <c>Vector2.Lerp(previous, current, blend)</c> when it was recorded on
    /// <paramref name="update"/>, otherwise exactly <paramref name="current"/>. A blend of 1 (fixed-step runs) is always exact.
    /// </summary>
    public readonly Vector2 At(Vector2 current, long update, float blend)
        => _valid && _update == update && blend < 1 ? Vector2.Lerp(_previous, current, Math.Max(0, blend)) : current;
}
