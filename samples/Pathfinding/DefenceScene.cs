using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Kernel;
using MicroEngine.Navigation;
using MicroEngine.Scenes;

namespace Pathfinding;

/// <summary>
/// A wave and the towers built in its way: creeps walk from the gate to the keep, a click builds a tower on a cell
/// (or takes it away), and the creeps go around. A tower is refused where a creep stands and where it would leave
/// the creeps no way to the keep.
/// </summary>
public sealed class DefenceScene : Scene
{
    public const int Tile = 16;
    /// <summary>Seconds between two creeps.</summary>
    public const float SpawnEvery = 0.6f;
    /// <summary>How far a tower sees.</summary>
    public const float Range = 40;
    public const float CreepRadius = 4;

    private NavGrid _nav = null!;
    private NavAgents _agents = null!;
    private readonly List<NavAgent> _creeps = [];
    private readonly List<NavCell> _towers = [];
    private readonly List<NavAgent> _inRange = [];
    private float _spawnIn, _refusedFor;
    private bool _pointerWasDown;

    public NavGrid Nav => _nav;
    public NavAgents Agents => _agents;
    public IReadOnlyList<NavAgent> Creeps => _creeps;
    public IReadOnlyList<NavCell> Towers => _towers;
    /// <summary>The centre of the cell creeps come from.</summary>
    public Vector2 Gate { get; private set; }
    /// <summary>The centre of the cell creeps walk to.</summary>
    public Vector2 Keep { get; private set; }
    /// <summary>How many creeps reached the keep.</summary>
    public int Reached { get; private set; }
    /// <summary>The cell of the latest refused tower.</summary>
    public NavCell Refused { get; private set; }

    public override void Enter()
    {
        // An open field inside a wall, with the gate in its left side and the keep in its right side.
        _nav = new NavGrid(20, 11, Tile);
        for (int y = 0; y < _nav.Height; y++)
            for (int x = 0; x < _nav.Width; x++)
                if ((x == 0 || y == 0 || x == _nav.Width - 1 || y == _nav.Height - 1) && !(y == 5 && (x == 0 || x == _nav.Width - 1))) _nav.SetWalkable(x, y, false);
        Gate = _nav.CenterOf(_nav.NodeOf(0, 5));
        Keep = _nav.CenterOf(_nav.NodeOf(_nav.Width - 1, 5));
        _agents = Systems.Add(new NavAgents(_nav));
    }

    private static RectF CellBounds(NavCell cell) => new(cell.X * Tile, cell.Y * Tile, Tile, Tile);

    #region build
    /// <summary>Builds a tower on a cell, or takes away the one that is there. Returns whether the field changed.</summary>
    public bool Toggle(NavCell cell)
    {
        if (!_nav.InBounds(cell.X, cell.Y)) return false;
        var area = CellBounds(cell);
        if (_towers.Remove(cell))
        {
            _nav.SetBlocked(area, false);
            return true;
        }
        // The rule of the game: on open ground, where no creep stands or would be walled in, and where the gate
        // keeps a way to the keep for the creeps still to come.
        bool allowed = _nav.IsWalkable(cell.X, cell.Y)
            && _agents.CanBlock(area)
            && _agents.Regions(CreepRadius).StaysConnected(area, _nav.NodeAt(Gate), _nav.NodeAt(Keep));
        if (!allowed)
        {
            Refused = cell;
            _refusedFor = 0.4f;
            return false;
        }
        // Blocking the cells is all it takes: the creeps whose way it closes search again by themselves.
        _nav.SetBlocked(area, true);
        _towers.Add(cell);
        return true;
    }
    #endregion

    public override void Update(float dt)
    {
        if (Input.Pressed("action"))
        {
            Game.Scenes.Replace(new RoomScene());
            return;
        }
        var pointer = Input.Pointer;
        if (pointer.Primary && !_pointerWasDown) Toggle(_nav.CellAt(pointer.Position));
        _pointerWasDown = pointer.Primary;
        _refusedFor = MathF.Max(0, _refusedFor - dt);

        #region wave
        // A creep leaves the gate when the last one made room, and is sent to the keep once.
        _spawnIn -= dt;
        if (_spawnIn <= 0 && !_agents.Occupied(RectF.FromCenter(Gate, 2 * CreepRadius, 2 * CreepRadius)))
        {
            _spawnIn = SpawnEvery;
            var creep = _agents.Add(Gate, new NavAgentSettings(CreepRadius, speed: 40));
            creep.MoveTo(Keep);
            _creeps.Add(creep);
        }
        // One that arrived is in the keep: it leaves the field.
        for (int i = _creeps.Count - 1; i >= 0; i--)
        {
            if (_creeps[i].Status != NavAgentStatus.Arrived) continue;
            _agents.Remove(_creeps[i]);
            _creeps.RemoveAt(i);
            Reached++;
        }
        #endregion
    }

    public override void HashState(StateHasher hash)
    {
        hash.Add(_spawnIn);
        hash.Add(_refusedFor);
        hash.Add(Reached);
        hash.Add(_towers.Count);
    }

    public override void Draw(IRenderer r)
    {
        for (int y = 0; y < _nav.Height; y++)
            for (int x = 0; x < _nav.Width; x++)
                if (!_nav.IsWalkable(x, y) && !_towers.Contains(new NavCell(x, y))) r.FillRect(new RectF(x * Tile, y * Tile, Tile, Tile), Color.Hex("566c86"));
        r.FillRect(RectF.FromCenter(Gate, Tile, Tile), Color.Hex("38b764").WithAlpha(0.5f));
        r.FillRect(RectF.FromCenter(Keep, Tile, Tile), Color.Hex("ffcd75").WithAlpha(0.5f));
        foreach (var creep in _creeps) RoomScene.DrawAgent(r, creep, Color.Hex("b13e53"));
        #region range
        foreach (var tower in _towers)
        {
            var center = CellBounds(tower).Center;
            r.FillRect(CellBounds(tower).Inflate(-2), Color.Hex("94b0c2"));
            // The creeps within the tower's range, in the order they came: it points at the first.
            _inRange.Clear();
            if (_agents.Query(center, Range, _inRange) > 0) r.DrawLine(center, _inRange[0].Position, 1, Color.Hex("ffcd75"));
        }
        #endregion
        if (_refusedFor > 0) r.FillRect(CellBounds(Refused), Color.Hex("b13e53").WithAlpha(0.6f));
        r.FillRect(new RectF(0, 0, r.Width, 12), Color.Black.WithAlpha(0.6f));
        Font.Draw(r, $"CLICK: BUILD OR REMOVE A TOWER   C: NEXT   REACHED {Reached}", new Vector2(4, 3), Color.White);
    }
}
