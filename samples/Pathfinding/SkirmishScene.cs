using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Navigation;
using MicroEngine.Scenes;

namespace Pathfinding;

/// <summary>
/// A group order: a click sends every unit to one point. They take the gaps they fit through (the two large units go
/// the wide way), keep apart on the way, and gather around the point instead of stacking on it.
/// </summary>
public sealed class SkirmishScene : Scene
{
    public const int Tile = 16;

    // '#' wall; u a unit, U a large unit.
    private static readonly string[] Rows =
    [
        "####################",
        "#u u u  #          #",
        "#       #          #",
        "#u u u             #",
        "#       #          #",
        "#u U u  ####   #####",
        "#       #          #",
        "#u U u             #",
        "#                  #",
        "#                  #",
        "####################",
    ];

    private NavGrid _nav = null!;
    private NavAgents _agents = null!;
    private readonly List<NavAgent> _units = [];
    private bool _pointerWasDown;

    public NavGrid Nav => _nav;
    public NavAgents Agents => _agents;
    public IReadOnlyList<NavAgent> Units => _units;
    /// <summary>The point of the latest order.</summary>
    public Vector2? Rally { get; private set; }

    public override void Enter()
    {
        _nav = new NavGrid(Rows[0].Length, Rows.Length, Tile);
        for (int y = 0; y < Rows.Length; y++)
            for (int x = 0; x < Rows[y].Length; x++)
                if (Rows[y][x] == '#') _nav.SetWalkable(x, y, false);
        _agents = Systems.Add(new NavAgents(_nav));
        #region sizes
        // Two sizes on one grid. A large unit is wider than a cell: it is led only through passages three cells wide.
        var small = new NavAgentSettings(radius: 4, speed: 70);
        var large = new NavAgentSettings(radius: 11, speed: 45) { Weight = 4 };     // harder to push aside
        for (int y = 0; y < Rows.Length; y++)
            for (int x = 0; x < Rows[y].Length; x++)
                if (Rows[y][x] is 'u' or 'U') _units.Add(_agents.Add(_nav.CenterOf(_nav.NodeOf(x, y)), Rows[y][x] == 'U' ? large : small));
        #endregion
    }

    #region order
    /// <summary>Sends the whole group to a point (what a click does).</summary>
    public void Order(Vector2 point)
    {
        Rally = point;
        // One order each. Their searches share the step's budget, and the bodies sort themselves out around the point.
        foreach (var unit in _units) unit.MoveTo(point);
    }
    #endregion

    public override void Update(float dt)
    {
        if (Input.Pressed("action"))
        {
            Game.Scenes.Replace(new DefenceScene());
            return;
        }
        var pointer = Input.Pointer;
        if (pointer.Primary && !_pointerWasDown) Order(pointer.Position);
        _pointerWasDown = pointer.Primary;
    }

    public override void Draw(IRenderer r)
    {
        for (int y = 0; y < _nav.Height; y++)
            for (int x = 0; x < _nav.Width; x++)
                if (!_nav.IsWalkable(x, y)) r.FillRect(new RectF(x * Tile, y * Tile, Tile, Tile), Color.Hex("566c86"));
        if (Rally is { } rally) r.FillCircle(rally, 2, Color.White.WithAlpha(0.7f));
        int arrived = 0;
        foreach (var unit in _units)
        {
            bool there = unit.Status == NavAgentStatus.Arrived;
            if (there) arrived++;
            RoomScene.DrawAgent(r, unit, unit.Radius > Tile / 2 ? Color.Hex("ef7d57") : there ? Color.Hex("73eff7") : Color.Hex("41a6f6"));
        }
        r.FillRect(new RectF(0, 0, r.Width, 12), Color.Black.WithAlpha(0.6f));
        Font.Draw(r, $"CLICK: MOVE THE GROUP   C: NEXT   ARRIVED {arrived}/{_units.Count}", new Vector2(4, 3), Color.White);
    }
}
