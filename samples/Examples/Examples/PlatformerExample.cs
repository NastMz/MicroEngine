using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.World;

namespace Examples;

/// <summary>
/// Side-view movement through tiles: gravity, jumping, solid walls, one-way platforms you can drop through, a moving
/// platform that carries the player, and coins: plain classes the scene keeps in a list.
/// </summary>
public sealed class PlatformerExample : Scene
{
    #region level
    private const int Tile = 16, Wall = 1, Ledge = 2;

    // '#' solid, '=' one-way platform (land on it from above, jump through it from below), 'c' a coin.
    private static readonly string[] Rows =
    [
        "####################",
        "#                  #",
        "#  c            c  #",
        "#  ====      ====  #",
        "#        c         #",
        "#       ====       #",
        "#                  #",
        "#   ===         c  #",
        "#             ##   #",
        "#     ##      ##   #",
        "####################",
    ];

    private readonly TileGrid _grid = new(
        TileLayer.Parse(Rows, new Dictionary<char, int> { ['#'] = Wall, ['='] = Ledge }),
        Tile,
        new Dictionary<int, TileFlags> { [Wall] = TileFlags.Solid, [Ledge] = TileFlags.OneWay });
    #endregion

    #region state
    private const float Gravity = 700, JumpSpeed = 280, RunSpeed = 90, MaxFall = 400;
    private RectF _player = new(40, 120, 10, 14);
    private Vector2 _velocity;
    private float _dropTime;                         // while > 0, one-way platforms let the player fall through
    private RectF _lift = new(176, 96, 32, 6);       // a moving platform: an extra solid rectangle, not a tile
    private readonly List<Coin> _coins = [];
    private int _collected;

    public RectF Player => _player;
    public int Collected => _collected;
    public int CoinsLeft => _coins.Count;

    public override void Enter()
    {
        foreach (var (x, y) in TileLayer.Find(Rows, 'c'))
            _coins.Add(new Coin { Position = new Vector2(x * Tile + 4, y * Tile + 4) });
    }
    #endregion

    #region update
    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }

        // The lift moves first and carries the player when he stands on it.
        var lift = _lift with { X = 176 + MathF.Sin(Age) * 48 };
        bool riding = _player.Bottom == _lift.Top && _player.Right > _lift.Left && _player.Left < _lift.Right;
        if (riding) _player = TileMotion.Move(_player, new Vector2(lift.X - _lift.X, 0), _grid).Bounds;
        _lift = lift;
        IReadOnlyList<RectF> solids = [_lift];

        bool grounded = TileMotion.IsOnGround(_player, _grid, solids);
        _velocity.X = Input.Axis("left", "right") * RunSpeed;
        _velocity.Y = MathF.Min(_velocity.Y + Gravity * dt, MaxFall);
        if (grounded && Input.Pressed("confirm"))
        {
            if (Input.Down("down")) _dropTime = 0.2f;          // down + jump: drop through a one-way platform
            else _velocity.Y = -JumpSpeed;
        }
        _dropTime = MathF.Max(0, _dropTime - dt);

        // One call resolves walls, floors, ceilings, one-way platforms and the lift.
        var move = TileMotion.Move(_player, _velocity * dt, _grid, solids, ignoreOneWay: _dropTime > 0);
        _player = move.Bounds;
        if (move.HitFloor || move.HitCeiling) _velocity.Y = 0;

        // Objects against the player: the game compares rectangles.
        foreach (var coin in _coins)
            if (coin.Bounds.Intersects(_player)) { coin.Collected = true; _collected++; }
        _coins.RemoveAll(static coin => coin.Collected);      // removed after the walk, never during it
        foreach (var coin in _coins) coin.Update(dt);
    }
    #endregion

    public override void Draw(IRenderer r)
    {
        for (int y = 0; y < _grid.Layer.Height; y++)
        for (int x = 0; x < _grid.Layer.Width; x++)
        {
            var cell = new RectF(x * Tile, y * Tile, Tile, Tile);
            if (_grid.Layer[x, y] == Wall) r.FillRect(cell, Color.Hex("566c86"));
            else if (_grid.Layer[x, y] == Ledge) r.FillRect(cell with { Height = 4 }, Color.Hex("a7f070"));
        }
        r.FillRect(_lift, Color.Hex("ffcd75"));
        foreach (var coin in _coins) coin.Draw(r);
        r.FillRect(_player, Color.Hex("41a6f6"));
        Font.Draw(r, $"COINS {_collected}   JUMP: Z   DROP: DOWN+Z", new Vector2(20, 18), Color.White);
        Heading.Draw(r, Font, "PLATFORMER");
    }
}

#region coin
/// <summary>A game object as a plain class: a position, a box around it, its own Update and Draw.</summary>
public sealed class Coin
{
    private float _time;

    public Vector2 Position { get; init; }
    public RectF Bounds => new(Position.X, Position.Y, 8, 8);
    public bool Collected { get; set; }

    public void Update(float dt) => _time += dt;

    public void Draw(IRenderer r) =>
        r.FillCircle(Bounds.Center + new Vector2(0, MathF.Sin(_time * 4)), 4, Color.Hex("ffec27"));
}
#endregion
