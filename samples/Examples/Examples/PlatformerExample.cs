using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Physics;
using MicroEngine.Scenes;
using MicroEngine.World;

namespace Examples;

/// <summary>
/// Side-view movement through tiles on the scene's physics world: a character controller (gravity, jumping, solid
/// walls, one-way platforms you can drop through), a moving platform that carries the player, and coins: plain classes
/// the scene keeps in a list.
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
    private static readonly Vector2 Size = new(10, 14);
    private CharacterController2D _hero = null!;
    private BodyId _lift;                            // a moving platform: a kinematic body the game moves
    private readonly List<Coin> _coins = [];
    private int _collected;

    public RectF Player => new(_hero.Position.X - Size.X / 2, _hero.Position.Y - Size.Y / 2, Size.X, Size.Y);
    public int Collected => _collected;
    public int CoinsLeft => _coins.Count;

    public override void Enter()
    {
        // The scene's physics world: the scene steps it after Update, hashes it and disposes it.
        var world = Physics.World;
        var level = world.CreateBody(default, BodyDescription.Static());
        world.AddTiles(level, _grid, _grid.Layer.Width, _grid.Layer.Height);
        _lift = world.CreateBody(default, BodyDescription.Kinematic(new Vector2(192, 99)));
        world.AddShape(_lift, ShapeDescription.Box(new Vector2(32, 6)));
        // A character: a kinematic box and the rules that move it. JumpReleaseGravityScale 1 gives every jump the
        // same height; the default cuts a jump short when the button is released.
        _hero = Physics.Track(CharacterController2D.Create(world, default, new Vector2(45, 127), Size,
            new CharacterMotion { Gravity = 700, MaxFallSpeed = 400, MoveSpeed = 90, JumpSpeed = 280, JumpReleaseGravityScale = 1 }));
        foreach (var (x, y) in TileLayer.Find(Rows, 'c'))
            _coins.Add(new Coin { Position = new Vector2(x * Tile + 4, y * Tile + 4) });
    }
    #endregion

    #region update
    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }

        // The lift moves first; the character standing on it is carried along in its own step.
        var world = Physics.World;
        world.SetPosition(_lift, new Vector2(192 + MathF.Sin(Age) * 48, 99));

        // The controller takes intent, not input: walls, floors, ceilings, one-way platforms and the lift are its job.
        _hero.Step(dt, new CharacterInput(
            Input.Axis("left", "right"),
            JumpPressed: Input.Pressed("confirm"),
            JumpHeld: Input.Down("confirm"),
            Drop: Input.Down("down") && Input.Pressed("confirm")));      // down + jump: drop through a one-way platform

        // Objects against the player: the game compares rectangles.
        var player = Player;
        foreach (var coin in _coins)
            if (coin.Bounds.Intersects(player)) { coin.Collected = true; _collected++; }
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
        var lift = Physics.World.PositionOf(_lift);
        r.FillRect(new RectF(lift.X - 16, lift.Y - 3, 32, 6), Color.Hex("ffcd75"));
        foreach (var coin in _coins) coin.Draw(r);
        r.FillRect(Player, Color.Hex("41a6f6"));
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
