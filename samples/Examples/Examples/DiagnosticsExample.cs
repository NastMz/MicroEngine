using System.Globalization;
using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Physics;
using MicroEngine.Physics.Tiles;
using MicroEngine.Scenes;
using MicroEngine.World;

namespace Examples;

/// <summary>
/// Diagnosing a game: F3 (or the gamepad's Back) turns on what the engine measures and draws. The overlay shows the last
/// frame's times, draw calls, allocations and slowest systems, and where the latest input went; the physics view draws
/// the tiles, the player's box, the sensor and the player's velocity.
/// </summary>
public sealed class DiagnosticsExample : Scene
{
    private const int Tile = 16;
    private const float Gravity = 700, JumpSpeed = 260, RunSpeed = 90;

    private static readonly string[] Rows =
    [
        "####################",
        "#                  #",
        "#                  #",
        "#                  #",
        "#      ====        #",
        "#                  #",
        "#           ###    #",
        "#                  #",
        "#   ##             #",
        "#                  #",
        "####################",
    ];

    private readonly TileGrid _grid = new(
        TileLayer.Parse(Rows, new Dictionary<char, int> { ['#'] = 1, ['='] = 2 }),
        Tile,
        new Dictionary<int, TileFlags> { [1] = TileFlags.Solid, [2] = TileFlags.OneWay });
    private readonly TilePhysicsWorld2D _world = new();
    private readonly List<string> _lastInput = [];
    private BodyId _player;
    private Vector2 _velocity;
    private MoveResult _last;

    public bool Showing => Game.Diagnostics.Enabled;
    public Vector2 PlayerPosition => _world.PositionOf(_player);

    public override void Enter()
    {
        Input.Bind("diagnostics", [Key.F3], [PadButton.Back]);
        var level = _world.CreateBody(default, BodyDescription.Static());
        _world.AddTiles(level, _grid);
        _player = _world.CreateBody(default, BodyDescription.Kinematic(new Vector2(60, 140)));
        _world.AddShape(_player, ShapeDescription.Box(new Vector2(10, 14)));
        var gem = _world.CreateBody(default, BodyDescription.Static(new Vector2(250, 140)));
        _world.AddShape(gem, ShapeDescription.Sensor(new Vector2(12, 12)));
    }

    public override void Exit()
    {
        Input.Unbind("diagnostics");
        Game.Diagnostics.Enabled = false;
        Game.DebugDraw.Enabled = false;
    }

    #region toggle
    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
        if (Input.Pressed("diagnostics"))
        {
            // Measuring and drawing are separate switches; this example turns both on and off together.
            bool on = !Game.Diagnostics.Enabled;
            Game.Diagnostics.Enabled = on;
            Game.DebugDraw.Enabled = on;
        }
        Move(dt);
        // Both calls do nothing while the debug drawing is off, so they can stay in the game.
        Game.DebugDraw.Physics(_world);
        var at = _world.PositionOf(_player);
        Game.DebugDraw.Line(at, at + _velocity * 0.1f, Color.White);
        Remember();
    }
    #endregion

    private void Move(float dt)
    {
        _velocity.X = Input.Axis("left", "right") * RunSpeed;
        _velocity.Y += Gravity * dt;
        if (_last.OnGround && Input.Pressed("confirm")) _velocity.Y = -JumpSpeed;
        _last = _world.Move(_player, _velocity * dt);
        if ((_last.OnGround && _velocity.Y > 0) || (_last.HitCeiling && _velocity.Y < 0)) _velocity.Y = 0;
        _world.Step(dt);
    }

    #region input
    /// <summary>Keeps the last few routed events: which layer took each one, or gameplay.</summary>
    private void Remember()
    {
        foreach (var routed in Game.InputSystem.Routed)
        {
            if (routed.Event.Kind is not (InputEventKind.KeyDown or InputEventKind.ButtonDown or InputEventKind.PointerDown)) continue;
            var target = routed.Layer?.GetType().Name ?? "gameplay";
            _lastInput.Insert(0, $"{routed.Event.Kind} {routed.Event.Key} -> {target}");
            if (_lastInput.Count > 3) _lastInput.RemoveAt(3);
        }
    }
    #endregion

    public override void Draw(IRenderer r)
    {
        for (int y = 0; y < Rows.Length; y++)
            for (int x = 0; x < Rows[y].Length; x++)
            {
                var flags = _grid.FlagsAt(x, y);
                if (flags == TileFlags.Solid) r.FillRect(new RectF(x * Tile, y * Tile, Tile, Tile), Color.Hex("3b5dc9"));
                else if (flags == TileFlags.OneWay) r.FillRect(new RectF(x * Tile, y * Tile, Tile, 4), Color.Hex("ef7d57"));
            }
        var at = _world.PositionOf(_player);
        r.FillRect(new RectF(at.X - 5, at.Y - 7, 10, 14), Color.Hex("a7f070"));
        Heading.Draw(r, Font, "DIAGNOSTICS", "F3: OVERLAY");
        if (Showing) Overlay(r);
    }

    #region overlay
    private void Overlay(IRenderer r)
    {
        var frame = Game.Diagnostics.LastFrame;
        var lines = new List<string>
        {
            string.Create(CultureInfo.InvariantCulture, $"FRAME {frame.Total:0.00} MS  UPDATE {frame.Update:0.00}  RENDER {frame.Render:0.00}"),
            string.Create(CultureInfo.InvariantCulture, $"{frame.Graphics.DrawCalls} DRAWS  {frame.Allocated} B  GC {frame.Collections}  VOICES {frame.Audio.ActiveVoices}"),
        };
        foreach (var system in Game.Diagnostics.Systems.OrderByDescending(s => s.Milliseconds).Take(3))
            lines.Add(string.Create(CultureInfo.InvariantCulture, $"  {system.Name} {system.Milliseconds:0.000}"));
        lines.AddRange(_lastInput);
        r.FillRect(new RectF(4, 16, 220, 8 * lines.Count + 4), Color.Black.WithAlpha(0.7f));
        for (int i = 0; i < lines.Count; i++) Font.Draw(r, lines[i].ToUpperInvariant(), new Vector2(6, 18 + 8 * i), Color.White);
    }
    #endregion
}
