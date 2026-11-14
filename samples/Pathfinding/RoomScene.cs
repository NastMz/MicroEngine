using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Navigation;
using MicroEngine.Scenes;
using MicroEngine.World;

namespace Pathfinding;

/// <summary>Something that walks a route: a box, a speed, the points left to walk through and the cells under them.</summary>
public sealed class Walker(Vector2 center, float size, float speed, Color color)
{
    public RectF Bounds { get; set; } = RectF.FromCenter(center, size, size);
    public float Speed { get; } = speed;
    public Color Color { get; } = color;
    public Vector2 Center => Bounds.Center;
    /// <summary>The points still to walk through, in order.</summary>
    public List<Vector2> Route { get; } = [];
    /// <summary>The cells of the path the route was made from.</summary>
    public List<int> Cells { get; } = [];
    /// <summary>The grid's version when the walker last looked at it.</summary>
    public int GridVersion { get; set; }
    /// <summary>How many times the walker searched for a path.</summary>
    public int Searches { get; set; }
}

/// <summary>
/// One room of tiles and four kinds of agent: the player's unit walks to the point clicked, a chaser goes after it, a
/// swarm reads one distance field towards it, and a patrol walks between two posts through a door that confirm (Z,
/// Enter or a pad's A) opens and closes.
/// </summary>
public sealed class RoomScene : Scene
{
    public const int Tile = 16, Wall = 1, Door = 2, Mud = 3;
    /// <summary>Half the side of a walker: routes keep that distance from walls.</summary>
    public const float Radius = 5;
    /// <summary>Mud is walked at a third of the speed, so entering it costs three times open ground.</summary>
    public const float MudCost = 3;
    /// <summary>How near the chaser and the swarm come to the player.</summary>
    public const float CatchDistance = 14;

    // '#' wall, 'D' the door, '~' mud; P the player's unit, C the chaser, A and B the patrol's posts, b the swarm.
    private static readonly string[] Rows =
    [
        "####################",
        "#      #     #     #",
        "#  P   #  A  D  B  #",
        "#      #     #     #",
        "#   ####     #  b  #",
        "#            #     #",
        "#   ~~~   ####  b  #",
        "#   ~~~            #",
        "#               b  #",
        "#  C               #",
        "####################",
    ];

    #region grid
    // The tile map the game collides with: walls are solid; the door is solid only while closed.
    private readonly TileGrid _tiles = new(
        TileLayer.Parse(Rows, new Dictionary<char, int> { ['#'] = Wall, ['D'] = Door, ['~'] = Mud }),
        Tile,
        new Dictionary<int, TileFlags> { [Wall] = TileFlags.Solid });
    private NavGrid _nav = null!;
    private readonly PathFinder _finder = new();      // one for the whole game: its memory is reused by every search
    private DistanceField _field = null!;

    private void BuildGrid()
    {
        // The grid the agents search: one cell per tile, solid tiles blocked. Mud can be walked, slowly, so its cells
        // cost more and a route crosses it only when going around takes longer.
        _nav = NavGrid.FromTiles(_tiles);
        for (int y = 0; y < _nav.Height; y++)
            for (int x = 0; x < _nav.Width; x++)
                if (_tiles.Layer[x, y] == Mud) _nav.SetCost(x, y, MudCost);
        _field = new DistanceField(_nav);
    }
    #endregion

    public override void Enter()
    {
        BuildGrid();
        Player = new Walker(At('P'), 2 * Radius, 80, Color.Hex("41a6f6"));
        Chaser = new Walker(At('C'), 2 * Radius, 50, Color.Hex("b13e53"));
        Posts = [At('A'), At('B')];
        Patrol = new Walker(Posts[0], 2 * Radius, 40, Color.Hex("ffcd75"));
        foreach (var (x, y) in TileLayer.Find(Rows, 'b'))
            _swarm.Add(new Walker(_nav.CenterOf(_nav.NodeOf(x, y)), 6, 45, Color.Hex("a7f070")));
        var (doorX, doorY) = TileLayer.Find(Rows, 'D').First();
        DoorBounds = new RectF(doorX * Tile, doorY * Tile, Tile, Tile);
        DoorCell = _nav.NodeOf(doorX, doorY);
    }

    private Vector2 At(char marker)
    {
        var (x, y) = TileLayer.Find(Rows, marker).First();
        return _nav.CenterOf(_nav.NodeOf(x, y));
    }

    private readonly List<Walker> _swarm = [];
    private int _post, _chasedCell = -1, _fieldGoal = -1;
    private bool _pointerWasDown;

    public NavGrid Nav => _nav;
    public TileGrid Tiles => _tiles;
    public Walker Player { get; private set; } = null!;
    public Walker Chaser { get; private set; } = null!;
    public Walker Patrol { get; private set; } = null!;
    public IReadOnlyList<Walker> Swarm => _swarm;
    /// <summary>The two points the patrol walks between.</summary>
    public Vector2[] Posts { get; private set; } = [];
    public RectF DoorBounds { get; private set; }
    public int DoorCell { get; private set; }
    public bool DoorClosed { get; private set; }

    #region plan
    /// <summary>Gives <paramref name="walker"/> the route to <paramref name="target"/>, or to the nearest point it can reach.</summary>
    public PathStatus Plan(Walker walker, Vector2 target)
    {
        // The route: the points to walk through, straightened so a walker of this size clears every corner.
        var status = _nav.FindPath(_finder, walker.Center, target, walker.Route, Radius);
        // The cells under it, kept to know later whether a change of the map closed this path.
        walker.Cells.Clear();
        walker.Cells.AddRange(_finder.Path);
        walker.GridVersion = _nav.Version;
        walker.Searches++;
        return status;
    }
    #endregion

