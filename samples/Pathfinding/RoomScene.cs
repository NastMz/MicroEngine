using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Navigation;
using MicroEngine.Scenes;
using MicroEngine.World;

namespace Pathfinding;

/// <summary>
/// One room of tiles and four kinds of agent: the player's unit walks to the point clicked, a chaser goes after it, a
/// swarm reads one distance field towards it, and a patrol walks between two posts through a door that confirm (Z,
/// Enter or a pad's A) opens and closes. Every one of them is an agent of one <see cref="NavAgents"/> system: this
/// scene only says what each wants.
/// </summary>
public sealed class RoomScene : Scene
{
    public const int Tile = 16, Wall = 1, Door = 2, Mud = 3;
    /// <summary>The radius of a walker: routes keep that distance from walls, and bodies from each other.</summary>
    public const float Radius = 5;
    /// <summary>Mud is walked at a third of the speed, so entering it costs three times open ground.</summary>
    public const float MudCost = 3;
    /// <summary>How near the chaser comes to the player.</summary>
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
    // The tile map the game draws and collides with: walls are solid; the door is solid only while closed.
    private readonly TileGrid _tiles = new(
        TileLayer.Parse(Rows, new Dictionary<char, int> { ['#'] = Wall, ['D'] = Door, ['~'] = Mud }),
        Tile,
        new Dictionary<int, TileFlags> { [Wall] = TileFlags.Solid });
    private NavGrid _nav = null!;
    private DistanceField _field = null!;

    private void BuildGrid()
    {
        // The grid the agents walk: one cell per tile, solid tiles blocked. Mud can be walked, slowly, so its cells
        // cost more and a route crosses it only when going around takes longer.
        _nav = NavGrid.FromTiles(_tiles);
        for (int y = 0; y < _nav.Height; y++)
            for (int x = 0; x < _nav.Width; x++)
                if (_tiles.Layer[x, y] == Mud) _nav.SetCost(x, y, MudCost);
        _field = new DistanceField(_nav);
    }
    #endregion

    #region agents
    private NavAgents _agents = null!;

    public override void Enter()
    {
        BuildGrid();
        // The agents of this scene: the system is stepped with the scene, before its Update.
        _agents = Systems.Add(new NavAgents(_nav));
        // A body's radius and speed; mud slows these down as much as it costs.
        var walker = new NavAgentSettings(Radius, speed: 80) { SlowedByCost = true };
        Player = _agents.Add(At('P'), walker);
        // The chaser has arrived when it is within reach of the point it was sent to.
        Chaser = _agents.Add(At('C'), walker with { Speed = 50, ArrivalTolerance = CatchDistance });
        Posts = [At('A'), At('B')];
        Patrol = _agents.Add(Posts[0], walker with { Speed = 40 });
        foreach (var (x, y) in TileLayer.Find(Rows, 'b'))
            _swarm.Add(_agents.Add(_nav.CenterOf(_nav.NodeOf(x, y)), new NavAgentSettings(radius: 3, speed: 45) { SlowedByCost = true }));
        var (doorX, doorY) = TileLayer.Find(Rows, 'D').First();
        DoorBounds = new RectF(doorX * Tile, doorY * Tile, Tile, Tile);
        DoorCell = _nav.NodeOf(doorX, doorY);
    }
    #endregion

    private Vector2 At(char marker)
    {
        var (x, y) = TileLayer.Find(Rows, marker).First();
        return _nav.CenterOf(_nav.NodeOf(x, y));
    }

    private readonly List<NavAgent> _swarm = [];
    private int _post, _fieldGoal = -1;
    private bool _pointerWasDown;

    public NavGrid Nav => _nav;
    public TileGrid Tiles => _tiles;
    public NavAgents Agents => _agents;
    public NavAgent Player { get; private set; } = null!;
    public NavAgent Chaser { get; private set; } = null!;
    public NavAgent Patrol { get; private set; } = null!;
    public IReadOnlyList<NavAgent> Swarm => _swarm;
    /// <summary>The two points the patrol walks between.</summary>
    public Vector2[] Posts { get; private set; } = [];
    public RectF DoorBounds { get; private set; }
    public int DoorCell { get; private set; }
    public bool DoorClosed { get; private set; }

    /// <summary>Sends the player's unit to a point of the room (what a click does).</summary>
    public void SendPlayerTo(Vector2 point) => Player.MoveTo(point);

    #region door
    /// <summary>Closes or opens the door; it stays open while somebody stands in it.</summary>
    public bool SetDoor(bool closed)
    {
        // Nobody is in the doorway, and nobody on the way somewhere would be shut in.
        if (closed && !_agents.CanBlock(DoorBounds)) return false;
        DoorClosed = closed;
        // The tile map changes, and the grid reads it again. That is all: the agents whose way crossed the door
        // search again by themselves, and the others never notice.
        _tiles.SetFlags(Door, closed ? TileFlags.Solid : TileFlags.None);
        _nav.ReadTiles(_tiles);
        return true;
    }
    #endregion

    public override void Update(float dt)
    {
        if (Input.Pressed("action"))
        {
            Game.Scenes.Replace(new SkirmishScene());
            return;
        }
        if (Input.Pressed("confirm")) SetDoor(!DoorClosed);

        #region orders
        // A click sends the player's unit: it finds its way, walks it and stops on the point.
        var pointer = Input.Pointer;
        if (pointer.Primary && !_pointerWasDown) Player.MoveTo(pointer.Position);
        _pointerWasDown = pointer.Primary;

        // The chaser wants to be where the player is. Ordering it every step is free: a route is searched only when
        // the player enters another cell.
        Chaser.MoveTo(Player.Position);

        // The patrol walks from post to post: when it has arrived, it is sent to the other one.
        if (Patrol.Status is NavAgentStatus.Idle or NavAgentStatus.Arrived)
        {
            _post = 1 - _post;
            Patrol.MoveTo(Posts[_post]);
        }
        #endregion

        #region field
        // The swarm all want the player: one field holds the cost from every cell to the player's cell, and each bug
        // follows it without a search of its own. It is computed again when the player changes cell or the map
        // changes, and the swarm is set after it again.
        int playerCell = _nav.NodeAt(Player.Position);
        if (playerCell != _fieldGoal || _field.Version != _nav.Version)
        {
            _fieldGoal = playerCell;
            _field.Compute(playerCell);
            foreach (var bug in _swarm) bug.Follow(_field);
        }
        #endregion
    }

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
        DrawAgent(r, Patrol, Color.Hex("ffcd75"));
        DrawAgent(r, Chaser, Color.Hex("b13e53"));
        DrawAgent(r, Player, Color.Hex("41a6f6"));
        foreach (var bug in _swarm) DrawAgent(r, bug, Color.Hex("a7f070"));
        r.FillRect(new RectF(0, 0, r.Width, 12), Color.Black.WithAlpha(0.6f));
        Font.Draw(r, $"CLICK: MOVE   Z: DOOR ({(DoorClosed ? "CLOSED" : "OPEN")})   C: NEXT   SEARCHES {_agents.Searches}", new Vector2(4, 3), Color.White);
    }

    /// <summary>An agent's body and the route it still walks.</summary>
    internal static void DrawAgent(IRenderer r, NavAgent agent, Color color)
    {
        var from = agent.Position;
        foreach (var point in agent.Route)
        {
            r.DrawLine(from, point, 1, color.WithAlpha(0.45f));
            from = point;
        }
        r.FillCircle(agent.Position, agent.Radius, color);
    }
}
