using System.Numerics;
using MicroEngine;
using MicroEngine.Kernel;
using MicroEngine.Scenes;
using TrailDuel.Rules;
using TrailDuel.Visuals;

namespace TrailDuel.World;

/// <summary>
/// A grid-walking character (the player or an NPC). Position is a tile; while stepping it slides
/// toward the next tile over <see cref="StepTime"/> seconds with a two-frame walk cycle.
/// Ledge hops cover two tiles with a parabolic arc. Given the scene's objects, it is drawn through them: an entity at
/// its pixel position with a child for its sprite (lifted by the hop), so the world draws both between steps.
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
    private readonly IWorld? _world;
    private readonly EntityId _entity, _sprite;

    /// <param name="world">The scene's objects to draw it through; none for actors that are never drawn (rule tests).</param>
    public Actor(CharacterLook look, int x, int y, Direction facing, IWorld? world = null)
    {
        Look = look;
        (X, Y, Facing) = (x, y, facing);
        (_fromX, _fromY) = (x, y);
        if (world is null) return;
        _world = world;
        _entity = world.Create();
        _sprite = world.Create(_entity);
        Place();
    }

    /// <summary>
    /// State hash: who it is (its NPC id and look), tile, facing, visibility, the step in progress (from-tile, progress,
    /// duration, hop) and the stride count. Left out: the walk clock and <see cref="Lift"/> (animation, the lift follows
    /// from the progress), its entities (the scene's objects hash their transforms) and the sprite-key cache drawing fills.
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
            if (!Moving) return new Vector2(X * Tile, Y * Tile);
            float t = Math.Clamp(_progress / _duration, 0, 1);
            return Vector2.Lerp(new Vector2(_fromX * Tile, _fromY * Tile), new Vector2(X * Tile, Y * Tile), t);
        }
    }

    private const int Tile = MapDef.TileSize;

    /// <summary>The tile whose row the actor should be drawn with (its feet, on the tile's last pixel row).</summary>
    public int DrawRow => (int)MathF.Floor((PixelPosition.Y + Tile - 1) / Tile);

    /// <summary>Moves to a tile at once; it is drawn there from this step on, never sliding from where it was.</summary>
    public void Teleport(int x, int y)
    {
        (X, Y, _fromX, _fromY) = (x, y, x, y);
        Moving = Hopping = false;
        Lift = 0;
        Place();
        _world?.Teleport(_entity);
    }

    /// <summary>Where to draw the actor's feet (its shadow) this frame: between the start and the end of the step.</summary>
    public Vector2 DrawPosition => _world?.DrawPosition2D(_entity) ?? PixelPosition;

    /// <summary>Where to draw its sprite this frame: <see cref="DrawPosition"/> lifted by the hop.</summary>
    public Vector2 DrawSpritePosition => _world?.DrawPosition2D(_sprite) ?? PixelPosition - new Vector2(0, Lift);

    /// <summary>Takes the actor out of the scene's objects (it left the map).</summary>
    public void Destroy() => _world?.Destroy(_entity);

    /// <summary>Puts its entities where the actor is: its pixel position, and the sprite lifted by the hop.</summary>
    private void Place()
    {
        if (_world is null) return;
        _world.Local(_entity).Position2D = PixelPosition;
        _world.Local(_sprite).Position2D = new Vector2(0, -Lift);
    }

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
        if (_progress < _duration)
        {
            Place();
            return false;
        }
        Moving = Hopping = false;
        Lift = 0;
        (_fromX, _fromY) = (X, Y);
        Place();
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