    /// <summary>Sends the player's unit to a point of the room (what a click does).</summary>
    public PathStatus SendPlayerTo(Vector2 point) => Plan(Player, point);

    #region door
    /// <summary>Closes or opens the door; it stays open while somebody stands in it.</summary>
    public bool SetDoor(bool closed)
    {
        if (closed && (Player.Bounds.Intersects(DoorBounds) || Chaser.Bounds.Intersects(DoorBounds) || Patrol.Bounds.Intersects(DoorBounds)
            || _swarm.Exists(bug => bug.Bounds.Intersects(DoorBounds)))) return false;
        DoorClosed = closed;
        // The tile map changes for collision, and the grid reads it again: its version changes, which every walker sees.
        _tiles.SetFlags(Door, closed ? TileFlags.Solid : TileFlags.None);
        _nav.ReadTiles(_tiles);
        return true;
    }
    #endregion

    public override void Update(float dt)
    {
        var pointer = Input.Pointer;
        if (pointer.Primary && !_pointerWasDown) SendPlayerTo(pointer.Position);
        _pointerWasDown = pointer.Primary;
        if (Input.Pressed("confirm")) SetDoor(!DoorClosed);

        Follow(Player, dt);
        int playerCell = _nav.NodeAt(Player.Center);

        #region chase
        // The chaser wants to be where the player is. It searches when the player enters another cell or the map
        // changes, never every frame.
        if (playerCell != _chasedCell || Chaser.GridVersion != _nav.Version)
        {
            _chasedCell = playerCell;
            Plan(Chaser, Player.Center);
        }
        if (Vector2.Distance(Chaser.Center, Player.Center) > CatchDistance) Follow(Chaser, dt);
        #endregion

        #region replan
        // The patrol walks from post to post. When the map changes it looks at the cells of its path and searches
        // again only if one of them closed: an open door elsewhere costs it nothing.
        if (Patrol.Route.Count == 0)
        {
            _post = 1 - _post;
            Plan(Patrol, Posts[_post]);
        }
        else if (Patrol.GridVersion != _nav.Version)
        {
            Patrol.GridVersion = _nav.Version;
            if (!_nav.IsOpen(Patrol.Cells)) Plan(Patrol, Posts[_post]);
        }
        Follow(Patrol, dt);
        #endregion

        #region field
        // The swarm all want the player: one field holds the cost from every cell to the player's cell, and each bug
        // reads the cell to step to. It is computed again when the player changes cell or the map changes.
        if (playerCell != _fieldGoal || _field.Version != _nav.Version)
        {
            _fieldGoal = playerCell;
            _field.Compute(playerCell);
        }
        foreach (var bug in _swarm)
        {
            int next = _field.Next(_nav.NodeAt(bug.Center));
            if (next >= 0 && Vector2.Distance(bug.Center, Player.Center) > CatchDistance) Step(bug, _nav.CenterOf(next), dt);
        }
        #endregion
    }

    #region follow
    // Walking is the game's: towards the next point of the route, and on to the one after it once there.
    private void Follow(Walker walker, float dt)
    {
        if (walker.Route.Count > 0 && Step(walker, walker.Route[0], dt)) walker.Route.RemoveAt(0);
    }

    // One step towards a point at the walker's speed (a third of it in mud), through the same tile collision as
    // everything else that moves. Returns whether the point was reached.
    private bool Step(Walker walker, Vector2 target, float dt)
    {
        var cell = _nav.CellAt(walker.Center);
        float reach = walker.Speed * dt / _nav.CostAt(cell.X, cell.Y);
        var to = target - walker.Center;
        bool arrives = to.Length() <= reach;
        walker.Bounds = TileMotion.Move(walker.Bounds, arrives ? to : Vector2.Normalize(to) * reach, _tiles).Bounds;
        return arrives;
    }
    #endregion

    public override void Draw(IRenderer r)
    {
        for (int y = 0; y < _nav.Height; y++)
            for (int x = 0; x < _nav.Width; x++)
            {
                var cell = new RectF(x * Tile, y * Tile, Tile, Tile);
                int id = _tiles.Layer[x, y];
                if (id == Wall) r.FillRect(cell, Color.Hex("566c86"));
                else if (id == Mud) r.FillRect(cell, Color.Hex("38503a"));
                else if (id == Door) r.FillRect(DoorClosed ? cell : cell with { Height = 3 }, Color.Hex("ef7d57"));
            }
        foreach (var post in Posts) r.FillCircle(post, 2, Color.Hex("ffcd75").WithAlpha(0.5f));
        DrawRoute(r, Patrol);
        DrawRoute(r, Chaser);
        DrawRoute(r, Player);
        foreach (var bug in _swarm) r.FillRect(bug.Bounds, bug.Color);
        r.FillRect(Patrol.Bounds, Patrol.Color);
        r.FillRect(Chaser.Bounds, Chaser.Color);
        r.FillRect(Player.Bounds, Player.Color);
        r.FillRect(new RectF(0, 0, r.Width, 12), Color.Black.WithAlpha(0.6f));
        Font.Draw(r, $"CLICK: MOVE   Z: DOOR ({(DoorClosed ? "CLOSED" : "OPEN")})   SEARCHES {Player.Searches + Chaser.Searches + Patrol.Searches}", new Vector2(4, 3), Color.White);
    }

    private static void DrawRoute(IRenderer r, Walker walker)
    {
        var from = walker.Center;
        foreach (var point in walker.Route)
        {
            r.DrawLine(from, point, 1, walker.Color.WithAlpha(0.6f));
            from = point;
        }
    }
}
