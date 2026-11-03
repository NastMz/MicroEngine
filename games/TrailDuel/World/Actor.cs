using System.Numerics;
using MicroEngine;
using MicroEngine.Kernel;
using TrailDuel.Rules;
using TrailDuel.Visuals;

namespace TrailDuel.World;

/// <summary>
/// A grid-walking character (the player or an NPC). Position is a tile; while stepping it slides
/// toward the next tile over <see cref="StepTime"/> seconds with a two-frame walk cycle.
/// Ledge hops cover two tiles with a parabolic arc.
/// </summary>
internal sealed class Actor : IStateHashed
{
    public const float StepTime = 0.24f;
    public const float RunStepTime = 0.13f;
    public const float HopTime = 0.42f;

    public CharacterLook Look { get; set; }
    public int X { get; private set; }
    public int Y { get; private set; }
    public Direction Facing { get; set; }
    public bool Moving { get; private set; }
    public bool Hopping { get; private set; }
    public NpcDef? Def { get; init; }
    public bool Visible { get; set; } = true;
    /// <summary>Extra vertical lift in pixels (hop arc).</summary>
    public float Lift { get; private set; }

    private int _fromX, _fromY;
    private float _progress, _duration;
    private int _stride;
    private float _walkClock;
    private Interpolated _drawnPosition, _drawnLift;

    public Actor(CharacterLook look, int x, int y, Direction facing)
    {
        Look = look;
        (X, Y, Facing) = (x, y, facing);
        (_fromX, _fromY) = (x, y);
    }

    /// <summary>
    /// State hash: who it is (its NPC id and look), tile, facing, visibility, the step in progress (from-tile, progress,
    /// duration, hop) and the stride count. Left out: the walk clock and <see cref="Lift"/> (animation, the lift follows
    /// from the progress), the positions held for drawing between updates and the sprite-key cache drawing fills.
    /// </summary>
    public void HashState(StateHasher hash)
    {
        hash.Add(Def?.Id);
        hash.Add(Look.Key);
        hash.Add(X);
        hash.Add(Y);
        hash.Add((int)Facing);
        hash.Add(Visible);
        hash.Add(Moving);
        hash.Add(Hopping);
        hash.Add(_fromX);
        hash.Add(_fromY);
        hash.Add(_progress);
        hash.Add(_duration);
        hash.Add(_stride);
    }

    /// <summary>Pixel position of the tile's top-left, interpolated while moving.</summary>
    public Vector2 PixelPosition
    {
        get
        {
            if (!Moving) return new Vector2(X * 16, Y * 16);
            float t = Math.Clamp(_progress / _duration, 0, 1);
            return Vector2.Lerp(new Vector2(_fromX * 16, _fromY * 16), new Vector2(X * 16, Y * 16), t);
        }
    }

    /// <summary>The tile whose row the actor should be drawn with (its feet).</summary>
    public int DrawRow => (int)MathF.Floor((PixelPosition.Y + 15) / 16);

    public void Teleport(int x, int y)
    {
        (X, Y, _fromX, _fromY) = (x, y, x, y);
        Moving = Hopping = false;
        Lift = 0;
        _drawnPosition.Snap();
        _drawnLift.Snap();
    }

    /// <summary>Keeps where the actor is at the end of update <paramref name="update"/>, to draw it between updates.</summary>
    public void RecordDrawn(long update)
    {
        _drawnPosition.Record(PixelPosition, update);
        _drawnLift.Record(new Vector2(0, Lift), update);
    }

    /// <summary>The pixel position to draw at <paramref name="blend"/> (<c>Game.FrameBlend</c>) of update <paramref name="update"/>.</summary>
    public Vector2 DrawPosition(long update, float blend) => _drawnPosition.At(PixelPosition, update, blend);

    /// <summary>The hop lift to draw at <paramref name="blend"/> of update <paramref name="update"/>.</summary>
    public float DrawLift(long update, float blend) => _drawnLift.At(new Vector2(0, Lift), update, blend).Y;

    public void StartStep(Direction dir, float duration)
    {
        Facing = dir;
        var (dx, dy) = dir.Delta();
        (_fromX, _fromY) = (X, Y);
        X += dx;
        Y += dy;
        _progress = 0;
        _duration = duration;
        Moving = true;
        _stride++;
    }

    /// <summary>Hops down over a ledge: two tiles in the facing direction.</summary>
    public void StartHop(Direction dir)
    {
        Facing = dir;
        var (dx, dy) = dir.Delta();
        (_fromX, _fromY) = (X, Y);
        X += dx * 2;
        Y += dy * 2;
        _progress = 0;
        _duration = HopTime;
        Moving = Hopping = true;
        _stride++;
    }

    /// <summary>Advances movement; returns true on the update the actor arrives on its tile.</summary>
    public bool Update(float dt)
    {
        _walkClock += dt;
        if (!Moving) return false;
        _progress += dt;
        float t = Math.Clamp(_progress / _duration, 0, 1);
        Lift = Hopping ? MathF.Sin(t * MathF.PI) * 9f : 0;
        if (_progress < _duration) return false;
        Moving = Hopping = false;
        Lift = 0;
        (_fromX, _fromY) = (X, Y);
        return true;
    }

    /// <summary>The walk frame: 0 standing, 1/2 alternate legs during the middle of a step.</summary>
    public int Frame
    {
        get
        {
            if (!Moving) return 0;
            float t = _progress / _duration;
            return t is > 0.2f and < 0.8f ? 1 + _stride % 2 : 0;
        }
    }

    /// <summary>Walking in place against a wall still animates the legs.</summary>
    public int BumpFrame(float time) => ((int)(time * 6) % 4) switch { 1 => 1, 3 => 2, _ => 0 };

    private string? _spriteKey, _spriteKeyLook;
    private Direction _spriteKeyFacing;
    private int _spriteKeyFrame;

    /// <summary>The atlas key of <paramref name="frame"/> facing <see cref="Facing"/>, formatted only when it changes.</summary>
    public string SpriteKey(int frame)
    {
        if (_spriteKey is null || !ReferenceEquals(_spriteKeyLook, Look.Key) || _spriteKeyFacing != Facing || _spriteKeyFrame != frame)
        {
            _spriteKey = CharacterArt.Key(Look.Key, Facing, frame);
            (_spriteKeyLook, _spriteKeyFacing, _spriteKeyFrame) = (Look.Key, Facing, frame);
        }
        return _spriteKey;
    }

    public float WalkClock => _walkClock;
}
